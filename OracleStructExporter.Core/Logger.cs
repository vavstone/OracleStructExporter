using OracleStructExporter.Core.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using TableBuilder;

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

        public void InsertRepoTextFileLog(ExportProgressData progressData, bool checkOnNecessaryBySettings)
        {
            //TODO реализовать
            
            
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

        public void InsertOtherFileLog(string message, string fileName)
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
                    var fileFullName = Path.Combine(pathToLog,
                        fileName);
                    if (!Directory.Exists(pathToLog))
                        Directory.CreateDirectory(pathToLog);
                    using (StreamWriter writer = new StreamWriter(fileFullName, true, encodingToFile1251))
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

        public void InsertRepoDBLog(ExportProgressData progressData, bool checkOnNecessaryBySettings, string connectionString, DBLog dbLogSettings)
        {

            //TODO проверять что это лог с репо и необходимость записи этого лога
            bool saveDetails = true;
            //if (!checkOnNecessaryBySettings ||
            //    IsNecessaryToInsertLogEntry(progressData.Level, progressData.Stage, LogType.DBLog))
            {
                if (_logSettings.DBLog.Enabled)
                {
                    //сохраняем в файл запись лога
                    DbWorker.SaveRepoChangesInDB(_logSettings.DBLog.DBLogPrefix, progressData, connectionString, dbLogSettings, saveDetails);
                }
            }

        }

        //public string GetStatInfo(List<SchemaWorkAggrStat> statList, string tableTitle)
        //{
        //    if (statList.Any())
        //    {
        //        var dataList = new List<List<string>>();
        //        var columnSettings = new List<ColumnSettings>();
        //        columnSettings.Add(new ColumnSettings { Header = "БД", Alignment = TextAlignment.Left, MaxWidth = 30, Padding = 0 });
        //        columnSettings.Add(new ColumnSettings { Header = "Схема", Alignment = TextAlignment.Left, MaxWidth = 30, Padding = 0 });
        //        columnSettings.Add(new ColumnSettings { Header = "План", Alignment = TextAlignment.Center, MaxWidth = 7, Padding = 0 });
        //        columnSettings.Add(new ColumnSettings { Header = "Время до планового запуска", Alignment = TextAlignment.Right, MaxWidth = 22, Padding = 0 });
        //        columnSettings.Add(new ColumnSettings { Header = "Раз в X часов, план", Alignment = TextAlignment.Right, MaxWidth = 10, Padding = 0 });
        //        columnSettings.Add(new ColumnSettings { Header = "Раз в X часов, факт", Alignment = TextAlignment.Right, MaxWidth = 10, Padding = 0 });
        //        columnSettings.Add(new ColumnSettings { Header = "Последний успешный запуск", Alignment = TextAlignment.Center, MaxWidth = 19, Padding = 0 });
        //        columnSettings.Add(new ColumnSettings { Header = "Кол-во успешных запусков", Alignment = TextAlignment.Right, MaxWidth = 10, Padding = 0 });
        //        columnSettings.Add(new ColumnSettings { Header = "Последний неусп-ый запуск", Alignment = TextAlignment.Center, MaxWidth = 19, Padding = 0 });
        //        columnSettings.Add(new ColumnSettings { Header = "Кол-во неусп-ых запусков", Alignment = TextAlignment.Right, MaxWidth = 10, Padding = 0 });
        //        columnSettings.Add(new ColumnSettings { Header = "Среднее время (мин)", Alignment = TextAlignment.Right, MaxWidth = 8, Padding = 0 });
        //        columnSettings.Add(new ColumnSettings { Header = "Средний объем", Alignment = TextAlignment.Right, MaxWidth = 8, Padding = 0 });
        //        foreach (var statItem in statList.OrderBy(c => c.TimeBeforePlanLaunch??TimeSpan.MaxValue))
        //        {
        //            var dataRow = new List<string>();
        //            dataRow.Add(statItem.DBId);
        //            dataRow.Add(statItem.UserName);
        //            dataRow.Add(statItem.IsScheduled ? "да" : "нет");
        //            dataRow.Add(statItem.TimeBeforePlanLaunch == null ? string.Empty : statItem.TimeBeforePlanLaunch.Value.ToStringFormat(false));
        //            dataRow.Add(statItem.OneTimePerHoursPlan == null ? string.Empty : statItem.OneTimePerHoursPlan.Value.ToString());
        //            dataRow.Add(statItem.OneTimePerHoursFact == null ? string.Empty : statItem.OneTimePerHoursFact.Value.ToStringFormat(1));
        //            dataRow.Add(statItem.LastSuccessLaunchFactTime == null ? string.Empty : statItem.LastSuccessLaunchFactTime.Value.ToString("yyyy.MM.dd HH:mm:ss"));
        //            dataRow.Add(statItem.SuccessLaunchesCount.ToString());
        //            dataRow.Add(statItem.LastErrorLaunchFactTime == null ? string.Empty : statItem.LastErrorLaunchFactTime.Value.ToString("yyyy.MM.dd HH:mm:ss"));
        //            dataRow.Add(statItem.ErrorLaunchesCount.ToString());
        //            dataRow.Add(statItem.AvgSuccessLaunchDurationInMinutes == null ? string.Empty : statItem.AvgSuccessLaunchDurationInMinutes.Value.ToStringFormat(1));
        //            dataRow.Add(statItem.AvgSuccessLaunchObjectsFactCount == null ? string.Empty : statItem.AvgSuccessLaunchObjectsFactCount.Value.ToStringFormat(0));
        //            dataList.Add(dataRow);
        //        }

        //        var textTableBuilder = new TextTableBuilder();
        //        return textTableBuilder.BuildTable(dataList, columnSettings, true, tableTitle);
        //    }
        //    return "Нет заданий и статистики";
        //}

        public string GetStatInfo(List<SchemaWorkAggrStat> statList, int lastDaysToAnalyz)
        {
            if (statList.Any())
            {
                var table = new TableBuilder.TableBuilder();
                var columns = new List<Column>();
                table.Columns = columns;

                columns.AddRange(new[]
                {
                    new Column { Id = "dbid", MaxWidth = 30, Alignment = Alignment.Left },
                    new Column { Id = "username", MaxWidth = 30, Alignment = Alignment.Left },
                    new Column { Id = "plan", MaxWidth = 7, Alignment = Alignment.Center },
                    new Column { Id = "timebeforeplan", MaxWidth = 22, Alignment = Alignment.Right },
                    new Column { Id = "oneinhoursplan", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "oneinhoursfact", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "lastsuccess", MaxWidth = 16, Alignment = Alignment.Center },
                    new Column { Id = "succsesscount", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "lasterror", MaxWidth = 16, Alignment = Alignment.Center },
                    new Column { Id = "errorscount", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgtime", MaxWidth = 9, Alignment = Alignment.Right },
                    new Column { Id = "avgsize", MaxWidth = 9, Alignment = Alignment.Right }
                });

                var headerRows = new List<List<HeaderCell>>();
                table.HeaderRows = headerRows;

                headerRows.Add(new List<HeaderCell>
                {
                    new HeaderCell { Content = $"Статистика за последние {lastDaysToAnalyz} дней ", ColumnId = "dbid", ColSpan = 12 }
                });

                headerRows.Add(new List<HeaderCell>
                {
                    new HeaderCell { Content = "БД", ColumnId = "dbid", RowSpan = 2},
                    new HeaderCell { Content = "Схема", ColumnId = "username", RowSpan = 2 },
                    new HeaderCell { Content = "План", ColumnId = "plan", RowSpan = 2 },
                    new HeaderCell { Content = "Время до планового запуска", ColumnId = "timebeforeplan", RowSpan = 2 },
                    new HeaderCell { Content = "Раз в X часов", ColumnId = "oneinhoursplan", ColSpan = 2},
                    new HeaderCell { Content = "Успешные запуски", ColumnId = "lastsuccess", ColSpan = 2 },
                    new HeaderCell { Content = "Неуспешные запуски", ColumnId = "lasterror", ColSpan = 2 },
                    new HeaderCell { Content = "Среднее время (мин)", ColumnId = "avgtime", RowSpan = 2 },
                    new HeaderCell { Content = "Средний объем", ColumnId = "avgsize", RowSpan = 2 }
                });


                headerRows.Add(new List<HeaderCell>
                {
                    
                    new HeaderCell { Content = "план", ColumnId = "oneinhoursplan" },
                    new HeaderCell { Content = "факт", ColumnId = "oneinhoursfact" },
                    new HeaderCell { Content = "последний", ColumnId = "lastsuccess" },
                    new HeaderCell { Content = "кол-во", ColumnId = "succsesscount" },
                    new HeaderCell { Content = "последний", ColumnId = "lasterror" },
                    new HeaderCell { Content = "кол-во", ColumnId = "errorscount" }
                    
                });

                var dataRows = new List<List<DataCell>>();
                table.DataRows = dataRows;

                foreach (var statItem in statList.OrderBy(c => c.TimeBeforePlanLaunch ?? TimeSpan.MaxValue))
                {
                    var dataRow = new List<DataCell>
                    {
                        new DataCell
                        {
                            Content = statItem.DBId,
                            ColumnId = "dbid"
                        },
                        new DataCell
                        {
                            Content = statItem.UserName,
                            ColumnId = "username"
                        },
                        new DataCell
                        {
                            Content = statItem.IsScheduled ? "да" : "нет",
                            ColumnId = "plan"
                        },
                        new DataCell
                        {
                            Content = statItem.TimeBeforePlanLaunch == null ? string.Empty : statItem.TimeBeforePlanLaunch.Value.ToStringFormat(false),
                            ColumnId = "timebeforeplan"
                        },
                        new DataCell
                        {
                            Content = statItem.OneTimePerHoursPlan == null ? string.Empty : statItem.OneTimePerHoursPlan.Value.ToString(),
                            ColumnId = "oneinhoursplan"
                        },
                        new DataCell
                        {
                            Content = statItem.OneTimePerHoursFact == null ? string.Empty : statItem.OneTimePerHoursFact.Value.ToStringFormat(1),
                            ColumnId = "oneinhoursfact"
                        },
                        new DataCell
                        {
                            Content = statItem.LastSuccessLaunchFactTime == null ? string.Empty : statItem.LastSuccessLaunchFactTime.Value.ToString("yy.MM.dd HH:mm"),
                            ColumnId = "lastsuccess"
                        },
                        new DataCell
                        {
                            Content = statItem.SuccessLaunchesCount.ToString(),
                            ColumnId = "succsesscount"
                        },
                        new DataCell
                        {
                            Content = statItem.LastErrorLaunchFactTime == null ? string.Empty : statItem.LastErrorLaunchFactTime.Value.ToString("yy.MM.dd HH:mm"),
                            ColumnId = "lasterror"
                        },
                        new DataCell
                        {
                            Content = statItem.ErrorLaunchesCount.ToString(),
                            ColumnId = "errorscount"
                        },
                        new DataCell
                        {
                            Content = statItem.AvgSuccessLaunchDurationInMinutes == null ? string.Empty : statItem.AvgSuccessLaunchDurationInMinutes.Value.ToStringFormat(1),
                            ColumnId = "avgtime"
                        },
                        new DataCell
                        {
                            Content = statItem.AvgSuccessLaunchObjectsFactCount == null ? string.Empty : statItem.AvgSuccessLaunchObjectsFactCount.Value.ToStringFormat(0),
                            ColumnId = "avgsize"
                        }
                    };
                    dataRows.Add(dataRow);
                }

                string result = table.ToString();
                return result;
            }
            return "Нет заданий и статистики";
        }

        public string GetStatFullInfo(List<SchemaWorkAggrFullStat> statList, int lastDaysToAnalyz)
        {
            if (statList.Any())
            {
                var table = new TableBuilder.TableBuilder();
                var columns = new List<Column>();
                table.Columns = columns;

                columns.AddRange(new[]
                {
                    new Column { Id = "dbid", MaxWidth = 30, Alignment = Alignment.Left },
                    new Column { Id = "username", MaxWidth = 30, Alignment = Alignment.Left },
                    new Column { Id = "plan", MaxWidth = 7, Alignment = Alignment.Center },
                    new Column { Id = "timebeforeplan", MaxWidth = 22, Alignment = Alignment.Right },
                    new Column { Id = "oneinhoursplan", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "oneinhoursfact7", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "oneinhoursfact30", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "oneinhoursfact90", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "oneinhoursfact", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "lastsuccess", MaxWidth = 16, Alignment = Alignment.Center },
                    new Column { Id = "succsesscount7", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "succsesscount30", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "succsesscount90", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "succsesscount", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "lasterror", MaxWidth = 16, Alignment = Alignment.Center },
                    new Column { Id = "errorscount7", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "errorscount30", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "errorscount90", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "errorscount", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "lastduration", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgtime7", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgtime30", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgtime90", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgtime", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "fromapptime7", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "fromapptime30", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "fromapptime90", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "fromapptime", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "fromalltime7", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "fromalltime30", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "fromalltime90", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "fromalltime", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "lastcommitcount", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgcommitcount7", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgcommitcount30", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgcommitcount90", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgcommitcount", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "lastallcount", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgallcount7", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgallcount30", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgallcount90", MaxWidth = 10, Alignment = Alignment.Right },
                    new Column { Id = "avgallcount", MaxWidth = 10, Alignment = Alignment.Right }
                   
                });

                var headerRows = new List<List<HeaderCell>>();
                table.HeaderRows = headerRows;

                headerRows.Add(new List<HeaderCell>
                {
                    new HeaderCell { Content = $"Статистика за последние {lastDaysToAnalyz} дней ", ColumnId = "dbid", ColSpan = 42 }
                });

                headerRows.Add(new List<HeaderCell>
                {
                    new HeaderCell { Content = "БД", ColumnId = "dbid", RowSpan = 2},
                    new HeaderCell { Content = "Схема", ColumnId = "username", RowSpan = 2 },
                    new HeaderCell { Content = "План", ColumnId = "plan", RowSpan = 2 },
                    new HeaderCell { Content = "Время до планового запуска", ColumnId = "timebeforeplan", RowSpan = 2 },

                    new HeaderCell { Content = "Раз в X часов", ColumnId = "oneinhoursplan", ColSpan = 5},
                    new HeaderCell { Content = "Успешные запуски", ColumnId = "lastsuccess", ColSpan = 5 },
                    new HeaderCell { Content = "Неуспешные запуски", ColumnId = "lasterror", ColSpan = 5 },
                    new HeaderCell { Content = "Среднее время обработки схемы", ColumnId = "lastduration", ColSpan = 5 },
                    new HeaderCell { Content = "Доля времени работы от времени работы приложения", ColumnId = "fromapptime7", ColSpan = 4 },
                    new HeaderCell { Content = "Доля времени работы от всего времени", ColumnId = "fromalltime7", ColSpan = 4 },
                    new HeaderCell { Content = "Среднее количество изменений", ColumnId = "lastcommitcount", ColSpan = 5 },
                    new HeaderCell { Content = "Среднее общее количество", ColumnId = "lastallcount", ColSpan = 5 }
                });


                headerRows.Add(new List<HeaderCell>
                {

                    new HeaderCell { Content = "план", ColumnId = "oneinhoursplan" },

                    new HeaderCell { Content = "за 7 дней", ColumnId = "oneinhoursfact7" },
                    new HeaderCell { Content = "за 30 дней", ColumnId = "oneinhoursfact30" },
                    new HeaderCell { Content = "за 90 дней", ColumnId = "oneinhoursfact90" },
                    new HeaderCell { Content = $"за {lastDaysToAnalyz} дней", ColumnId = "oneinhoursfact" },

                    new HeaderCell { Content = "последн.", ColumnId = "lastsuccess" },
                    new HeaderCell { Content = "за 7 дней", ColumnId = "succsesscount7" },
                    new HeaderCell { Content = "за 30 дней", ColumnId = "succsesscount30" },
                    new HeaderCell { Content = "за 90 дней", ColumnId = "succsesscount90" },
                    new HeaderCell { Content = $"за {lastDaysToAnalyz} дней", ColumnId = "succsesscount" },

                    new HeaderCell { Content = "последн.", ColumnId = "lasterror" },
                    new HeaderCell { Content = "за 7 дней", ColumnId = "errorscount7" },
                    new HeaderCell { Content = "за 30 дней", ColumnId = "errorscount30" },
                    new HeaderCell { Content = "за 90 дней", ColumnId = "errorscount90" },
                    new HeaderCell { Content = $"за {lastDaysToAnalyz} дней", ColumnId = "errorscount" },

                    new HeaderCell { Content = "последн.", ColumnId = "lastduration" },
                    new HeaderCell { Content = "за 7 дней", ColumnId = "avgtime7" },
                    new HeaderCell { Content = "за 30 дней", ColumnId = "avgtime30" },
                    new HeaderCell { Content = "за 90 дней", ColumnId = "avgtime90" },
                    new HeaderCell { Content = $"за {lastDaysToAnalyz} дней", ColumnId = "avgtime" },

                    new HeaderCell { Content = "за 7 дней", ColumnId = "fromapptime7" },
                    new HeaderCell { Content = "за 30 дней", ColumnId = "fromapptime30" },
                    new HeaderCell { Content = "за 90 дней", ColumnId = "fromapptime90" },
                    new HeaderCell { Content = $"за {lastDaysToAnalyz} дней", ColumnId = "fromapptime" },

                    new HeaderCell { Content = "за 7 дней", ColumnId = "fromalltime7" },
                    new HeaderCell { Content = "за 30 дней", ColumnId = "fromalltime30" },
                    new HeaderCell { Content = "за 90 дней", ColumnId = "fromalltime90" },
                    new HeaderCell { Content = $"за {lastDaysToAnalyz} дней", ColumnId = "fromalltime" },

                    new HeaderCell { Content = "последн.", ColumnId = "lastcommitcount" },
                    new HeaderCell { Content = "за 7 дней", ColumnId = "avgcommitcount7" },
                    new HeaderCell { Content = "за 30 дней", ColumnId = "avgcommitcount30" },
                    new HeaderCell { Content = "за 90 дней", ColumnId = "avgcommitcount90" },
                    new HeaderCell { Content = $"за {lastDaysToAnalyz} дней", ColumnId = "avgcommitcount" },

                    new HeaderCell { Content = "последн.", ColumnId = "lastallcount" },
                    new HeaderCell { Content = "за 7 дней", ColumnId = "avgallcount7" },
                    new HeaderCell { Content = "за 30 дней", ColumnId = "avgallcount30" },
                    new HeaderCell { Content = "за 90 дней", ColumnId = "avgallcount90" },
                    new HeaderCell { Content = $"за {lastDaysToAnalyz} дней", ColumnId = "avgallcount" }
                });

                var dataRows = new List<List<DataCell>>();
                table.DataRows = dataRows;

                foreach (var statItem in statList.OrderBy(c => c.TimeBeforePlanLaunch ?? TimeSpan.MaxValue))
                {
                    var dataRow = new List<DataCell>
                    {
                        new DataCell
                        {
                            Content = statItem.DBId,
                            ColumnId = "dbid"
                        },
                        new DataCell
                        {
                            Content = statItem.UserName,
                            ColumnId = "username"
                        },
                        new DataCell
                        {
                            Content = statItem.IsScheduled ? "да" : "нет",
                            ColumnId = "plan"
                        },
                        new DataCell
                        {
                            Content = statItem.TimeBeforePlanLaunch == null ? string.Empty : statItem.TimeBeforePlanLaunch.Value.ToStringFormat(false),
                            ColumnId = "timebeforeplan"
                        },
                        new DataCell
                        {
                            Content = statItem.OneTimePerHoursPlan == null ? string.Empty : statItem.OneTimePerHoursPlan.Value.ToString(),
                            ColumnId = "oneinhoursplan"
                        },
                        new DataCell
                        {
                            Content = statItem.OneTimePerHoursFact7 == null ? string.Empty : statItem.OneTimePerHoursFact7.Value.ToStringFormat(1),
                            ColumnId = "oneinhoursfact7"
                        },
                        new DataCell
                        {
                            Content = statItem.OneTimePerHoursFact30 == null ? string.Empty : statItem.OneTimePerHoursFact30.Value.ToStringFormat(1),
                            ColumnId = "oneinhoursfact30"
                        },
                        new DataCell
                        {
                            Content = statItem.OneTimePerHoursFact90 == null ? string.Empty : statItem.OneTimePerHoursFact90.Value.ToStringFormat(1),
                            ColumnId = "oneinhoursfact90"
                        },
                        new DataCell
                        {
                            Content = statItem.OneTimePerHoursFact == null ? string.Empty : statItem.OneTimePerHoursFact.Value.ToStringFormat(1),
                            ColumnId = "oneinhoursfact"
                        },
                        new DataCell
                        {
                            Content = statItem.LastSuccessLaunchFactTime == null ? string.Empty : statItem.LastSuccessLaunchFactTime.Value.ToString("yy.MM.dd HH:mm"),
                            ColumnId = "lastsuccess"
                        },
                        new DataCell
                        {
                            Content = statItem.SuccessLaunchesCount7.ToString(),
                            ColumnId = "succsesscount7"
                        },
                        new DataCell
                        {
                            Content = statItem.SuccessLaunchesCount30.ToString(),
                            ColumnId = "succsesscount30"
                        },
                        new DataCell
                        {
                            Content = statItem.SuccessLaunchesCount90.ToString(),
                            ColumnId = "succsesscount90"
                        },
                        new DataCell
                        {
                            Content = statItem.SuccessLaunchesCount.ToString(),
                            ColumnId = "succsesscount"
                        },
                        new DataCell
                        {
                            Content = statItem.LastErrorLaunchFactTime == null ? string.Empty : statItem.LastErrorLaunchFactTime.Value.ToString("yy.MM.dd HH:mm"),
                            ColumnId = "lasterror"
                        },
                        new DataCell
                        {
                            Content = statItem.ErrorLaunchesCount7.ToString(),
                            ColumnId = "errorscount7"
                        },
                        new DataCell
                        {
                            Content = statItem.ErrorLaunchesCount30.ToString(),
                            ColumnId = "errorscount30"
                        },
                        new DataCell
                        {
                            Content = statItem.ErrorLaunchesCount90.ToString(),
                            ColumnId = "errorscount90"
                        },
                        new DataCell
                        {
                            Content = statItem.ErrorLaunchesCount.ToString(),
                            ColumnId = "errorscount"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "lastduration"
                        },
                        new DataCell
                        {
                            Content = statItem.AvgSuccessLaunchDurationInMinutes7 == null ? string.Empty : statItem.AvgSuccessLaunchDurationInMinutes7.Value.ToStringFormat(1),
                            ColumnId = "avgtime7"
                        },
                        new DataCell
                        {
                            Content = statItem.AvgSuccessLaunchDurationInMinutes30 == null ? string.Empty : statItem.AvgSuccessLaunchDurationInMinutes30.Value.ToStringFormat(1),
                            ColumnId = "avgtime30"
                        },
                        new DataCell
                        {
                            Content = statItem.AvgSuccessLaunchDurationInMinutes90 == null ? string.Empty : statItem.AvgSuccessLaunchDurationInMinutes90.Value.ToStringFormat(1),
                            ColumnId = "avgtime90"
                        },
                        new DataCell
                        {
                            Content = statItem.AvgSuccessLaunchDurationInMinutes == null ? string.Empty : statItem.AvgSuccessLaunchDurationInMinutes.Value.ToStringFormat(1),
                            ColumnId = "avgtime"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "fromapptime7"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "fromapptime30"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "fromapptime90"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "fromapptime"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "fromalltime7"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "fromalltime30"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "fromalltime90"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "fromalltime"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "lastcommitcount"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "avgcommitcount7"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "avgcommitcount30"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "avgcommitcount90"
                        },
                        new DataCell
                        {
                            Content = "-",
                            ColumnId = "avgcommitcount"
                        },
                        new DataCell
                        {
                            Content = statItem.LastSuccessLaunchAllObjectsFactCount == null ? string.Empty : statItem.LastSuccessLaunchAllObjectsFactCount.Value.ToString(),
                            ColumnId = "lastallcount"
                        },
                        new DataCell
                        {
                            Content = statItem.AvgSuccessLaunchAllObjectsFactCount7 == null ? string.Empty : statItem.AvgSuccessLaunchAllObjectsFactCount7.Value.ToStringFormat(0),
                            ColumnId = "avgallcount7"
                        },
                        new DataCell
                        {
                            Content = statItem.AvgSuccessLaunchAllObjectsFactCount30 == null ? string.Empty : statItem.AvgSuccessLaunchAllObjectsFactCount30.Value.ToStringFormat(0),
                            ColumnId = "avgallcount30"
                        },
                        new DataCell
                        {
                            Content = statItem.AvgSuccessLaunchAllObjectsFactCount90 == null ? string.Empty : statItem.AvgSuccessLaunchAllObjectsFactCount90.Value.ToStringFormat(0),
                            ColumnId = "avgallcount90"
                        },
                        new DataCell
                        {
                            Content = statItem.AvgSuccessLaunchAllObjectsFactCount == null ? string.Empty : statItem.AvgSuccessLaunchAllObjectsFactCount.Value.ToStringFormat(0),
                            ColumnId = "avgallcount"
                        }
                    };
                    dataRows.Add(dataRow);
                }

                string result = table.ToString();
                return result;
            }
            return "Нет заданий и статистики";
        }
    }
}
