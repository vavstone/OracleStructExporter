using System;

namespace OracleStructExporter.Core
{
    public class CommitStat
    {
        public int ProcessId { get; set; }
        public DateTime CommitCommonDate { get; set; }
        public string DBId { get; set; }
        public string UserName { get; set; }
        public bool IsInitial { get; set; }
        public int AllAddCnt { get; set; }
        public int AllUpdCnt { get; set; }
        public int AllDelCnt { get; set; }
        public int AllAddSize { get; set; }
        public int AllUpdSize { get; set; }
        public int AllDelSize { get; set; }

        public int AllCnt
        {
            get
            {
                return AllAddCnt + AllUpdCnt + AllDelCnt;
            }
        }

        /// <summary>
        /// Не включен размер удаленных файлов
        /// </summary>
        public int AllSize
        {
            get
            {
                return AllAddSize + AllUpdCnt;
            }
        }
    }
}
