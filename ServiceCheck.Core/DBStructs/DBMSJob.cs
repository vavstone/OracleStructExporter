using System;
using System.Globalization;

namespace ServiceCheck.Core.DBStructs
{
    public class DBMSJob
    {
        public int Job { get; set; }
        public string What { get; set; }
        internal DateTime NextDate { get; set; }
        internal string NextSec { get; set; }
        public string Interval { get; set; }

        public DateTime NextTime
        {
            get
            {
                DateTime nextSecTime;
                if (DateTime.TryParseExact(NextSec, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out nextSecTime))
                {
                    if (nextSecTime.Hour!=0 && nextSecTime.Minute != 0 && nextSecTime.Second != 0)
                        return new DateTime(NextDate.Year, NextDate.Month, NextDate.Day, nextSecTime.Hour, nextSecTime.Minute, nextSecTime.Second);
                }
                return NextDate;
            }
        }
    }
}
