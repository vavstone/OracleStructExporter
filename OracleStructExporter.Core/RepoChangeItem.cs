using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleStructExporter.Core
{
    public class RepoChangeItem
    {
        public string FileName { get; set; }
        //public string FilePathRelativeToRepoFolder { get; set; }
        public OracleObjectType ObjectType { get; set; }
        public RepoOperation Operation { get; set; }
        public string DBId { get; set; }
        public string UserName { get; set; }
        public int ProcessId { get; set; }
        public DateTime CommitCommonDate { get; set; }
        public DateTime CommitCurFileTime { get; set; }
        public bool IsInitial { get; set; }
        public long FileSize { get; set; }
    }
}
