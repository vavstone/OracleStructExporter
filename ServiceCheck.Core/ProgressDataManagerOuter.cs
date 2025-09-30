using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceCheck.Core
{
    public class ProgressDataManagerOuter
    {
        internal List<ExportProgressDataOuter> ProgressDataList { get; set; } = new List<ExportProgressDataOuter>();
        IProgress<ExportProgressDataOuter> _progressReporter;
        private string _processId;
        public Connection Connection;

        internal List<ProgressDataManagerOuter> ChildProgressManagers { get; set; } = new List<ProgressDataManagerOuter>();

        public void SetProcessId(string id)
        {
            _processId = id;
        }

        public int CurrentThreadErrorsCount
        {
            get
            {
                return ProgressDataList.Where(c => c.Level == ExportProgressDataLevel.ERROR).Count();
            }
        }

        public int ChildThreadsErrorsCount
        {
            get
            {
                var errCount = 0;
                if (ChildProgressManagers.Any())
                    errCount += ChildProgressManagers.Sum(c => c.CurrentThreadErrorsCount);
                return errCount;
            }
        }

        public int AllErrorsCount
        {
            get
            {
                return CurrentThreadErrorsCount + ChildThreadsErrorsCount;
            }
        }

        int CurrentThreadSchemaObjCountPlan
        {
            get
            {
                var getObjectsNamesEnd = ProgressDataList
                    .FirstOrDefault(c => c.Stage == ExportProgressDataStageOuter.GET_OBJECTS_NAMES &&
                                         c.Level == ExportProgressDataLevel.STAGEENDINFO);
                if (getObjectsNamesEnd != null)
                    return getObjectsNamesEnd.SchemaObjCountPlan??0;
                return 0;
            }
        }

        public int ChildThreadsSchemaObjCountPlan
        {
            get
            {
                return ChildProgressManagers.Sum(c => c.CurrentThreadSchemaObjCountPlan);
            }
        }

        int CurrentThreadSchemaObjCountFact
        {
            get
            {
                var processSchemaEnd = ProgressDataList
                    .FirstOrDefault(c => c.Stage == ExportProgressDataStageOuter.PROCESS_SCHEMA &&
                                         c.Level == ExportProgressDataLevel.STAGEENDINFO);
                if (processSchemaEnd != null)
                    return processSchemaEnd.SchemaObjCountFact??0;
                return 0;
            }
        }

        public int ChildThreadsSchemaObjCountFact
        {
            get
            {
                return ChildProgressManagers.Sum(c => c.CurrentThreadSchemaObjCountFact);
            }
        }


        public ProgressDataManagerOuter(IProgress<ExportProgressDataOuter> progressReporter)
        {
            _progressReporter = progressReporter;
            
        }

        public void SetSchedulerProps(string processId, Connection currentConnection)
        {
            _processId = processId;
            Connection = currentConnection;
        }

        public void ReportCurrentProgress(ExportProgressDataOuter progressData)
        {
            progressData.ProcessId = _processId;
            progressData.CurrentConnection = Connection;
            if (progressData.Level == ExportProgressDataLevel.STAGEENDINFO)
            {
                var startItem = ProgressDataList.FirstOrDefault(c =>
                    c.Level == ExportProgressDataLevel.STAGESTARTINFO &&
                    c.Stage == progressData.Stage &&
                    (string.IsNullOrWhiteSpace(progressData.ObjectName) || c.ObjectName == progressData.ObjectName) &&
                    (string.IsNullOrWhiteSpace(progressData.ObjectType) || c.ObjectType == progressData.ObjectType));
                if (startItem != null)
                {
                    progressData.StartStageProgressData = startItem;
                }
            }
            ProgressDataList.Add(progressData);
            _progressReporter?.Report(progressData);
        }
    }
}