using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCheck.Core
{
    public class RepoChangeCommitGroupInfo
    {
        public DateTime CommitCommonDate { get; set; }
        public int ProcessId { get; set; }
        public bool IsInitial { get; set; }
        public List<RepoChangeObjAndOperGroupInfo> OperationsList { get; set; } = new List<RepoChangeObjAndOperGroupInfo>();

        public int ChangesCount
        {
            get
            {
                return OperationsList.Sum(c => c.ChangesCount);
            }
        }
        public DateTime? FirstModificationTime
        {
            get
            {
                if (OperationsList.Any())
                    return OperationsList.Min(c => c.FirstModificationTime);
                return null;
            }
        }
        public DateTime? LastModificationTime
        {
            get
            {
                if (OperationsList.Any())
                    return OperationsList.Max(c => c.LastModificationTime);
                return null;
            }
        }
    }
}