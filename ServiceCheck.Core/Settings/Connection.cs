using System.Xml.Serialization;
namespace ServiceCheck.Core
{
    public class Connection
    {
		[XmlAttribute]
        public string DbId { get; set; }
        [XmlIgnore]
        public string DBIdC
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(DbId)) return DbId;
                return $"{Host}:{Port}/{SID}";
            }
        }
        [XmlIgnore]
        public string DBIdCForFileSystem
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(DbId)) return DbId;
                return $"{Host.Replace(".", "_")}_{Port}_{SID}";
            }
        }
		[XmlAttribute]
        public string Host { get; set; }
		[XmlAttribute]
        public string Port { get; set; }
		[XmlAttribute]
        public string SID { get; set; }

		[XmlAttribute]
        public string UserName { get; set; }
		[XmlAttribute]
        public string Password { get; set; }
        [XmlIgnore]
        public string PasswordH { get; set; }
        [XmlIgnore]
        public string PasswordC
        {
            get
            {
                return string.IsNullOrWhiteSpace(Password) ? PasswordH : Password;
            }
        }

        //TODO доработать для Scheduler
        public ExportSettingsDetails ExportSettingsDetails { get; set; }

        public string UserNameAndDBIdC
        {
            get
            {
                return $"{UserName.ToUpper()}@{DBIdC}";
            }
        }
    }
}