using System;
using System.Text;

namespace ServiceCheck.Core
{
    public static class Processor
    {
        public static HData Back(HData data, string splitter)
        {
            try
            {
                var dataOut = new HData();
                if (!string.IsNullOrWhiteSpace(data.Value))
                {
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
                }
                else
                    dataOut.Value = data.Value;

                return dataOut;
            }
            catch (Exception error)
            {
                throw new Exception($"Ошибка back. Value={data.Value}, splitter={splitter}", error);
            }
        }

        public static HData Direct(HData data, string splitter)
        {
            try
            {
                var dataOut = new HData();
                if (!string.IsNullOrWhiteSpace(data.Value))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(data.Value);
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
                }
                else
                    dataOut.Value = data.Value;
                return dataOut;
            }
            catch (Exception error)
            {
                throw new Exception($"Ошибка кодирования direct. Value={data.Value}, splitter={splitter}", error);
            }
        }
    }
}
