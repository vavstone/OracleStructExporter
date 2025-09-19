using ServiceCheck.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ServiceCheck.WinForm
{
    public partial class MainForm : Form
    {
        OSESettings settings;
        Exporter exporter;
        Logger logger;
        List<ThreadInfo> threads;
        List<ThreadLogInfoControl> threadLogInfoControls = new List<ThreadLogInfoControl>();
        //string processId;

        public MainForm()
        {
            InitializeComponent();
            LoadSettingsFromFile();
            LoadConnectionsGrid();
            //UpdateFormValues();
        }

        private void UpdateFormValues()
        {
            txtOutputFolder.Text = settings.ExportSettings.PathToExportDataMain;
            cbSetSeqValTo1.Checked = settings.ExportSettings.ExportSettingsDetails.SetSequencesValuesTo1;
            
            var items = new[]
            {
                new { Key = GetPartitionMode.NONE, Text = "Не получать партиции таблиц" },
                new { Key = GetPartitionMode.ONLYDEFPART, Text = "Получать только партиции таблиц по умолчанию" },
                new { Key = GetPartitionMode.ALL, Text = "Получать все партиции таблиц" }
            };

            cbGetPartMode.DataSource = items;
            cbGetPartMode.DisplayMember = "Text";
            cbGetPartMode.ValueMember = "Key";

            switch (settings.ExportSettings.ExportSettingsDetails.GetPartitionMode)
            {
                case GetPartitionMode.NONE: cbGetPartMode.SelectedIndex = 0; break;
                case GetPartitionMode.ONLYDEFPART: cbGetPartMode.SelectedIndex = 1; break;
                case GetPartitionMode.ALL: cbGetPartMode.SelectedIndex = 2; break;
            }


            if (settings.ExportSettings.ExportSettingsDetails.MaskForFileNames != null)
            {
                tbNameObjectMaskInclude.Text = settings.ExportSettings.ExportSettingsDetails.MaskForFileNames.Include;
                tbNameObjectMaskInclude.Text = settings.ExportSettings.ExportSettingsDetails.MaskForFileNames.Exclude;
            }

            var objectsToProcess = settings.ExportSettings.ExportSettingsDetails.ObjectTypesToProcessC;
            if (objectsToProcess.Contains("DBLINKS")) cbDBlinks.Checked = true;
            if (objectsToProcess.Contains("FUNCTIONS")) cbFunctions.Checked = true;
            if (objectsToProcess.Contains("JOBS")) cbJobs.Checked = true;
            if (objectsToProcess.Contains("PACKAGES")) cbPackages.Checked = true;
            if (objectsToProcess.Contains("PROCEDURES")) cbProcedures.Checked = true;
            if (objectsToProcess.Contains("SEQUENCES")) cbSequences.Checked = true;
            if (objectsToProcess.Contains("SYNONYMS")) cbSynonyms.Checked = true;
            if (objectsToProcess.Contains("TABLES")) cbTables.Checked = true;
            if (objectsToProcess.Contains("TRIGGERS")) cbTriggers.Checked = true;
            if (objectsToProcess.Contains("TYPES")) cbTypes.Checked = true;
            if (objectsToProcess.Contains("VIEWS")) cbViews.Checked = true;
        }

        void LoadSettingsFromFile()
        {
            settings = SettingsHelper.LoadSettings();
            //settings.RepairSettingsValues();
        }
		
		private void LoadConnectionsGrid()
        {
            gridConnections.AutoGenerateColumns = false;
            gridConnections.DataSource = settings.Connections;
            gridConnections.Columns.Clear();
            
            var col1 = new DataGridViewCheckBoxColumn();
            col1.Name = "colConnSelected";
            col1.HeaderText = String.Empty;
            col1.Width = 30;
            gridConnections.Columns.Add(col1);
            var col2 = new DataGridViewTextBoxColumn();
            col2.Name = "colConnDbIdC";
            col2.DataPropertyName = "DBIdC";
            col2.HeaderText = "БД";
            col2.Width = 200;
            gridConnections.Columns.Add(col2);
            var col3 = new DataGridViewTextBoxColumn();
            col3.Name = "colConnUserName";
            col3.DataPropertyName = "UserName";
            col3.HeaderText = "Схема";
            col3.Width = 190;
            gridConnections.Columns.Add(col3);
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

            exportSettingsDetails.SetSequencesValuesTo1 = cbSetSeqValTo1.Checked;

            switch (cbGetPartMode.SelectedIndex)
            {
                case 0: exportSettingsDetails.GetPartitionMode = GetPartitionMode.NONE; break;
                case 1: exportSettingsDetails.GetPartitionMode = GetPartitionMode.ONLYDEFPART; break;
                case 2: exportSettingsDetails.GetPartitionMode = GetPartitionMode.ALL; break;
            }


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
            logger = new Logger(settings.TextFilesLog);

            UpdateSettingsFromInputs(settings);

            // Определение подключений для обработки
            foreach (DataGridViewRow row in gridConnections.Rows)
            {
                var checkboxCell = row.Cells[0] as DataGridViewCheckBoxCell;
                var checkboxValue = checkboxCell.Value != null ? (bool)checkboxCell.Value : false;
                if (checkboxValue)
                {
                    var dbIdCell = row.Cells[1] as DataGridViewTextBoxCell;
                    var userNameCell = row.Cells[2] as DataGridViewTextBoxCell;
                    var dbIdC = dbIdCell.Value.ToString();
                    var userName = userNameCell.Value.ToString();
                    var conn = settings.Connections.FirstOrDefault(c => c.DBIdC == dbIdC && c.UserName == userName);

                    ThreadInfo threadInfo = new ThreadInfo();
                    threadInfo.Connection = conn;
                    threadInfo.ExportSettings = settings.ExportSettings;
                    threads.Add(threadInfo);
                }
            }

            if (!threads.Any())
            {
                // Создание подключения из ручного ввода
                var conn = GetConnectionParamsFromInputs();

                if (string.IsNullOrEmpty(conn.Host) || string.IsNullOrEmpty(conn.Port) ||
                    string.IsNullOrEmpty(conn.SID) || string.IsNullOrEmpty(conn.UserName) ||
                    string.IsNullOrEmpty(settings.ExportSettings.PathToExportDataMain) ||
                    !settings.ExportSettings.ExportSettingsDetails.ObjectTypesToProcessC.Any())
                {
                    MessageBox.Show("Заполните все обязательные поля и выберите хотя бы один тип объекта");
                    return;
                }

                var existingConn = settings.Connections.FirstOrDefault(c =>
                    c.Host.ToUpper() == conn.Host.ToUpper() &&
                    c.Port.ToUpper() == conn.Port.ToUpper() &&
                    c.SID.ToUpper() == conn.SID.ToUpper() &&
                    c.UserName.ToUpper() == conn.UserName.ToUpper());
                if (existingConn != null)
                    conn = existingConn;
                else
                {
                    settings.Connections.Add(conn);
                }

                ThreadInfo threadInfo = new ThreadInfo();
                threadInfo.Connection = conn;
                threadInfo.ExportSettings = settings.ExportSettings;
                threads.Add(threadInfo);
            }

            var connectionsToProcess = new List<PrognozBySchema>();
            foreach (var threadInfo in threads)
            {
                var conn = threadInfo.Connection;
                ThreadLogInfoControl threadLogInfoControl = threadLogInfoControls.FirstOrDefault(c =>
                    c.Connection.DBIdC.ToUpper() == conn.DBIdC.ToUpper() &&
                    c.Connection.UserName.ToUpper() == conn.UserName.ToUpper());
                if (threadLogInfoControl == null)
                {
                    threadLogInfoControl = new ThreadLogInfoControl();
                    threadLogInfoControl.Connection = conn;
                    threadLogInfoControls.Add(threadLogInfoControl);
                    var newTab =
                        new TabPage(
                            $"{threadLogInfoControl.Connection.UserName}@{threadLogInfoControl.Connection.DBIdC}");
                    tabCtrlLog.TabPages.Add(newTab);
                    newTab.Controls.Add(threadLogInfoControl);
                    threadLogInfoControl.Dock = DockStyle.Fill;
                }
                threadLogInfoControl.SetProgressStatus("Выгружено: 0, осталось выгрузить: 0");
                threadLogInfoControl.StartProgressBar();
                connectionsToProcess.Add(new PrognozBySchema{ DbId = threadInfo.Connection.DbId, UserName = threadInfo.Connection.UserName});
            }

            btnExport.Enabled = false;
            btnCancel.Enabled = true;

            exporter = new Exporter();
            exporter.ProgressChanged += ProgressChanged;
            exporter.SetSettings(settings);


            var schemasToWorkInfo = logger.GetToWorkInfo(connectionsToProcess, false);


            exporter.StartWork(threads, schemasToWorkInfo, true, false);

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

            if (progressData.IsProgressFromMainProcess)
            {
                logger.InsertMainTextFileLog(progressData, true);
                if (progressData.ProcessFinished)
                {
                    btnExport.Enabled = true;
                    btnCancel.Enabled = false;
                    MessageBox.Show(progressData.Message);
                }
            }
            else
            {
                //сообщения от потоков
                logger.InsertThreadsTextFileLog(progressData, true, out message);
                //logger.InsertThreadsDBLog(progressData, true, exporter.LogDBConnectionString, settings.LogSettings.DBLog);

                var currentThreadLogInfoControl = threadLogInfoControls.FirstOrDefault(c =>
                    c.Connection.DBIdC.ToUpper() == progressData.CurrentConnection.DBIdC.ToUpper() &&
                    c.Connection.UserName.ToUpper() == progressData.CurrentConnection.UserName.ToUpper());

                if (!string.IsNullOrWhiteSpace(message))
                {
                    // Обновление прогресса
                    currentThreadLogInfoControl.SetProgressStatus($"Выгружено: {progressData.Current??0}, " +
                                             $"осталось выгрузить: {progressData.SchemaObjCountPlan??0 - progressData.Current??0}");
                    currentThreadLogInfoControl.AppendText(message);
                    currentThreadLogInfoControl.SetLblStatus(progressData.Message);
                    // Для ProgressBar: меняем стиль, если это первый прогресс
                    currentThreadLogInfoControl.SetProgressStyleIfFirstProgress(progressData.SchemaObjCountPlan??0);
                    // Обновление значения прогресс-бара
                    if (progressData.Current > 0)
                        currentThreadLogInfoControl.SetProgressValue(progressData.Current??0);
                }

                if (progressData.ThreadFinished)
                {
                    if (progressData.Level == ExportProgressDataLevel.CANCEL)
                        currentThreadLogInfoControl.SetLblStatus("Поток прерван");
                    else if (progressData.Level == ExportProgressDataLevel.ERROR)
                        currentThreadLogInfoControl.SetLblStatus("Ошибка работы потока");
                    else
                        currentThreadLogInfoControl.SetLblStatus("Готово");

                    currentThreadLogInfoControl.EndProgressBar();
                }
            }
        }
		
		private void gridConnections_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var connection = (Connection)gridConnections.Rows[e.RowIndex].DataBoundItem;
                //txt.Text = connection.DBIdC;
                txtHost.Text = connection.Host;
                txtPort.Text = connection.Port;
                txtSID.Text = connection.SID;
                txtUsername.Text = connection.UserName;
                txtPassword.Text = connection.PasswordC;
            }
        }

        void SetAllConnections(bool value)
        {
            foreach (DataGridViewRow row in gridConnections.Rows)
            {
                var checkboxCell = row.Cells[0] as DataGridViewCheckBoxCell;
                checkboxCell.Value = value;
            }
        }


        private void btCheckAllConnections_Click(object sender, EventArgs e)
        {
            SetAllConnections(true);
        }

        private void btCheckNoneConnections_Click(object sender, EventArgs e)
        {
            SetAllConnections(false);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UpdateFormValues();
        }
    }
}
