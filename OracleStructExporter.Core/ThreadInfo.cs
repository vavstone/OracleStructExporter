using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace OracleStructExporter.Core
{
    public class ThreadInfo
    {
        public ThreadInfo()
        {
            StartDateTime = DateTime.Now;
        }
        public DateTime StartDateTime { get; private set; }
        public Connection Connection { get; set; }
        public ExportSettings ExportSettings { get; set; }
        public string ProcessId { get; set; }
        public bool Finished { get; set; }

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

        public string DBSubfolder
        {
            get
            {
                return Connection.DBIdCForFileSystem.ToUpper();
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
