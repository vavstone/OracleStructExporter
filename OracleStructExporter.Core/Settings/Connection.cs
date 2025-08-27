namespace OracleStructExporter.Core
{
    public class Connection
    {
        internal string DbId { get; set; }
        public string DBIdC
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(DbId)) return DbId;
                return $"{Host}:{Port}/{SID}";
            }
        }

        public string DBIdCForFileSystem
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(DbId)) return DbId;
                return $"{Host.Replace(".", "_")}_{Port}_{SID}";
            }
        }

        public string Host { get; set; }
        public string Port { get; set; }
        public string SID { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }
        internal string PasswordH { get; set; }

        public string PasswordC
        {
            get
            {
                return string.IsNullOrWhiteSpace(Password) ? PasswordH : Password;
            }
        }

        public ExportSettingsDetails ExportSettingsDetails { get; set; }
    }
}