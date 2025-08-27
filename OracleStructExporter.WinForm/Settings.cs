using System.Configuration;

namespace OracleStructExporter
{
    public static class Settings
    {
        public static string SESSION_TRANSFORM
        {
            get
            {
                return ConfigurationManager.AppSettings["SESSION_TRANSFORM"];
            }
        }

        public static string ADD_SLASH_TO
        {
            get
            {
                return ConfigurationManager.AppSettings["ADD_SLASH_TO"];
            }
        }

        public static string SKIP_GRANT_OPTIONS
        {
            get
            {
                return ConfigurationManager.AppSettings["SKIP_GRANT_OPTIONS"];
            }
        }

        public static string ORDER_GRANT_OPTIONS
        {
            get
            {
                return ConfigurationManager.AppSettings["SKIP_GRANT_OPTIONS"];
            }
        }

        public static string ExcludeStageInfo
        {
            get
            {
                return ConfigurationManager.AppSettings["ExcludeStageInfo"];
            }
        }
    }
}
