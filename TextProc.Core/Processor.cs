using System;
using System.Text;

namespace TextProc.Core
{
    public static class Processor
    {
        public static HData Back(HData data, string splitter)
        {
            try
            {
                var dataOut = new HData();
                var date = data.GetDateFromValue(splitter);
                var cleanValue = data.GetCleanValue(splitter);
                var strBytes = cleanValue.Split(new[] {splitter}, StringSplitOptions.RemoveEmptyEntries);

                var keyData = dataOut.GetKey(date.Value);
                var decBytes = new byte[strBytes.Length];

                for (var i = 0; i < strBytes.Length; i++)
                {
                    decBytes[i] = (byte) (Convert.ToByte(strBytes[i]) ^ keyData);
                }

                dataOut.Value = Encoding.UTF8.GetString(decBytes);
                dataOut.Value = dataOut.Value;

                return dataOut;
            }
            catch (Exception error)
            {
                throw new Exception($"Ошибка декодирования Data. Value={data.Value}, splitter={splitter}", error);
            }
        }

        public static HData Direct(HData data, string splitter)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data.Value);
                var dataOut = new HData();
                var date = DateTime.Now;
                var keyData = dataOut.GetKey(date);

                for (int i = 0; i < bytes.Length; i++)
                {
                    dataOut.Value += bytes[i] ^ keyData;
                    if (i < bytes.Length - 1)
                        dataOut.Value += splitter;
                }

                dataOut.Value = dataOut.Value;
                dataOut.AddDateToValue(date, splitter);

                return dataOut;
            }
            catch (Exception error)
            {
                throw new Exception($"Ошибка кодирования Data. Value={data.Value}, splitter={splitter}", error);
            }
        }
    }
}
