using System;
using System.Collections.Generic;
using System.Linq;

namespace OracleStructExporter.Core
{
    public class SchemaWorkAggrStat
    {
        public string DBId { get; set; }
        public string UserName { get; set; }

        public DateTime? LastSuccessLaunchFactTime { get; set; }
        public DateTime? LastErrorLaunchFactTime { get; set; }
        public DateTime? LastLaunchFactTime { get; set; }

        public double? AvgSuccessLaunchDurationInMinutes { get; set; }
        public double? AvgSuccessLaunchObjectsFactCount { get; set; }

        public int SuccessLaunchesCount { get; set; }
        public int ErrorLaunchesCount { get; set; }

        public bool IsScheduled { get; set; }
        public int? OneTimePerHoursPlan { get; set; }
        public double? OneTimePerHoursFact { get; set; }

        public TimeSpan? TimeBeforePlanLaunch { get; set; }

        public static List<SchemaWorkAggrStat> GetAggrStat(List<SchemaWorkStat> plainStat, List<ConnectionToProcess> scheduledConnections)
        {
            var res = scheduledConnections.
                //Select(c=>new Tuple<string,string>(c.DbId.ToUpper(),c.UserName.ToUpper())).
                //Distinct().
                Select(c => new SchemaWorkAggrStat
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
                    
                    res.Add(new SchemaWorkAggrStat
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

                var allEnded = plainStat.Where(c =>
                    c.DBId == statItem.DBId && c.UserName == statItem.UserName &&
                    c.Level == ExportProgressDataLevel.STAGEENDINFO && c.ErrorsCount!=null).ToList();
                var successEnded = allEnded.Where(c => c.ErrorsCount == 0).ToList();
                var errorsEnded = allEnded.Where(c => c.ErrorsCount > 0).ToList();

                if (successEnded.Any())
                {
                    statItem.LastSuccessLaunchFactTime = successEnded.Max(c => c.EventTime);
                    statItem.SuccessLaunchesCount = successEnded.Count;
                    statItem.AvgSuccessLaunchObjectsFactCount = successEnded.Average(c => c.SchemaObjCountFact);
                    List<TimeSpan> durations = new List<TimeSpan>();
                    foreach (var endItem in successEnded)
                    {
                        var startItem = plainStat.FirstOrDefault(c => c.ProcessId == endItem.ProcessId && c.Level == ExportProgressDataLevel.STAGESTARTINFO);
                        if (startItem!=null)
                            durations.Add(endItem.EventTime-startItem.EventTime);
                    }
                    if (durations.Any())
                        statItem.AvgSuccessLaunchDurationInMinutes = durations.Average(c => c.TotalMinutes);

                    var sinceFirstSuccess = DateTime.Now - successEnded.Min(c => c.EventTime);
                    statItem.OneTimePerHoursFact = sinceFirstSuccess.TotalHours / successEnded.Count;
                }

                if (errorsEnded.Any())
                {
                    statItem.LastErrorLaunchFactTime = errorsEnded.Max(c => c.EventTime);
                    statItem.ErrorLaunchesCount = errorsEnded.Count;
                }

                if (allEnded.Any())
                {
                    statItem.LastLaunchFactTime = allEnded.Max(c => c.EventTime);
                    if (statItem.IsScheduled && statItem.OneTimePerHoursPlan != null)
                    {
                        var nextScheduleTime = statItem.LastLaunchFactTime.Value.AddHours(statItem.OneTimePerHoursPlan.Value);
                        statItem.TimeBeforePlanLaunch = nextScheduleTime - DateTime.Now;
                    }
                }

               

            }


            return res;
        }
    }
}
