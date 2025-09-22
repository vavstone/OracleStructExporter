using System;

namespace ServiceCheck.Core
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
        //если MaskWorked=true, фактически файлы разные, но за счет сравнения по маске считаем их одинаковыми, и используем только для формирования лога
        public bool MaskWorked { get; set; }
    }
}
