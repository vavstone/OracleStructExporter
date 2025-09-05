using OracleStructExporter.Core.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OracleStructExporter.Core
{
    public class Logger
    {
        LogSettings _logSettings;
        //Connection _connection;
        static object lockObj = 0;

        private const string textSplitter =
            "----------------------------------------------------------------------------------------------------------------------------";
        public Logger(LogSettings logSettings)
        {
            _logSettings = logSettings;
            //_connection = connection;
        }

        public bool IsNecessaryToInsertLogEntry(ExportProgressDataLevel progressDataLevel,
            ExportProgressDataStage progressDataStage, LogType logType)
        {
            if (progressDataLevel == ExportProgressDataLevel.ERROR)
                return true;
            var curLog = logType == LogType.TextFilesLog ? (Log) _logSettings.TextFilesLog : (Log) _logSettings.DBLog;
            if (!curLog.ExcludeStageInfoC.Any())
                return true;
            foreach (var stage in curLog.ExcludeStageInfoC.Keys)
            {
                if (stage == progressDataStage &&
                    (curLog.ExcludeStageInfoC[stage] == ExportProgressDataLevel.NONE ||
                     curLog.ExcludeStageInfoC[stage] == progressDataLevel))
                    return false;
            }
            return true;
        }

        public void InsertThreadsTextFileLog(ExportProgressData progressData, bool checkOnNecessaryBySettings,
            out string messageText)
        {
            messageText = "";

            lock (lockObj)
            {
                if (!checkOnNecessaryBySettings || IsNecessaryToInsertLogEntry(progressData.Level, progressData.Stage, LogType.TextFilesLog))
                {
                    //готовим текст
                    if (progressData.Stage == ExportProgressDataStage.PROCESS_SCHEMA &&
                        progressData.Level == ExportProgressDataLevel.STAGESTARTINFO)
                    {
                        //вставляем стартовый разделитель
                        //messageText += Environment.NewLine;
                        messageText += textSplitter + Environment.NewLine;
                        messageText += "НАЧАЛО РАБОТЫ" + Environment.NewLine;
                        messageText += textSplitter + Environment.NewLine;
                        messageText += Environment.NewLine;
                    }

                    messageText += $"{progressData.EventTime.ToString("yyyy.MM.dd HH:mm:ss.fff")}. ProcessId: {progressData.ProcessId}. {progressData.Message}{Environment.NewLine}";

                    if (progressData.Level == ExportProgressDataLevel.ERROR &&
                        !string.IsNullOrWhiteSpace(progressData.ErrorDetails))
                        messageText += progressData.ErrorDetails + Environment.NewLine;

                    if (progressData.Stage == ExportProgressDataStage.PROCESS_SCHEMA &&
                        progressData.Level == ExportProgressDataLevel.STAGEENDINFO)
                    {
                        //вставляем финальный разделитель
                        messageText += Environment.NewLine;
                        messageText += textSplitter + Environment.NewLine;
                        messageText += "КОНЕЦ РАБОТЫ" + Environment.NewLine;
                        messageText += textSplitter + Environment.NewLine;
                        messageText += Environment.NewLine;
                    }

                    if (_logSettings.TextFilesLog.Enabled)
                    {
                        //сохраняем в файл запись лога
                        var encodingToFile1251 = Encoding.GetEncoding(1251);
                        var pathToLog = _logSettings.TextFilesLog.PathToLogFilesC;
                        var fileName = Path.Combine(pathToLog,
                            $"{progressData.CurrentConnection.DBIdCForFileSystem}_{progressData.CurrentConnection.UserName}.txt");
                        if (!Directory.Exists(pathToLog))
                            Directory.CreateDirectory(pathToLog);
                        using (StreamWriter writer = new StreamWriter(fileName, true, encodingToFile1251))
                        {
                            // Записываем DDL объекта
                            writer.Write(messageText);
                        }
                    }
                }
            }
        }

        public void InsertMainTextFileLog(ExportProgressData progressData, bool checkOnNecessaryBySettings)
        {
            var messageText = "";

            if (!checkOnNecessaryBySettings || IsNecessaryToInsertLogEntry(progressData.Level, progressData.Stage, LogType.TextFilesLog))
            {
                //готовим текст
                if (progressData.Stage == ExportProgressDataStage.PROCESS_MAIN &&
                    progressData.Level == ExportProgressDataLevel.STAGESTARTINFO)
                {
                    //вставляем стартовый разделитель
                    //messageText += Environment.NewLine;
                    messageText += textSplitter + Environment.NewLine;
                    messageText += "НАЧАЛО РАБОТЫ" + Environment.NewLine;
                    messageText += textSplitter + Environment.NewLine;
                    messageText += Environment.NewLine;
                }

                messageText += $"{progressData.EventTime.ToString("yyyy.MM.dd HH:mm:ss.fff")}. ProcessId: {progressData.ProcessId??"не задано"}. {progressData.Message}{Environment.NewLine}";

                if (progressData.Level == ExportProgressDataLevel.ERROR &&
                    !string.IsNullOrWhiteSpace(progressData.ErrorDetails))
                    messageText += progressData.ErrorDetails + Environment.NewLine;

                if (progressData.Stage == ExportProgressDataStage.PROCESS_MAIN &&
                    progressData.Level == ExportProgressDataLevel.STAGEENDINFO)
                {
                    //вставляем финальный разделитель
                    messageText += Environment.NewLine;
                    messageText += textSplitter + Environment.NewLine;
                    messageText += "КОНЕЦ РАБОТЫ" + Environment.NewLine;
                    messageText += textSplitter + Environment.NewLine;
                    messageText += Environment.NewLine;
                }

                if (_logSettings.TextFilesLog.Enabled)
                {
                    //сохраняем в файл запись лога
                    var encodingToFile1251 = Encoding.GetEncoding(1251);
                    var pathToLog = _logSettings.TextFilesLog.PathToLogFilesC;
                    var fileName = Path.Combine(pathToLog,
                        "log.txt");
                    if (!Directory.Exists(pathToLog))
                        Directory.CreateDirectory(pathToLog);
                    using (StreamWriter writer = new StreamWriter(fileName, true, encodingToFile1251))
                    {
                        // Записываем DDL объекта
                        writer.Write(messageText);
                    }
                }
            }
        }

        public void InsertStatFileLog(string message)
        {
            var messageText = "";

              //готовим текст


                messageText += $"{DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff")}{Environment.NewLine}";
                messageText += Environment.NewLine;
                messageText += message;
                messageText += Environment.NewLine;
            

                if (_logSettings.TextFilesLog.Enabled)
                {
                    //сохраняем в файл запись лога
                    var encodingToFile1251 = Encoding.GetEncoding(1251);
                    var pathToLog = _logSettings.TextFilesLog.PathToLogFilesC;
                    var fileName = Path.Combine(pathToLog,
                        "stat.txt");
                    if (!Directory.Exists(pathToLog))
                        Directory.CreateDirectory(pathToLog);
                    using (StreamWriter writer = new StreamWriter(fileName, true, encodingToFile1251))
                    {
                        // Записываем DDL объекта
                        writer.Write(messageText);
                    }
                }
            
        }

        public void InsertThreadsDBLog(ExportProgressData progressData, bool checkOnNecessaryBySettings, string connectionString, DBLog dbLogSettings)
        {
            if (!checkOnNecessaryBySettings ||
                IsNecessaryToInsertLogEntry(progressData.Level, progressData.Stage, LogType.DBLog))
            {
                if (_logSettings.DBLog.Enabled)
                {
                    //сохраняем в файл запись лога
                    DbWorker.SaveConnWorkLogInDB(_logSettings.DBLog.DBLogPrefix, progressData,  connectionString, dbLogSettings);
                }
            }

        }

        public string GetStatInfo(List<SchemaWorkAggrStat> statList, string tableTitle)
        {
            if (statList.Any())
            {
                var dataList = new List<List<string>>();
                var columnSettings = new List<ColumnSettings>();
                columnSettings.Add(new ColumnSettings { Header = "БД", Alignment = TextAlignment.Left, MaxWidth = 30, Padding = 0 });
                columnSettings.Add(new ColumnSettings { Header = "Схема", Alignment = TextAlignment.Left, MaxWidth = 30, Padding = 0 });
                columnSettings.Add(new ColumnSettings { Header = "План", Alignment = TextAlignment.Center, MaxWidth = 7, Padding = 0 });
                columnSettings.Add(new ColumnSettings { Header = "Время до планового запуска", Alignment = TextAlignment.Right, MaxWidth = 22, Padding = 0 });
                columnSettings.Add(new ColumnSettings { Header = "Раз в X часов, план", Alignment = TextAlignment.Right, MaxWidth = 10, Padding = 0 });
                columnSettings.Add(new ColumnSettings { Header = "Раз в X часов, факт", Alignment = TextAlignment.Right, MaxWidth = 10, Padding = 0 });
                columnSettings.Add(new ColumnSettings { Header = "Последний успешный запуск", Alignment = TextAlignment.Center, MaxWidth = 19, Padding = 0 });
                columnSettings.Add(new ColumnSettings { Header = "Кол-во успешных запусков", Alignment = TextAlignment.Right, MaxWidth = 10, Padding = 0 });
                columnSettings.Add(new ColumnSettings { Header = "Последний неусп-ый запуск", Alignment = TextAlignment.Center, MaxWidth = 19, Padding = 0 });
                columnSettings.Add(new ColumnSettings { Header = "Кол-во неусп-ых запусков", Alignment = TextAlignment.Right, MaxWidth = 10, Padding = 0 });
                columnSettings.Add(new ColumnSettings { Header = "Среднее время (мин)", Alignment = TextAlignment.Right, MaxWidth = 8, Padding = 0 });
                columnSettings.Add(new ColumnSettings { Header = "Средний объем", Alignment = TextAlignment.Right, MaxWidth = 8, Padding = 0 });
                foreach (var statItem in statList.OrderBy(c => c.TimeBeforePlanLaunch??TimeSpan.MaxValue))
                {
                    var dataRow = new List<string>();
                    dataRow.Add(statItem.DBId);
                    dataRow.Add(statItem.UserName);
                    dataRow.Add(statItem.IsScheduled ? "да" : "нет");
                    dataRow.Add(statItem.TimeBeforePlanLaunch == null ? string.Empty : statItem.TimeBeforePlanLaunch.Value.ToStringFormat(false));
                    dataRow.Add(statItem.OneTimePerHoursPlan == null ? string.Empty : statItem.OneTimePerHoursPlan.Value.ToString());
                    dataRow.Add(statItem.OneTimePerHoursFact == null ? string.Empty : statItem.OneTimePerHoursFact.Value.ToStringFormat(1));
                    dataRow.Add(statItem.LastSuccessLaunchFactTime == null ? string.Empty : statItem.LastSuccessLaunchFactTime.Value.ToString("yyyy.MM.dd HH:mm:ss"));
                    dataRow.Add(statItem.SuccessLaunchesCount.ToString());
                    dataRow.Add(statItem.LastErrorLaunchFactTime == null ? string.Empty : statItem.LastErrorLaunchFactTime.Value.ToString("yyyy.MM.dd HH:mm:ss"));
                    dataRow.Add(statItem.ErrorLaunchesCount.ToString());
                    dataRow.Add(statItem.AvgSuccessLaunchDurationInMinutes == null ? string.Empty : statItem.AvgSuccessLaunchDurationInMinutes.Value.ToStringFormat(1));
                    dataRow.Add(statItem.AvgSuccessLaunchObjectsFactCount == null ? string.Empty : statItem.AvgSuccessLaunchObjectsFactCount.Value.ToStringFormat(0));
                    dataList.Add(dataRow);
                }

                var textTableBuilder = new TextTableBuilder();
                return textTableBuilder.BuildTable(dataList, columnSettings, true, tableTitle);
            }
            return "Нет заданий и статистики";
        }
    }
}
