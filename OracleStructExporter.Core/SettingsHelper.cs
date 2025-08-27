using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public static class SettingsHelper
    {
        public static OSESettings LoadSettings(string filePath)
        {
            var serializer = new XmlSerializer(typeof(OSESettings));
            
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                var settings = (OSESettings)serializer.Deserialize(stream);
                ValidateSettings(settings);
                return settings;
            }
        }
        
        public static void ValidateSettings(OSESettings settings)
        {
            // ФЛК: проверка на повторяющиеся Connection
            var connections = settings.Connections;
            var duplicates = connections
                .GroupBy(c => new { c.DbId, c.Host, c.Port, c.SID, c.UserName })
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
                
            if (duplicates.Any())
            {
                throw new Exception("Найдены повторяющиеся Connection в настройках");
            }
            
            // Дополнительные проверки...
        }
    }
}