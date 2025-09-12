using System;
using System.Globalization;

namespace TextProc.Core
{
    public class HData
    {
        public string  Value { get; set; }

        public DateTime? GetDateFromValue(string splitter)
        {
            var dateStrLength = splitter.Length * 2 + 6;
            if (!string.IsNullOrEmpty(Value) && Value.Length >= dateStrLength)
            {
                var dateStr = Value.Substring(Value.Length - dateStrLength);
                if (TryParseShortDate(dateStr, splitter, out DateTime date))
                    return date;
            }
            return null;
        }

        public string GetCleanValue(string splitter)
        {
            try
            {
                var dateStrLength = splitter.Length * 2 + 6;
                return Value.Substring(0, Value.Length - dateStrLength);
            }
            catch (Exception error)
            {
                throw new Exception($"Ошибка получения чистого значения объекта класса Data. Value={Value}, splitter={splitter}", error);
            }
        }

        public void AddDateToValue(DateTime date, string splitter)
        {
            Value += splitter + DateToShortString(date, splitter);
        }

        public int GetKey(DateTime date)
        {
            return (int)date.DayOfWeek;
        }

        public static bool TryParseShortDate(string dateString, string splitter, out DateTime result)
        {
            return DateTime.TryParseExact(
                dateString,
                $"yy{splitter}MM{splitter}dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result
            );
        }

        public static string DateToShortString(DateTime date, string splitter)
        {
            return date.ToString($"yy{splitter}MM{splitter}dd");
        }
    }
}
