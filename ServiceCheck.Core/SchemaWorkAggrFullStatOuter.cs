using ServiceCheck.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCheck.Core
{
    public class SchemaWorkAggrFullStatOuter
    {
        public static int Interval7 = 7;
        public static int Interval30 = 30;
        public static int Interval90 = 90;

        //Общие, не привязанные к разбивке по интервалам значения
        public string DBId { get; set; }
        public string UserName { get; set; }
        public string DbLink { get; set; }
        //public string DbFolder { get; set; }
        public bool IsScheduled { get; set; }
        public int? OneTimePerHoursPlan { get; set; }
        public TimeSpan? TimeBeforePlanLaunch { get; set; }

        //Всегда считаем только для максимального интервала
        public DateTime? LastSuccessLaunchFactTime { get; set; }
        public DateTime? LastErrorLaunchFactTime { get; set; }
        public DateTime? LastLaunchFactTime { get; set; }
        public int? LastSuccessLaunchAllObjectsFactCount { get; set; }
        public TimeSpan? LastSuccessLaunchDuration { get; set; }


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
        public int? SumSuccessLaunchCommitObjectsFactCount7 { get; set; }
        public int? SumSuccessLaunchCommitObjectsFactCount30 { get; set; }
        public int? SumSuccessLaunchCommitObjectsFactCount90 { get; set; }
        public int? SumSuccessLaunchCommitObjectsFactCount { get; set; }

        public double? AvgSuccessLaunchAllObjectsFactCount7 { get; set; }
        public double? AvgSuccessLaunchAllObjectsFactCount30 { get; set; }
        public double? AvgSuccessLaunchAllObjectsFactCount90 { get; set; }
        public double? AvgSuccessLaunchAllObjectsFactCount { get; set; }

        static SchemaWorkStatOuter GetStartItemAndDuration(SchemaWorkStatOuter endItem, List<SchemaWorkStatOuter> allItems, out TimeSpan? duration)
        {
            duration = default;
            var startItem = allItems.FirstOrDefault(c => c.ProcessId == endItem.ProcessId && c.Level == ExportProgressDataLevel.STAGESTARTINFO);
            if (startItem != null)
                duration = endItem.EventTime - startItem.EventTime;
            return startItem;
        }

        public static List<SchemaWorkAggrFullStatOuter> GetAggrFullStat(List<SchemaWorkStatOuter> plainStat, List<AppWorkStat> workStat, List<CommitStat> commitStat, List<ConnectionOuterToProcess> scheduledConnections, int maxInterval)
        {
            var statList = new List<SchemaWorkAggrFullStatOuter>();
            foreach (var connectionOuterToProcess in scheduledConnections)
            {
                foreach (var dbOuter in connectionOuterToProcess.DbOuter)
                {
                    var item = new SchemaWorkAggrFullStatOuter
                    {
                        DBId = connectionOuterToProcess.DbId.ToUpper(),
                        UserName = connectionOuterToProcess.UserName.ToUpper(),
                        DbLink = dbOuter.DbLink == null?"<NONE>": dbOuter.DbLink.ToUpper(),
                        IsScheduled = dbOuter.Enabled,
                        OneTimePerHoursPlan = dbOuter.OneSuccessResultPerHours
                    };
                    statList.Add(item);
                }
            }

            foreach (var item in plainStat.Select(c=>new SchemaWorkAggrFullStatOuter {DBId = c.DBId, UserName = c.UserName, DbLink = c.DbLink}).Distinct())
            {
                if (!statList.Any(c => c.DBId == item.DBId && c.UserName == item.UserName && c.DbLink==item.DbLink))
                {

                    statList.Add(new SchemaWorkAggrFullStatOuter
                    {
                        DBId = item.DBId,
                        UserName = item.UserName,
                        DbLink = item.DbLink,
                        IsScheduled = false,
                        OneTimePerHoursPlan = null
                    });
                }
            }

            var notInitialCommits = commitStat.Where(c => !c.IsInitial).ToList();

            foreach (var statItem in statList)
            {
                FillAggrFullStatItemForInterval(plainStat, workStat, notInitialCommits, statItem, maxInterval, Interval7);
                FillAggrFullStatItemForInterval(plainStat, workStat, notInitialCommits, statItem, maxInterval, Interval30);
                FillAggrFullStatItemForInterval(plainStat, workStat, notInitialCommits, statItem, maxInterval, Interval90);
                FillAggrFullStatItemForInterval(plainStat, workStat, notInitialCommits, statItem, maxInterval, null);
            }

            var res = statList.
                OrderBy(c => c.IsScheduled ? 0 : 1).
                ThenBy(c => c.TimeBeforePlanLaunch ?? TimeSpan.MinValue).
                ThenBy(c => c.DBId).
                ThenBy(c => c.UserName).
                ThenBy(c=>c.DbLink).
                ToList();

            return res;
        }

        static TimeSpan GetDuration(DateTime forDate, int daysAgo)
        {
            return forDate - forDate.AddDays(-daysAgo);
        }

        static void FillAggrFullStatItemForInterval(List<SchemaWorkStatOuter> plainStat, List<AppWorkStat> workStat, List<CommitStat> commitStat, SchemaWorkAggrFullStatOuter item, int maxInterval, int? interval)
        {
            var now = DateTime.Now;

            var curSchemaPlainStat = plainStat.Where(c => c.DBId == item.DBId && c.UserName == item.UserName && c.DbLink==item.DbLink).ToList();
            var curSchemaPlainStatEnded = curSchemaPlainStat.Where(c => c.Level == ExportProgressDataLevel.STAGEENDINFO).ToList();
            var curSchemaPlainStatEndedInInterval = curSchemaPlainStatEnded
                .Where(c => interval == null || c.EventTime >= now.AddDays(-interval.Value)).ToList();
            var curSchemaPlainStatEndedInIntervalSuccess = curSchemaPlainStatEndedInInterval.Where(c => c.ErrorsCount == null|| c.ErrorsCount == 0).ToList();
            var curSchemaPlainStatEndedInIntervalErrors = curSchemaPlainStatEndedInInterval.Where(c => c.ErrorsCount != null && c.ErrorsCount > 0).ToList();

            var workStatInInterval = workStat.Where(c => interval == null || c.EndTime >= now.AddDays(-interval.Value)).ToList();
            //для упрощения используем дату коммита
            //var commitStatInInterval = commitStat.Where(c => interval == null || c.CommitCommonDate >= now.AddDays(-interval.Value)).ToList();

            if (curSchemaPlainStatEndedInIntervalSuccess.Any())
            {
                //Общие цифры, если задан максимальный интервал
                if (interval == null)
                {
                    var lastSuccessLaunch = curSchemaPlainStatEndedInIntervalSuccess.OrderByDescending(c => c.EventTime).FirstOrDefault();
                    if (lastSuccessLaunch != null)
                    {
                        item.LastSuccessLaunchFactTime = lastSuccessLaunch.EventTime;
                        item.LastSuccessLaunchAllObjectsFactCount = lastSuccessLaunch.SchemaObjCountFact;
                        var startItem = GetStartItemAndDuration(lastSuccessLaunch, curSchemaPlainStat, out var duration);
                        if (duration != null)
                            item.LastSuccessLaunchDuration = duration;
                        if (startItem != null)
                        {
                            var commits = commitStat.Where(c => c.ProcessId == startItem.ProcessId).ToList();
                            if (commits.Any())
                                item.LastSuccessLaunchCommitObjectsFactCount = commits.Sum(c => c.AllCnt);
                            else
                                item.LastSuccessLaunchCommitObjectsFactCount = 0;
                        }
                    }
                }

                if (interval==null) 
                    item.SuccessLaunchesCount = curSchemaPlainStatEndedInIntervalSuccess.Count;
                else if (interval == Interval7)
                    item.SuccessLaunchesCount7 = curSchemaPlainStatEndedInIntervalSuccess.Count;
                else if (interval == Interval30)
                    item.SuccessLaunchesCount30 = curSchemaPlainStatEndedInIntervalSuccess.Count;
                else if (interval == Interval90)
                    item.SuccessLaunchesCount90 = curSchemaPlainStatEndedInIntervalSuccess.Count;

                if (interval == null)
                    item.AvgSuccessLaunchAllObjectsFactCount = curSchemaPlainStatEndedInIntervalSuccess.Average(c => c.SchemaObjCountFact);
                else if (interval == Interval7)
                    item.AvgSuccessLaunchAllObjectsFactCount7 = curSchemaPlainStatEndedInIntervalSuccess.Average(c => c.SchemaObjCountFact);
                else if (interval == Interval30)
                    item.AvgSuccessLaunchAllObjectsFactCount30 = curSchemaPlainStatEndedInIntervalSuccess.Average(c => c.SchemaObjCountFact);
                else if (interval == Interval90)
                    item.AvgSuccessLaunchAllObjectsFactCount90 = curSchemaPlainStatEndedInIntervalSuccess.Average(c => c.SchemaObjCountFact);

                
                List<TimeSpan> durationsSuccess = new List<TimeSpan>();
                List<int> commitsCountList = new List<int>();
                foreach (var endItem in curSchemaPlainStatEndedInIntervalSuccess)
                {
                    var startItem = GetStartItemAndDuration(endItem, curSchemaPlainStat, out var duration);
                    if (duration != null)
                        durationsSuccess.Add(duration.Value);

                    if (startItem != null)
                    {
                        var commits = commitStat.Where(c => c.ProcessId == startItem.ProcessId).ToList();
                        if (commits.Any())
                            commitsCountList.Add(commits.Sum(c => c.AllCnt));
                        else
                            commitsCountList.Add(0);
                    }
                }

                if (commitsCountList.Any())
                {
                    if (interval == null)
                        item.SumSuccessLaunchCommitObjectsFactCount = commitsCountList.Sum();
                    else if (interval == Interval7)
                        item.SumSuccessLaunchCommitObjectsFactCount7 = commitsCountList.Sum();
                    else if (interval == Interval30)
                        item.SumSuccessLaunchCommitObjectsFactCount30 = commitsCountList.Sum();
                    else if (interval == Interval90)
                        item.SumSuccessLaunchCommitObjectsFactCount90 = commitsCountList.Sum();
                }


                var durationsAppWork = new List<TimeSpan>();
                foreach (var appWorkStat in workStatInInterval)
                {
                    durationsAppWork.Add(appWorkStat.EndTime - appWorkStat.StartTime);
                }

                double? durationAppWorkSumInMilliseconds = null;
                if (durationsAppWork.Any())
                    durationAppWorkSumInMilliseconds = durationsAppWork.Sum(c => c.TotalMilliseconds);

                if (durationsSuccess.Any())
                {
                    var intervalToCheck = interval ?? maxInterval;
                    if (interval == null)
                    {
                        item.AvgSuccessLaunchDurationInMinutes = durationsSuccess.Average(c => c.TotalMinutes);
                        item.FromAllTime = durationsSuccess.Sum(c =>
                            (c.TotalMilliseconds) / GetDuration(now, intervalToCheck).TotalMilliseconds) * 100;
                        if (durationAppWorkSumInMilliseconds != null && durationAppWorkSumInMilliseconds.Value > 0)
                            item.FromAppTime = durationsSuccess.Sum(c => c.TotalMilliseconds) / durationAppWorkSumInMilliseconds * 100;
                    }
                    else if (interval == Interval7)
                    {
                        item.AvgSuccessLaunchDurationInMinutes7 = durationsSuccess.Average(c => c.TotalMinutes);
                        item.FromAllTime7 = durationsSuccess.Sum(c =>
                            (c.TotalMilliseconds) / GetDuration(now, intervalToCheck).TotalMilliseconds) * 100;
                        if (durationAppWorkSumInMilliseconds != null && durationAppWorkSumInMilliseconds.Value > 0)
                            item.FromAppTime7 = durationsSuccess.Sum(c => c.TotalMilliseconds) / durationAppWorkSumInMilliseconds * 100;
                    }
                    else if (interval == Interval30)
                    {
                        item.AvgSuccessLaunchDurationInMinutes30 = durationsSuccess.Average(c => c.TotalMinutes);
                        item.FromAllTime30 = durationsSuccess.Sum(c =>
                            (c.TotalMilliseconds) / GetDuration(now, intervalToCheck).TotalMilliseconds) * 100;
                        if (durationAppWorkSumInMilliseconds != null && durationAppWorkSumInMilliseconds.Value > 0)
                            item.FromAppTime30 = durationsSuccess.Sum(c => c.TotalMilliseconds) / durationAppWorkSumInMilliseconds * 100;
                    }
                    else if (interval == Interval90)
                    {
                        item.AvgSuccessLaunchDurationInMinutes90 = durationsSuccess.Average(c => c.TotalMinutes);
                        item.FromAllTime90 = durationsSuccess.Sum(c =>
                            (c.TotalMilliseconds) / GetDuration(now, intervalToCheck).TotalMilliseconds) * 100;
                        if (durationAppWorkSumInMilliseconds != null && durationAppWorkSumInMilliseconds.Value > 0)
                            item.FromAppTime90 = durationsSuccess.Sum(c => c.TotalMilliseconds) / durationAppWorkSumInMilliseconds * 100;
                    }
                }
                
                var sinceFirstSuccess = DateTime.Now - curSchemaPlainStatEndedInIntervalSuccess.Min(c => c.EventTime);

                //if (durations.Any())
                //{
                    if (interval == null)
                        item.OneTimePerHoursFact = sinceFirstSuccess.TotalHours / curSchemaPlainStatEndedInIntervalSuccess.Count;
                    else if (interval == Interval7)
                        item.OneTimePerHoursFact7 = sinceFirstSuccess.TotalHours / curSchemaPlainStatEndedInIntervalSuccess.Count;
                    else if (interval == Interval30)
                        item.OneTimePerHoursFact30 = sinceFirstSuccess.TotalHours / curSchemaPlainStatEndedInIntervalSuccess.Count;
                    else if (interval == Interval90)
                        item.OneTimePerHoursFact90 = sinceFirstSuccess.TotalHours / curSchemaPlainStatEndedInIntervalSuccess.Count;
                //}
            }

            if (curSchemaPlainStatEndedInIntervalErrors.Any())
            {
                //Общие цифры, если задан максимальный интервал
                if (interval == null)
                {
                    item.LastErrorLaunchFactTime = curSchemaPlainStatEndedInIntervalErrors.Max(c => c.EventTime);
                }
                if (interval == null)
                    item.ErrorLaunchesCount = curSchemaPlainStatEndedInIntervalErrors.Count;
                else if (interval == Interval7)
                    item.ErrorLaunchesCount7 = curSchemaPlainStatEndedInIntervalErrors.Count;
                else if (interval == Interval30)
                    item.ErrorLaunchesCount30 = curSchemaPlainStatEndedInIntervalErrors.Count;
                else if (interval == Interval90)
                    item.ErrorLaunchesCount90 = curSchemaPlainStatEndedInIntervalErrors.Count;

            }

            if (curSchemaPlainStatEndedInInterval.Any())
            {
                //Общие цифры, если задан максимальный интервал
                if (interval == null)
                {
                    item.LastLaunchFactTime = curSchemaPlainStatEndedInInterval.Max(c => c.EventTime);
                    if (item.IsScheduled && item.OneTimePerHoursPlan != null)
                    {
                        var nextScheduleTime = item.LastLaunchFactTime.Value.AddHours(item.OneTimePerHoursPlan.Value);
                        item.TimeBeforePlanLaunch = nextScheduleTime - DateTime.Now;

                        //если последней была ошибка, сокращаем интервал вдвое
                        if (item.LastErrorLaunchFactTime != null && item.LastSuccessLaunchFactTime != null &&
                            item.LastErrorLaunchFactTime > item.LastSuccessLaunchFactTime)
                        {
                            item.TimeBeforePlanLaunch = item.TimeBeforePlanLaunch.Value.DivideBy(2);
                        }
                    }
                }
            }
        }
    }
}
