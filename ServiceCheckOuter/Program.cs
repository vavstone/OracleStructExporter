using ServiceCheck.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ServiceCheckOuter
{
    class Program
    {
        static OSESettings settings;
        static ExporterOuter exporter;
        static LoggerOuter logger;
        static int waitIntervalBeforeExitInSeconds;

        static void LoadSettingsFromFile()
        {
            settings = SettingsHelper.LoadSettings();
        }

        static void WaitBeforeExit()
        {
            Thread.Sleep(waitIntervalBeforeExitInSeconds*1000);
        }

        static void Main(string[] args)
        {
            try
            {
                LoadSettingsFromFile();

                exporter = new ExporterOuter();
                exporter.ProgressChanged += ProgressChanged;
                exporter.SetSettings(settings);
                exporter.SetSchedulerOuterProps();
                logger = new LoggerOuter(settings.TextFilesLog, settings.SchedulerOuterSettings.DBLog);

                //Статистика
                var prefixForGettingStat = settings.SchedulerOuterSettings.DBLog.DBLogPrefix; //settings.LogSettings.DBLog.DBLogPrefix;

                waitIntervalBeforeExitInSeconds = 5;

                //для отладки!!!!!!!!
                //prefixForGettingStat = "OSECA";

                // Проверка на уже запущенные экземпляры
                if (IsAlreadyRunning())
                {
                    exporter.ReportMainProcessError("Другая копия приложения уже запущена");
                    WaitBeforeExit();
                    return;
                }

                var statFullList = exporter.GetAggrFullStat(settings.SchedulerOuterSettings.ConnectionsOuterToProcess.ConnectionListToProcess, settings.SchedulerOuterSettings.GetStatForLastDays, prefixForGettingStat);
                

                var statInfoShort = logger.GetStatInfoV2(statFullList, settings.SchedulerOuterSettings.GetStatForLastDays);
                var statInfoFull = logger.GetStatFullInfo(statFullList, settings.SchedulerOuterSettings.GetStatForLastDays);
                
                var statSplitPeriodAppend = LoggerOuter.PeriodAppendToLogFileName(settings.TextFilesLog.StatSplitPeriod);
                logger.InsertStatFileLog(statInfoShort, $"stat{statSplitPeriodAppend}.txt");
                logger.InsertStatFileLog(statInfoFull, $"stat_full{statSplitPeriodAppend}.txt");

                // Выбор заданий для обработки
                var connectionToProcess = ExporterOuter.SelectConnectionToProcess(
                    statFullList,
                    settings.SchedulerOuterSettings.MinSuccessResultsForStat);
                if (connectionToProcess==null)
                {
                    exporter.ReportMainProcessMessage($"Нет заданий для обработки. {Environment.NewLine}");
                }
                else
                {
                    var dbIdC = connectionToProcess.DbId.ToUpper();
                    var userName = connectionToProcess.UserName.ToUpper();
                    var dbLink = connectionToProcess.DbLink.ToUpper();
                    var conn = settings.Connections.FirstOrDefault(c =>
                        c.DBIdC.ToUpper() == dbIdC && c.UserName.ToUpper() == userName);
                    ThreadInfoOuter threadInfo = new ThreadInfoOuter();
                    threadInfo.Connection = conn;
                    threadInfo.DbLink = dbLink;
                    //TODO
                    threadInfo.SchemasInclude = new List<string>();
                    threadInfo.SchemasExclude = new List<string>();


                    threadInfo.ExportSettings = settings.ExportSettings;
                    var schemasToWorkInfo = logger.GetToWorkInfo(connectionToProcess, true);
                    exporter.StartWork(threadInfo, schemasToWorkInfo, settings.TestMode);
                }

                WaitBeforeExit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            
        }

        static void ProgressChanged(object sender, ExportProgressChangedEventArgsOuter e)
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
                logger.InsertThreadsDBLog(progressData, true, exporter.LogDBConnectionString,
                    settings.SchedulerOuterSettings.DBLog);//settings.LogSettings.DBLog);
                if (progressData.IsEndOfSimpleRepoCreating)
                {
                    logger.InsertRepoTextFileLog(progressData, true);
                    logger.InsertRepoDBLog(progressData, true, exporter.LogDBConnectionString,
                        //settings.LogSettings.DBLog);
                        settings.SchedulerOuterSettings.DBLog);
                }
            }
        }

        private static bool IsAlreadyRunning()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            return processes.Length > 1;
        }

    }
}
