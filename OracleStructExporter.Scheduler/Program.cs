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
        //private static OSESettings _settings;
        //private static string _logPrefix;
        //private static int _processId;

        static OSESettings settings;
        static Exporter exporter;
        static Logger logger;
        


        static void LoadSettingsFromFile()
        {
            settings = SettingsHelper.LoadSettings();
            settings.RepairSettingsValues();
        }

        static void WaitBeforeExit()
        {
            Thread.Sleep(5000);
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

                // Проверка на уже запущенные экземпляры
                if (IsAlreadyRunning())
                {
                    exporter.ReportMainProcessError("Другая копия приложения уже запущена");
                    WaitBeforeExit();
                    return;
                }
                // Выбор заданий для обработки
                var connectionsToProcess = SelectConnectionsToProcess();
                if (!connectionsToProcess.Any())
                {
                    exporter.ReportMainProcessMessage("Нет заданий для обработки");
                    WaitBeforeExit();
                    return;
                }
                var threads = new List<ThreadInfo>();
                // Определение подключений для обработки
                foreach (var item in connectionsToProcess)
                {
                    var dbIdC = item.DbId;
                    var userName = item.UserName;
                    var conn = settings.Connections.FirstOrDefault(c => c.DBIdC == dbIdC && c.UserName == userName);
                    ThreadInfo threadInfo = new ThreadInfo();
                    threadInfo.Connection = conn;
                    threadInfo.ExportSettings = settings.ExportSettings;
                    threads.Add(threadInfo);
                }
                exporter.StartWork(threads,false);
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
            string message;

            if (progressData.IsProgressFromMainProcess)
            {
                //TODO сообщения от главного процесса
                logger.InsertMainTextFileLog(progressData, true);
            }
            else
            {
                //сообщения от потоков
                logger.InsertThreadsTextFileLog(progressData, true, out message);
                logger.InsertThreadsDBLog(progressData, true, exporter.LogDBConnectionString);
            }
        }

        private static bool IsAlreadyRunning()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            return processes.Length > 1;
        }

        private static List<ConnectionToProcess> SelectConnectionsToProcess()
        {
            var listEnabledConnections =
                settings.SchedulerSettings.ConnectionsToProcess.ConnectionListToProcess.Where(c =>
                    c.Enabled).ToList();
            var listNotProcessedForLongTimeConnections = new List<ConnectionToProcess>();
            {
                //TODO только коннекты, период обработки которых истек
                listNotProcessedForLongTimeConnections = listEnabledConnections;
            }

            var listToProcessConnections = new List<ConnectionToProcess>();
            {
                // Случайный порядок, количество коннектов в процессе не должно превышать MaxConnectPerOneProcess
                listToProcessConnections = listNotProcessedForLongTimeConnections.OrderBy(r => Guid.NewGuid())
                    .Take(settings.SchedulerSettings.ConnectionsToProcess.MaxConnectPerOneProcess).ToList();
            }

            return listToProcessConnections;

            // Реализация логики выбора соединений для обработки
            // согласно алгоритму из ТЗ
            //return settings.SchedulerSettings.ConnectionsToProcess.ConnectionListToProcess
            //    .Where(c => c.Enabled && ShouldProcessConnection(c))
            //    .OrderBy(r => Guid.NewGuid()) // Случайный порядок
            //    .Take(settings.SchedulerSettings.ConnectionsToProcess.MaxConnectPerOneProcess)
            //    .ToArray();
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