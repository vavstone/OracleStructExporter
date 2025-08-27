using System.Collections.Generic;
using System.Linq;

namespace OracleStructExporter.Core
{
    public class ExportSettingsDetails
    {
        public MaskForFileNames MaskForFileNames { get; set; }
        public string SessionTransform { get; set; }
        public Dictionary<string, string> SessionTransformC {
            get
            {
                return SessionTransform.SplitToDictionary(";", ":", true);
            }
        }
        public string AddSlashTo { get; set; }
        public List<string> AddSlashToC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AddSlashTo))
                    return new List<string>();
                return AddSlashTo.Split(';').ToList();
            }
        }
        public string SkipGrantOptions { get; set; }
        public List<string> SkipGrantOptionsC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SkipGrantOptions))
                    return new List<string>();
                return SkipGrantOptions.Split(';').ToList();
            }
        }
        public string OrderGrantOptions { get; set; }
        public List<string> OrderGrantOptionsC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(OrderGrantOptions))
                    return new List<string>();
                return OrderGrantOptions.Split(';').ToList();
            }
        }
        public string ObjectTypesToProcess { get; set; }
        public List<string> ObjectTypesToProcessC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ObjectTypesToProcess))
                    return new List<string>();
                return ObjectTypesToProcess.Split(';').ToList();
            }
        }
        public bool SetSequencesValuesTo1 { get; set; }
        public bool ExtractOnlyDefPart { get; set; }

    }
}