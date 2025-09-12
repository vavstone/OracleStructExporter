using System;

namespace ServiceCheck.Core
{
    public class RepoChangeObjAndOperGroupInfo
    {
        public OracleObjectType ObjectType { get; set; }
        public RepoOperation Operation { get; set; }
        public int ChangesCount { get; set; }
        public long FilesSize { get; set; }
        public DateTime? FirstModificationTime { get; set; }
        public DateTime? LastModificationTime { get; set; }
    }
}