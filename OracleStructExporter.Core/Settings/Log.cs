using System;
using System.Collections.Generic;

namespace OracleStructExporter.Core
{
    public class Log
    {
        public bool Enabled { get; set; }
        public string ExcludeStageInfo { get; set; }

        public Dictionary<ExportProgressDataStage, ExportProgressDataLevel> ExcludeStageInfoC
        {
            get
            {
                var dict = ExcludeStageInfo.SplitToDictionary(";", ":", true);
                var res = new Dictionary<ExportProgressDataStage, ExportProgressDataLevel>();
                foreach (var key in dict.Keys)
                {
                    ExportProgressDataStage resKey;
                    if (ExportProgressDataStage.TryParse(key, true, out resKey))
                    {
                        
                        if (string.IsNullOrWhiteSpace(dict[key]))
                            res[resKey] = ExportProgressDataLevel.NONE;
                        else
                        {
                            ExportProgressDataLevel resValue;
                            if (ExportProgressDataLevel.TryParse(dict[key], true, out resValue))
                            {
                                res[resKey] = resValue;
                            }
                            else
                            {
                                throw new Exception($"Некорректное значение значения: {dict[key]}");
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"Некорректное значение ключа: {key}");
                    }
                }

                return res;
            }
        }
    }
}