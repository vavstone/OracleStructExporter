using OracleStructExporter.Core.Settings;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace OracleStructExporter.Core
{
    public class Logger
    {
        LogSettings _logSettings;
        //Connection _connection;

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

        public void InsertThreadsDBLog(ExportProgressData progressData, bool checkOnNecessaryBySettings)
        {
            //TODO реализовать
        }
    }
}
