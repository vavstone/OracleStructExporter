using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class ExportSettingsDetails
    {
        [XmlElement]
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
        public GetPartitionMode GetPartitionMode { get; set; }


        public static ExportSettingsDetails GetSumExportSettingsDetails(ExportSettingsDetails lowPrioritySettings, ExportSettingsDetails highPrioritySettings)
        {
            var res = new ExportSettingsDetails();
            if (lowPrioritySettings != null)
            {
                if (lowPrioritySettings.MaskForFileNames!=null)
                    res.MaskForFileNames = lowPrioritySettings.MaskForFileNames;
                if (!string.IsNullOrWhiteSpace(lowPrioritySettings.SessionTransform))
                    res.SessionTransform = lowPrioritySettings.SessionTransform;
                if (!string.IsNullOrWhiteSpace(lowPrioritySettings.AddSlashTo))
                    res.AddSlashTo = lowPrioritySettings.AddSlashTo;
                if (!string.IsNullOrWhiteSpace(lowPrioritySettings.SkipGrantOptions))
                    res.SkipGrantOptions = lowPrioritySettings.SkipGrantOptions;
                if (!string.IsNullOrWhiteSpace(lowPrioritySettings.OrderGrantOptions))
                    res.OrderGrantOptions = lowPrioritySettings.OrderGrantOptions;
                if (!string.IsNullOrWhiteSpace(lowPrioritySettings.ObjectTypesToProcess))
                    res.ObjectTypesToProcess = lowPrioritySettings.ObjectTypesToProcess;
                res.SetSequencesValuesTo1 = lowPrioritySettings.SetSequencesValuesTo1;
                res.GetPartitionMode = lowPrioritySettings.GetPartitionMode;
            }
            if (highPrioritySettings != null)
            {
                if (highPrioritySettings.MaskForFileNames!=null)
                    res.MaskForFileNames = highPrioritySettings.MaskForFileNames;
                if (!string.IsNullOrWhiteSpace(highPrioritySettings.SessionTransform))
                    res.SessionTransform = highPrioritySettings.SessionTransform;
                if (!string.IsNullOrWhiteSpace(highPrioritySettings.AddSlashTo))
                    res.AddSlashTo = highPrioritySettings.AddSlashTo;
                if (!string.IsNullOrWhiteSpace(highPrioritySettings.SkipGrantOptions))
                    res.SkipGrantOptions = highPrioritySettings.SkipGrantOptions;
                if (!string.IsNullOrWhiteSpace(highPrioritySettings.OrderGrantOptions))
                    res.OrderGrantOptions = highPrioritySettings.OrderGrantOptions;
                if (!string.IsNullOrWhiteSpace(highPrioritySettings.ObjectTypesToProcess))
                    res.ObjectTypesToProcess = highPrioritySettings.ObjectTypesToProcess;
                res.SetSequencesValuesTo1 = highPrioritySettings.SetSequencesValuesTo1;
                res.GetPartitionMode = highPrioritySettings.GetPartitionMode;
            }
            return res;

        }

    }
}