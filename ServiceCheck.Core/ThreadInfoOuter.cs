using System;
using System.Collections.Generic;

namespace ServiceCheck.Core
{
    public class ThreadInfoOuter
    {
        public ThreadInfoOuter()
        {
            StartDateTime = DateTime.Now;
        }
        public DateTime StartDateTime { get; private set; }
        public Connection Connection { get; set; }
        public string DbLink { get; set; }
        public string DBSubfolder { get; set; }
        public List<string> SchemasInclude { get; set; }
        public List<string> SchemasExclude { get; set; }
        public ExportSettings ExportSettings { get; set; }
        public string ProcessId { get; set; }
        //public bool Finished { get; set; }

        //public string CommitName
        //{
        //    get
        //    {
        //        return VcsManager.GetCommitName(StartDateTime, ProcessId);
        //    }
        //}

        public string ProcessSubFolder
        {
            get
            {
                //return ExportSettings.UseProcessesSubFoldersInMain ? CommitName : string.Empty;
                return VcsManager.GetCommitName(StartDateTime, ProcessId);
            }
        }

        public string UserNameSubfolder
        {
            get
            {
                return Connection.UserName.ToUpper();
            }
        }
    }
}
