using System;
using System.Collections.Generic;
using System.Linq;

namespace OracleStructExporter.Core
{
    public class SchemaWorkAggrFullStat
    {
        private static int Interval7 = 7;
        private static int Interval30 = 30;
        private static int Interval90 = 90;

        //Общие, не привязанные к разбивке по интервалам значения
        public string DBId { get; set; }
        public string UserName { get; set; }
        public bool IsScheduled { get; set; }
        public int? OneTimePerHoursPlan { get; set; }
        public TimeSpan? TimeBeforePlanLaunch { get; set; }

        //Всегда считаем только для максимального интервала
        public DateTime? LastSuccessLaunchFactTime { get; set; }
        public DateTime? LastErrorLaunchFactTime { get; set; }
        public DateTime? LastLaunchFactTime { get; set; }
        public int? LastSuccessLaunchAllObjectsFactCount { get; set; }
        public double? LastSuccessLaunchDuration { get; set; }


        //Считаем в разрезе интервалов
        public double? AvgSuccessLaunchDurationInMinutes7 { get; set; }
        public double? AvgSuccessLaunchDurationInMinutes30 { get; set; }
        public double? AvgSuccessLaunchDurationInMinutes90 { get; set; }
        public double? AvgSuccessLaunchDurationInMinutes { get; set; }

        public int SuccessLaunchesCount7 { get; set; }
        public int SuccessLaunchesCount30 { get; set; }
        public int SuccessLaunchesCount90 { get; set; }
        public int SuccessLaunchesCount { get; set; }

        public int ErrorLaunchesCount7 { get; set; }
        public int ErrorLaunchesCount30 { get; set; }
        public int ErrorLaunchesCount90 { get; set; }
        public int ErrorLaunchesCount { get; set; }

        
        public double? OneTimePerHoursFact7 { get; set; }
        public double? OneTimePerHoursFact30 { get; set; }
        public double? OneTimePerHoursFact90 { get; set; }
        public double? OneTimePerHoursFact { get; set; }

        public double? FromAppTime7 { get; set; }
        public double? FromAppTime30 { get; set; }
        public double? FromAppTime90 { get; set; }
        public double? FromAppTime { get; set; }

        public double? FromAllTime7 { get; set; }
        public double? FromAllTime30 { get; set; }
        public double? FromAllTime90 { get; set; }
        public double? FromAllTime { get; set; }

        public int? LastSuccessLaunchCommitObjectsFactCount { get; set; }
        public double? AvgSuccessLaunchCommitObjectsFactCount7 { get; set; }
        public double? AvgSuccessLaunchCommitObjectsFactCount30 { get; set; }
        public double? AvgSuccessLaunchCommitObjectsFactCount90 { get; set; }
        public double? AvgSuccessLaunchCommitObjectsFactCount { get; set; }

        public double? AvgSuccessLaunchAllObjectsFactCount7 { get; set; }
        public double? AvgSuccessLaunchAllObjectsFactCount30 { get; set; }
        public double? AvgSuccessLaunchAllObjectsFactCount90 { get; set; }
        public double? AvgSuccessLaunchAllObjectsFactCount { get; set; }

        public static List<SchemaWorkAggrFullStat> GetAggrFullStat(List<SchemaWorkStat> plainStat, List<ConnectionToProcess> scheduledConnections)
        {

            var res = scheduledConnections.
                //Select(c=>new Tuple<string,string>(c.DbId.ToUpper(),c.UserName.ToUpper())).
                //Distinct().
                Select(c => new SchemaWorkAggrFullStat
                {
                    DBId = c.DbId.ToUpper(),
                    UserName = c.UserName.ToUpper(),
                    OneTimePerHoursPlan = c.OneSuccessResultPerHours,
                    IsScheduled = c.Enabled
                }).ToList();
            foreach (var item in plainStat.Select(c=>new Tuple<string,string>(c.DBId, c.UserName)).Distinct())
            {
                if (!res.Any(c => c.DBId == item.Item1 && c.UserName == item.Item2))
                {
                    
                    res.Add(new SchemaWorkAggrFullStat
                    {
                        DBId = item.Item1,
                        UserName = item.Item2,
                        IsScheduled = false,
                        OneTimePerHoursPlan = null
                    });
                }
            }

            foreach (var statItem in res)
            {

                FillAggrFullStatItemForInterval(plainStat, statItem, Interval7);
                FillAggrFullStatItemForInterval(plainStat, statItem, Interval30);
                FillAggrFullStatItemForInterval(plainStat, statItem, Interval90);
                FillAggrFullStatItemForInterval(plainStat, statItem, null);
            }

            return res;
        }

        static void FillAggrFullStatItemForInterval(List<SchemaWorkStat> plainStat, SchemaWorkAggrFullStat item, int? interval)
        {
            var now = DateTime.Now;
            var allEnded = plainStat.Where(c =>
                    c.DBId == item.DBId && c.UserName == item.UserName &&
                    c.Level == ExportProgressDataLevel.STAGEENDINFO && 
                    c.ErrorsCount != null &&
                    (interval==null || c.EventTime>= now.AddDays(-interval.Value))).ToList();
            var successEnded = allEnded.Where(c => c.ErrorsCount == 0).ToList();
            var errorsEnded = allEnded.Where(c => c.ErrorsCount > 0).ToList();

            if (successEnded.Any())
            {
                //Общие цифры, если задан максимальный интервал
                if (interval == null)
                {
                    var lastSuccessLaunch = successEnded.OrderByDescending(c => c.EventTime).FirstOrDefault();
                    if (lastSuccessLaunch != null)
                    {
                        item.LastSuccessLaunchFactTime = lastSuccessLaunch.EventTime;
                        item.LastSuccessLaunchAllObjectsFactCount = lastSuccessLaunch.SchemaObjCountFact;
                    }
                    
                }

                if (interval==null) 
                    item.SuccessLaunchesCount = successEnded.Count;
                else if (interval == Interval7)
                    item.SuccessLaunchesCount7 = successEnded.Count;
                else if (interval == Interval30)
                    item.SuccessLaunchesCount30 = successEnded.Count;
                else if (interval == Interval90)
                    item.SuccessLaunchesCount90 = successEnded.Count;

                if (interval == null)
                    item.AvgSuccessLaunchAllObjectsFactCount = successEnded.Average(c => c.SchemaObjCountFact);
                else if (interval == Interval7)
                    item.AvgSuccessLaunchAllObjectsFactCount7 = successEnded.Average(c => c.SchemaObjCountFact);
                else if (interval == Interval30)
                    item.AvgSuccessLaunchAllObjectsFactCount30 = successEnded.Average(c => c.SchemaObjCountFact);
                else if (interval == Interval90)
                    item.AvgSuccessLaunchAllObjectsFactCount90 = successEnded.Average(c => c.SchemaObjCountFact);

                
                List<TimeSpan> durations = new List<TimeSpan>();
                foreach (var endItem in successEnded)
                {
                    var startItem = plainStat.FirstOrDefault(c => c.ProcessId == endItem.ProcessId && c.Level == ExportProgressDataLevel.STAGESTARTINFO);
                    if (startItem != null)
                        durations.Add(endItem.EventTime - startItem.EventTime);
                }

                if (durations.Any())
                {
                    if (interval == null)
                        item.AvgSuccessLaunchDurationInMinutes = durations.Average(c => c.TotalMinutes);
                    else if (interval == Interval7)
                        item.AvgSuccessLaunchDurationInMinutes7 = durations.Average(c => c.TotalMinutes);
                    else if (interval == Interval30)
                        item.AvgSuccessLaunchDurationInMinutes30 = durations.Average(c => c.TotalMinutes);
                    else if (interval == Interval90)
                        item.AvgSuccessLaunchDurationInMinutes90 = durations.Average(c => c.TotalMinutes);
                }
                
                var sinceFirstSuccess = DateTime.Now - successEnded.Min(c => c.EventTime);

                if (durations.Any())
                {
                    if (interval == null)
                        item.OneTimePerHoursFact = sinceFirstSuccess.TotalHours / successEnded.Count;
                    else if (interval == Interval7)
                        item.OneTimePerHoursFact7 = sinceFirstSuccess.TotalHours / successEnded.Count;
                    else if (interval == Interval30)
                        item.OneTimePerHoursFact30 = sinceFirstSuccess.TotalHours / successEnded.Count;
                    else if (interval == Interval90)
                        item.OneTimePerHoursFact90 = sinceFirstSuccess.TotalHours / successEnded.Count;
                }
            }

            if (errorsEnded.Any())
            {
                //Общие цифры, если задан максимальный интервал
                if (interval == null)
                {
                    item.LastErrorLaunchFactTime = errorsEnded.Max(c => c.EventTime);
                }
                if (interval == null)
                    item.ErrorLaunchesCount = errorsEnded.Count;
                else if (interval == Interval7)
                    item.ErrorLaunchesCount7 = errorsEnded.Count;
                else if (interval == Interval30)
                    item.ErrorLaunchesCount30 = errorsEnded.Count;
                else if (interval == Interval90)
                    item.ErrorLaunchesCount90 = errorsEnded.Count;

            }

            if (allEnded.Any())
            {
                //Общие цифры, если задан максимальный интервал
                if (interval == null)
                {
                    item.LastLaunchFactTime = allEnded.Max(c => c.EventTime);
                    if (item.IsScheduled && item.OneTimePerHoursPlan != null)
                    {
                        var nextScheduleTime = item.LastLaunchFactTime.Value.AddHours(item.OneTimePerHoursPlan.Value);
                        item.TimeBeforePlanLaunch = nextScheduleTime - DateTime.Now;
                    }
                }
            }
        }
    }
}
