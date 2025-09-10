using System;
using System.Collections.Generic;
using System.Linq;

namespace OracleStructExporter.Core
{
    public class RepoChangeDbSchemaGroupInfo
    {
        public string DBId { get; set; }
        public string UserName { get; set; }
        public List<RepoChangeCommitGroupInfo> CommitsList { get; set; } = new List<RepoChangeCommitGroupInfo>();

        public int ChangesCount
        {
            get
            {
                return CommitsList.Sum(c => c.ChangesCount);
            }
        }
        public DateTime? FirstModificationTime
        {
            get
            {
                if (CommitsList.Any())
                    return CommitsList.Min(c => c.FirstModificationTime);
                return null;
            }
        }
        public DateTime? LastModificationTime
        {
            get
            {
                if (CommitsList.Any())
                    return CommitsList.Max(c => c.LastModificationTime);
                return null;
            }
        }
    }
}