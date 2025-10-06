using System.Collections.Generic;
using System;
using System.Linq;
using System.Xml.Serialization;

namespace ServiceCheck.Core
{
    public class SchedulerOuterSettings
    {
        [XmlElement]
        public string SysUsersToExlude { get; set; }

        public ConnectionsOuterToProcess ConnectionsOuterToProcess { get; set; }

        [XmlElement]
        public string PathToExportDataTemp { get; set; }
        [XmlElement]
        public string PathToExportDataWithErrors { get; set; }

        public RepoSettings RepoSettings { get; set; }

        public DBLog DBLog { get; set; }

        [XmlElement]
        public int GetStatForLastDays { get; set; }
        [XmlElement]
        public int MinSuccessResultsForStat { get; set; }


        [XmlIgnore]
        public List<string> SysUsersToExludeC
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SysUsersToExlude))
                    return new List<string>();
                return SysUsersToExlude.Trim().Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }
        
    }
}