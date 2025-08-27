using OracleStructExporter.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace OracleStructExporter.WinForm
{
    public partial class MainForm : Form
    {
        OSESettings settings;
        Exporter exporter;
        Logger logger;
        List<ThreadInfo> threads;
        List<ThreadLogInfoControl> threadLogInfoControls = new List<ThreadLogInfoControl>();
        string processId;

        public MainForm()
        {
            InitializeComponent();
        }

        OSESettings LoadSettingsFromFile()
        {
            var settings = new OSESettings();

            {
                //TODO здесь надо заполнить settings по результатам парсинга файла настроек
            }

            {
                //временное решение
                settings.Connections = new Connections();
                settings.Connections.ConnectionsList = new List<Connection>();

                var exportSettingsDetails = new ExportSettingsDetails();
                exportSettingsDetails.AddSlashTo = Settings.ADD_SLASH_TO;
                exportSettingsDetails.SessionTransform = Settings.SESSION_TRANSFORM;
                exportSettingsDetails.SkipGrantOptions = Settings.SKIP_GRANT_OPTIONS;
                exportSettingsDetails.OrderGrantOptions = Settings.ORDER_GRANT_OPTIONS;
                exportSettingsDetails.SetSequencesValuesTo1 = cbSetSeqValTo1.Checked;
                exportSettingsDetails.ExtractOnlyDefPart = cbGetOnlyFirstPart.Checked;

                settings.ExportSettings = new ExportSettings();
                settings.ExportSettings.WriteOnlyToMainDataFolder = true;
                settings.ExportSettings.UseProcessesSubFolders = false;
                settings.ExportSettings.ExportSettingsDetails = exportSettingsDetails;

                var logSettings = new LogSettings();
                logSettings.TextFilesLog = new TextFilesLog();
                logSettings.TextFilesLog.ExcludeStageInfo = Settings.ExcludeStageInfo;
                logSettings.TextFilesLog.Enabled = true;

                logSettings.DBLog = new DBLog();
                logSettings.DBLog.DBLogPrefix = "OSEWA";
                logSettings.DBLog.ExcludeStageInfo = Settings.ExcludeStageInfo;
                logSettings.DBLog.Enabled = true;

                settings.LogSettings = logSettings;
            }

            settings.RepairSettingsValues();

            return settings;
        }

        void UpdateSettingsFromInputs(OSESettings settings)
        {
            var exportSettingsDetails = settings.ExportSettings.ExportSettingsDetails;
            exportSettingsDetails.MaskForFileNames = new MaskForFileNames();
            exportSettingsDetails.MaskForFileNames.Include = tbNameObjectMaskInclude.Text.Trim().ToUpper().Replace('*', '%');
            exportSettingsDetails.MaskForFileNames.Exclude = tbNameObjectMaskExclude.Text.Trim().ToUpper().Replace('*', '%');

            List<string> objectTypesToProcess = new List<string>();
            if (cbDBlinks.Checked) objectTypesToProcess.Add("DBLINKS");
            if (cbFunctions.Checked) objectTypesToProcess.Add("FUNCTIONS");
            if (cbJobs.Checked) objectTypesToProcess.Add("JOBS");
            if (cbPackages.Checked) objectTypesToProcess.Add("PACKAGES");
            if (cbProcedures.Checked) objectTypesToProcess.Add("PROCEDURES");
            if (cbSequences.Checked) objectTypesToProcess.Add("SEQUENCES");
            if (cbSynonyms.Checked) objectTypesToProcess.Add("SYNONYMS");
            if (cbTables.Checked) objectTypesToProcess.Add("TABLES");
            if (cbTriggers.Checked) objectTypesToProcess.Add("TRIGGERS");
            if (cbTypes.Checked) objectTypesToProcess.Add("TYPES");
            if (cbViews.Checked) objectTypesToProcess.Add("VIEWS");
            exportSettingsDetails.ObjectTypesToProcess = objectTypesToProcess.MergeFormatted(string.Empty, ";");

            settings.ExportSettings.PathToExportDataMain = txtOutputFolder.Text.Trim();
        }

        Connection GetConnectionParamsFromInputs()
        {
            var conn = new Connection();
            conn.Host = txtHost.Text.Trim();
            conn.Port = txtPort.Text.Trim();
            conn.SID = txtSID.Text.Trim();
            conn.UserName = txtUsername.Text.Trim();
            conn.Password = txtPassword.Text;
            return conn;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            

            threads = new List<ThreadInfo>();
            settings = LoadSettingsFromFile();
            logger = new Logger(settings.LogSettings);

            bool startThreadsList;
            {
                //TODO если пользователь выбрал хотя бы одно (через соответствующий checkbox) соединение в таблице gridConnections, то запускаем в потоках все выбранные соединения, игнорируя и выставляя "Disabled" все опциональные элементы ввода (в группах "Типы объектов", "Дополнительно", элементы "Целевая папка" и так далее), иначе запускаем один поток в соответствии с заполненными пользователем полями
                //здесь надо выставить флаг
                startThreadsList = false;
            }

            if (startThreadsList)
            {
                //TODO собираем информация о выбранных коннектах, и запускаем для каждого свой поток
                //здесь надо заполнить массив threads
            }
            else
            {
                var conn = GetConnectionParamsFromInputs();
                UpdateSettingsFromInputs(settings);

                if (string.IsNullOrEmpty(conn.Host) || string.IsNullOrEmpty(conn.Port) ||
                    string.IsNullOrEmpty(conn.SID) || string.IsNullOrEmpty(conn.UserName) ||
                    string.IsNullOrEmpty(settings.ExportSettings.PathToExportDataMain) || !settings.ExportSettings.ExportSettingsDetails.ObjectTypesToProcessC.Any())
                {
                    MessageBox.Show("Заполните все обязательные поля и выберите хотя бы один тип объекта");
                    return;
                }


                ThreadLogInfoControl threadLogInfoControl = threadLogInfoControls.FirstOrDefault(c =>
                    c.Connection.DBIdC.ToUpper() == conn.DBIdC.ToUpper() &&
                    c.Connection.UserName.ToUpper() == conn.UserName.ToUpper());
                if (threadLogInfoControl == null)
                {
                  threadLogInfoControl = new ThreadLogInfoControl();
                  threadLogInfoControl.Connection = conn;
                  threadLogInfoControls.Add(threadLogInfoControl);
                  var newTab = new TabPage($"{threadLogInfoControl.Connection.UserName}@{threadLogInfoControl.Connection.DBIdC}");
                  tabCtrlLog.TabPages.Add(newTab);
                  newTab.Controls.Add(threadLogInfoControl);
                  threadLogInfoControl.Dock = DockStyle.Fill;
                }


                settings.Connections.ConnectionsList.Add(conn);

                settings.LogSettings.DBLog.DBLogDBId = conn.DBIdC;
                settings.LogSettings.DBLog.DBLogUserName = conn.UserName;

                threadLogInfoControl.SetProgressStatus("Выгружено: 0, осталось выгрузить: 0");
                threadLogInfoControl.ResetProgressBar();

                btnExport.Enabled = false;
                btnCancel.Enabled = true;

                exporter = new Exporter();
                exporter.ProgressChanged += ProgressChanged;

                ThreadInfo threadInfo = new ThreadInfo();
                threadInfo.Connection = conn;
                threadInfo.ExportSettings = settings.ExportSettings;
                threads.Add(threadInfo);
            }

            if (settings.LogSettings.DBLog.Enabled)
            {
                var dbLogConn = settings.Connections.ConnectionsList.First(c =>
                    c.DBIdC.ToUpper() == settings.LogSettings.DBLog.DBLogDBId.ToUpper() && c.UserName.ToUpper() ==
                    settings.LogSettings.DBLog.DBLogUserName.ToUpper());
                Exporter.SetNewProcess(settings.LogSettings.DBLog.DBLogPrefix, dbLogConn, out processId);
            }
            else
            {
                processId = "NONE";
            }


            foreach (var thread in threads)
            {
                thread.ProcessId = processId;
                exporter.StartWork(thread);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (exporter != null)
                exporter.CancelWork();
            btnCancel.Enabled = false;
        }

        private void ProgressChanged(object sender, ExportProgressChangedEventArgs e)
        {
            var progressData = e.Progress;
            string message;
            logger.InsertTextFileLog(progressData, true, out message);
            logger.InsertDBLog(progressData, true);

            var currentThreadLogInfoControl = threadLogInfoControls.FirstOrDefault(c =>
                c.Connection.DBIdC.ToUpper() == progressData.CurrentConnection.DBIdC.ToUpper() &&
                c.Connection.UserName.ToUpper() == progressData.CurrentConnection.UserName.ToUpper());

            if (!string.IsNullOrWhiteSpace(message))
            {
                // Обновление прогресса
                currentThreadLogInfoControl.SetProgressStatus($"Выгружено: {progressData.Current}, " +
                                         $"осталось выгрузить: {progressData.TotalObjects - progressData.Current}");
               currentThreadLogInfoControl.AppendText(message);
                currentThreadLogInfoControl.SetLblStatus(progressData.Message);
                // Для ProgressBar: меняем стиль, если это первый прогресс
                currentThreadLogInfoControl.SetProgressStyleIfFirstProgress(progressData.TotalObjects);
                // Обновление значения прогресс-бара
                currentThreadLogInfoControl.SetProgressValue(progressData.Current);
            }

            if (progressData.ProcessFinished)
            {

                if (progressData.Level != ExportProgressDataLevel.ERROR &&
                    progressData.Level != ExportProgressDataLevel.CANCEL)
                {
                    currentThreadLogInfoControl.SetLblStatus("Готово");
                }

                var currentThread = threads.First(c => c.Connection == progressData.CurrentConnection);
                currentThread.Finished = true;
                if (threads.All(c => c.Finished))
                {
                    if (settings.LogSettings.DBLog.Enabled)
                    {
                        var dbLogConn = settings.Connections.ConnectionsList.First(c =>
                            c.DBIdC.ToUpper() == settings.LogSettings.DBLog.DBLogDBId.ToUpper() && c.UserName.ToUpper() ==
                            settings.LogSettings.DBLog.DBLogUserName.ToUpper());
                        Exporter.UpdateProcess(settings.LogSettings.DBLog.DBLogPrefix, dbLogConn, processId);
                    }
                    btnExport.Enabled = true;
                    btnCancel.Enabled = false;
                    MessageBox.Show(progressData.Message);
                }
            }
        }
    }
}
