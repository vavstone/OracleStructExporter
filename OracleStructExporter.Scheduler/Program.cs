using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OracleStructExporter.Core;

namespace OracleStructExporter.Scheduler
{
    class Program
    {
        private static OSESettings _settings;
        private static string _logPrefix;
        private static int _processId;
        
        static void Main(string[] args)
        {
            try
            {
                // Проверка на уже запущенные экземпляры
                if (IsAlreadyRunning())
                {
                    Logger.LogToFile("Другая копия приложения уже запущена", "common_log.txt");
                    return;
                }
                
                // Загрузка настроек
                _settings = SettingsHelper.LoadSettings("OSESettings.xml");
                _logPrefix = _settings.LogSettings.DBLog.DBLogPrefix;
                
                // Выбор заданий для обработки
                var connectionsToProcess = SelectConnectionsToProcess();
                
                if (!connectionsToProcess.Any())
                {
                    Logger.LogToFile("Нет заданий для обработки", "common_log.txt");
                    return;
                }
                
                // Создание записи в таблице PROCESS
                _processId = CreateProcessRecord(connectionsToProcess.Count());
                
                // Запуск обработки в параллельных потоках
                Parallel.ForEach(connectionsToProcess, connection => 
                {
                    ProcessConnection(connection, _processId);
                });
                
                // Обновление времени завершения процесса
                UpdateProcessEndTime(_processId);
            }
            catch (Exception ex)
            {
                Logger.LogToFile($"Критическая ошибка: {ex.Message}", "common_log.txt");
            }
        }
        
        private static bool IsAlreadyRunning()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            return processes.Length > 1;
        }
        
        private static Connection[] SelectConnectionsToProcess()
        {
            // Реализация логики выбора соединений для обработки
            // согласно алгоритму из ТЗ
            return _settings.SchedulerSettings.ConnectionsToProcess.ConnectionToProcess
                .Where(c => c.Enabled && ShouldProcessConnection(c))
                .OrderBy(r => Guid.NewGuid()) // Случайный порядок
                .Take(_settings.SchedulerSettings.ConnectionsToProcess.MaxConnectPerOneProcess)
                .ToArray();
        }
        
        private static int CreateProcessRecord(int connectionsCount)
        {
            using (var conn = new OracleConnection(GetLogConnectionString()))
            {
                conn.Open();
                string query = $"INSERT INTO {_logPrefix}PROCESS (id, connections_to_process, start_time) " +
                               $"VALUES ({_logPrefix}PROCESS_SEQ.NEXTVAL, :connectionsCount, SYSDATE) " +
                               $"RETURNING id INTO :id";
                
                using (var cmd = new OracleCommand(query, conn))
                {
                    cmd.Parameters.Add("connectionsCount", connectionsCount);
                    var idParam = new OracleParameter("id", OracleDbType.Int32, ParameterDirection.Output);
                    cmd.Parameters.Add(idParam);
                    
                    cmd.ExecuteNonQuery();
                    return Convert.ToInt32(idParam.Value);
                }
            }
        }
        
        private static void ProcessConnection(Connection connection, int processId)
        {
            var exporter = new Exporter(_settings);
            exporter.ProgressChanged += (sender, e) => 
            {
                LogProgress(e);
            };
            
            exporter.StartWork(connection, processId);
        }
        
        private static void LogProgress(ExportProgressData data)
        {
            // Логирование в файл
            Logger.LogToFile($"{data.EventTime}: {data.Stage} - {data.Message}", "common_log.txt");
            
            // Логирование в БД
            Logger.LogToDatabase(data, _logPrefix, GetLogConnectionString());
        }
    }
}