using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace OracleStructExporter.Core
{
    public class ExportSettingsDetails
    {
        public MaskForFileNames MaskForFileNames { get; set; }
        
        [XmlElement]
        public string SessionTransform { get; set; }
        [XmlIgnore]
        public Dictionary<string, string> SessionTransformC {
            get
            {
                return SessionTransform.SplitToDictionary(";", ":", true);
            }
        }
        [XmlElement]
        public string AddSlashTo { get; set; }
        [XmlIgnore]
        public List<string> AddSlashToC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AddSlashTo))
                    return new List<string>();
                return AddSlashTo.Trim().Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }
        [XmlElement]
        public string SkipGrantOptions { get; set; }
        [XmlIgnore]
        public List<string> SkipGrantOptionsC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SkipGrantOptions))
                    return new List<string>();
                return SkipGrantOptions.Trim().Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }
        [XmlElement]
        public string OrderGrantOptions { get; set; }
        [XmlIgnore]
        public List<string> OrderGrantOptionsC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(OrderGrantOptions))
                    return new List<string>();
                return OrderGrantOptions.Trim().Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }
        [XmlElement]
        public string ObjectTypesToProcess { get; set; }
        [XmlIgnore]
        public List<string> ObjectTypesToProcessC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ObjectTypesToProcess))
                    return new List<string>();
                return ObjectTypesToProcess.Trim().Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }
        [XmlAttribute]
        public bool SetSequencesValuesTo1 { get; set; }
        [XmlAttribute]
        public bool ExtractOnlyDefPart { get; set; }

    }
}