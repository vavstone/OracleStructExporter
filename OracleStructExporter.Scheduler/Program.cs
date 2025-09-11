using OracleStructExporter.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace OracleStructExporter.Scheduler
{
    class Program
    {

        static OSESettings settings;
        static Exporter exporter;
        static Logger logger;
        static int waitIntervalBeforeExitInSeconds;
        


        static void LoadSettingsFromFile()
        {
            settings = SettingsHelper.LoadSettings();
            settings.RepairSettingsValues();
        }

        static void WaitBeforeExit()
        {
            Thread.Sleep(waitIntervalBeforeExitInSeconds*1000);
            //Console.ReadLine();
        }

        static void Main(string[] args)
        {
            try
            {

                LoadSettingsFromFile();

                exporter = new Exporter();
                exporter.ProgressChanged += ProgressChanged;
                exporter.SetSettings(settings);
                logger = new Logger(settings.LogSettings);

                //Статистика
                var prefixForGettingStat = settings.LogSettings.DBLog.DBLogPrefix;
                //TODO брать из настроек
                var getStatForLastDays = 365;
                var minSuccessResultsForStat = 1;
                var testMode = false;
                waitIntervalBeforeExitInSeconds = 5;

                //для отладки!!!!!!!!
                //prefixForGettingStat = "OSECA";
                //testMode = true;

                // Проверка на уже запущенные экземпляры
                if (IsAlreadyRunning())
                {
                    exporter.ReportMainProcessError("Другая копия приложения уже запущена");
                    WaitBeforeExit();
                    return;
                }


                

                //var statList = exporter.GetAggrStat(settings.SchedulerSettings.ConnectionsToProcess.ConnectionListToProcess, getStatForLastDays, prefix);
                var statFullList = exporter.GetAggrFullStat(settings.SchedulerSettings.ConnectionsToProcess.ConnectionListToProcess, getStatForLastDays, prefixForGettingStat);
                

                //var statInfoShort = logger.GetStatInfo(statList, getStatForLastDays);
                var statInfoShort = logger.GetStatInfoV2(statFullList, getStatForLastDays);
                var statInfoFull = logger.GetStatFullInfo(statFullList, getStatForLastDays);
                
                //var statInfo3 = logger.GetStatInfoV3(statList, $"Статистика 3 за последние {getStatForLastDays} дней");
                //var statInfo4 = logger.GetStatInfoV4(statList, $"Статистика 4 за последние {getStatForLastDays} дней");

                logger.InsertOtherFileLog(statInfoShort, "stat.txt");
                logger.InsertOtherFileLog(statInfoFull, "stat_full.txt");

                //logger.InsertStatFileLog(statInfo3);
                //logger.InsertStatFileLog(statInfo4);

                // Выбор заданий для обработки
                //var connectionsToProcess = SelectConnectionsToProcess(statList);
                var connectionsToProcess = Exporter.SelectConnectionsToProcess(statFullList, settings.SchedulerSettings.ConnectionsToProcess.MaxConnectPerOneProcess, minSuccessResultsForStat);
                if (!connectionsToProcess.Any())
                {
                    exporter.ReportMainProcessMessage($"Нет заданий для обработки. {Environment.NewLine}");
                }
                else
                {
                    
                    var threads = new List<ThreadInfo>();
                    // Определение подключений для обработки
                    foreach (var item in connectionsToProcess)
                    {
                        var dbIdC = item.DbId.ToUpper();
                        var userName = item.UserName.ToUpper();
                        var conn = settings.Connections.FirstOrDefault(c => c.DBIdC.ToUpper() == dbIdC && c.UserName.ToUpper() == userName);
                        ThreadInfo threadInfo = new ThreadInfo();
                        threadInfo.Connection = conn;
                        threadInfo.ExportSettings = settings.ExportSettings;
                        threads.Add(threadInfo);
                    }

                    var schemasToWorkInfo = logger.GetToWorkInfo(connectionsToProcess, true);
                    exporter.StartWork(threads, schemasToWorkInfo, false, testMode);
                }

                WaitBeforeExit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            //try
            //{
            //    // Проверка на уже запущенные экземпляры
            //    if (IsAlreadyRunning())
            //    {
            //        Logger.LogToFile("Другая копия приложения уже запущена", "common_log.txt");
            //        return;
            //    }

            //    // Загрузка настроек
            //    _settings = SettingsHelper.LoadSettings("OSESettings.xml");
            //    _logPrefix = _settings.LogSettings.DBLog.DBLogPrefix;

            //    // Выбор заданий для обработки
            //    var connectionsToProcess = SelectConnectionsToProcess();

            //    if (!connectionsToProcess.Any())
            //    {
            //        Logger.LogToFile("Нет заданий для обработки", "common_log.txt");
            //        return;
            //    }

            //    // Создание записи в таблице PROCESS
            //    _processId = CreateProcessRecord(connectionsToProcess.Count());

            //    // Запуск обработки в параллельных потоках
            //    Parallel.ForEach(connectionsToProcess, connection => 
            //    {
            //        ProcessConnection(connection, _processId);
            //    });

            //    // Обновление времени завершения процесса
            //    UpdateProcessEndTime(_processId);
            //}
            //catch (Exception ex)
            //{
            //    Logger.LogToFile($"Критическая ошибка: {ex.Message}", "common_log.txt");
            //}
        }

        static void ProgressChanged(object sender, ExportProgressChangedEventArgs e)
        {
            var progressData = e.Progress;
            if (progressData.IsProgressFromMainProcess)
            {
                logger.InsertMainTextFileLog(progressData, true);
            }
            else
            {
                //сообщения от потоков
                logger.InsertThreadsTextFileLog(progressData, true, out string message);
                logger.InsertThreadsDBLog(progressData, true, exporter.LogDBConnectionString, settings.LogSettings.DBLog);
                if (progressData.IsEndOfSimpleRepoCreating)
                {
                    logger.InsertRepoTextFileLog(progressData, true);
                    logger.InsertRepoDBLog(progressData, true, exporter.LogDBConnectionString,
                        settings.LogSettings.DBLog);
                }

                
            }
        }

        private static bool IsAlreadyRunning()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            return processes.Length > 1;
        }

       

        //private static List<ConnectionToProcess> SelectConnectionsToProcess()
        //{
        //    var listEnabledConnections =
        //        settings.SchedulerSettings.ConnectionsToProcess.ConnectionListToProcess.Where(c =>
        //            c.Enabled).ToList();
        //    var listNotProcessedForLongTimeConnections = new List<ConnectionToProcess>();
        //    {
        //        //только коннекты, период обработки которых истек или по которым еще не было успешного результата или для которых не назначена частота проверки (OneSuccessResultPerHours=0)
        //        foreach (var enabledConnection in listEnabledConnections)
        //        {
        //            var connToDo = true;
        //            if (enabledConnection.OneSuccessResultPerHours > 0)
        //            {
        //                var lastSuccessTime =
        //                    exporter.GetLastSuccessExportForSchema(enabledConnection.DbId, enabledConnection.UserName);
        //                if (lastSuccessTime != null)
        //                {
        //                    var timeFromLastSuccess = DateTime.Now - lastSuccessTime.Value;
        //                    if (TimeSpan.FromHours(enabledConnection.OneSuccessResultPerHours) > timeFromLastSuccess)
        //                        connToDo = false;
        //                }
        //            }
        //            if (connToDo)
        //                listNotProcessedForLongTimeConnections.Add(enabledConnection);
        //        }

        //        //listNotProcessedForLongTimeConnections = listEnabledConnections;
        //    }

        //    var listToProcessConnections = new List<ConnectionToProcess>();
        //    {
        //        // Случайный порядок, количество коннектов в процессе не должно превышать MaxConnectPerOneProcess
        //        listToProcessConnections = listNotProcessedForLongTimeConnections.OrderBy(r => Guid.NewGuid())
        //            .Take(settings.SchedulerSettings.ConnectionsToProcess.MaxConnectPerOneProcess).ToList();
        //    }

        //    return listToProcessConnections;

        //    // Реализация логики выбора соединений для обработки
        //    // согласно алгоритму из ТЗ
        //    //return settings.SchedulerSettings.ConnectionsToProcess.ConnectionListToProcess
        //    //    .Where(c => c.Enabled && ShouldProcessConnection(c))
        //    //    .OrderBy(r => Guid.NewGuid()) // Случайный порядок
        //    //    .Take(settings.SchedulerSettings.ConnectionsToProcess.MaxConnectPerOneProcess)
        //    //    .ToArray();
        //}

        private static List<ConnectionToProcess> SelectConnectionsToProcess(List<SchemaWorkAggrStat> statInfo)
        {
            var listEnabledConnections =
                settings.SchedulerSettings.ConnectionsToProcess.ConnectionListToProcess.Where(c =>
                    c.Enabled).ToList();
            Dictionary<ConnectionToProcess, TimeSpan?> listToWorkConnectionsWithTimeBeforePlanLaunch =
                new Dictionary<ConnectionToProcess, TimeSpan?>();
            //var listNotProcessedForLongTimeConnections = new List<ConnectionToProcess>();
            {
                //только коннекты, период обработки которых истек или по которым еще не было успешного результата или для которых не назначена частота проверки (OneSuccessResultPerHours=0)
                foreach (var enabledConnection in listEnabledConnections)
                {
                    var connToDo = true;
                    TimeSpan? timeBeforePlanLaunch = null;
                    if (enabledConnection.OneSuccessResultPerHours > 0)
                    {
                        var statInfoItem = statInfo.FirstOrDefault(c =>
                            c.DBId == enabledConnection.DbId.ToUpper() &&
                            c.UserName == enabledConnection.UserName.ToUpper());
                             
                        if (statInfoItem != null && statInfoItem.TimeBeforePlanLaunch != null)
                        {
                            if (statInfoItem.TimeBeforePlanLaunch.Value>TimeSpan.Zero)
                                connToDo = false;
                            else
                            {
                                timeBeforePlanLaunch = statInfoItem.TimeBeforePlanLaunch.Value;
                            }
                        }
                    }

                    if (connToDo)
                        //listNotProcessedForLongTimeConnections.Add(enabledConnection);
                        listToWorkConnectionsWithTimeBeforePlanLaunch[enabledConnection] = timeBeforePlanLaunch;
                }

                //listNotProcessedForLongTimeConnections = listEnabledConnections;
            }

            var listToProcessConnections = new List<ConnectionToProcess>();
            {
                // По приоритету с наибольшим опозданием от графика количество коннектов в процессе не должно превышать MaxConnectPerOneProcess
                listToProcessConnections = listToWorkConnectionsWithTimeBeforePlanLaunch.
                    OrderBy(c=>c.Value??TimeSpan.MinValue).
                    Select(c=>c.Key).
                    Take(settings.SchedulerSettings.ConnectionsToProcess.MaxConnectPerOneProcess).ToList();
            }

            return listToProcessConnections;

        }

        

        //private static int CreateProcessRecord(int connectionsCount)
        //{
        //    using (var conn = new OracleConnection(GetLogConnectionString()))
        //    {
        //        conn.Open();
        //        string query = $"INSERT INTO {_logPrefix}PROCESS (id, connections_to_process, start_time) " +
        //                       $"VALUES ({_logPrefix}PROCESS_SEQ.NEXTVAL, :connectionsCount, SYSDATE) " +
        //                       $"RETURNING id INTO :id";

        //        using (var cmd = new OracleCommand(query, conn))
        //        {
        //            cmd.Parameters.Add("connectionsCount", connectionsCount);
        //            var idParam = new OracleParameter("id", OracleDbType.Int32, ParameterDirection.Output);
        //            cmd.Parameters.Add(idParam);

        //            cmd.ExecuteNonQuery();
        //            return Convert.ToInt32(idParam.Value);
        //        }
        //    }
        //}

        //private static void ProcessConnection(Connection connection, int processId)
        //{
        //    var exporter = new Exporter(_settings);
        //    exporter.ProgressChanged += (sender, e) => 
        //    {
        //        LogProgress(e);
        //    };

        //    exporter.StartWork(connection, processId);
        //}

        //private static void LogProgress(ExportProgressData data)
        //{
        //    // Логирование в файл
        //    Logger.LogToFile($"{data.EventTime}: {data.Stage} - {data.Message}", "common_log.txt");

        //    // Логирование в БД
        //    Logger.LogToDatabase(data, _logPrefix, GetLogConnectionString());
        //}
    }
}