using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public static class SettingsHelper
    {
        public static OSESettings LoadSettings()
        {

            var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var filePath = Path.Combine(currentDir, "OSESettings.xml");

            string content;
            if (File.Exists(filePath))
            {
                content = File.ReadAllText(filePath);
            }
            else
            {
                var rawFilePath = Path.Combine(currentDir, "rawdata.txt");
                var data = new HData {Value = File.ReadAllText(rawFilePath)};
                content = Processor.Back(data, " ").Value;
            }

            var serializer = new XmlSerializer(typeof(OSESettings));

            //using (var stream = new FileStream(filePath, FileMode.Open))
            //{
            //    var settings = (OSESettings)serializer.Deserialize(stream);
            //    ValidateSettings(settings);
            //    return settings;
            //}

            OSESettings settings;
            using (TextReader reader = new StringReader(content))
            {
                settings = (OSESettings)serializer.Deserialize(reader);
                ValidateSettings(settings);
            }
            return settings;
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