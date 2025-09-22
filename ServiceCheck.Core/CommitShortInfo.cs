namespace ServiceCheck.Core
{
    public class CommitShortInfo
    {
        /// <summary>
        /// В формате yyyy-mm-dd_{сквозной порядковый номер коммита}
        /// </summary>
        public string FolderName { get; set; }
        public int FilesAddOrUpdateCount { get; set; }
        public int FilesDeleteCount { get; set;}
        public bool IsInitial { get; set; }
    }
}
