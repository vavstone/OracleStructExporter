using System;
using System.Collections.Generic;
using System.Linq;

namespace OracleStructExporter.Core
{
    public class ProgressDataManager
    {
        internal List<ExportProgressData> ProgressDataList { get; set; } = new List<ExportProgressData>();
        IProgress<ExportProgressData> _progressReporter;
        private string _processId;
        private Connection _currentConnection;

        internal List<ProgressDataManager> ChildProgressManagers { get; set; } = new List<ProgressDataManager>();

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
                    .FirstOrDefault(c => c.Stage == ExportProgressDataStage.GET_OBJECTS_NAMES &&
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
                    .FirstOrDefault(c => c.Stage == ExportProgressDataStage.PROCESS_SCHEMA &&
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


        public ProgressDataManager(IProgress<ExportProgressData> progressReporter, string processId, Connection currentConnection)
        {
            _progressReporter = progressReporter;
            _processId = processId;
            _currentConnection = currentConnection;
        }

        public void ReportCurrentProgress(ExportProgressData progressData)
        {
            //var progressData = new ExportProgressData(level, stage, objectName, current, totalObjects, /*threadFinished,*/ textAddInfo, objectNumAddInfo, _processId, _currentConnection, error, errorDetails);
            progressData.ProcessId = _processId;
            progressData.CurrentConnection = _currentConnection;
            if (progressData.Level == ExportProgressDataLevel.STAGEENDINFO)
            {
                var startItem = ProgressDataList.FirstOrDefault(c =>
                    c.Level == ExportProgressDataLevel.STAGESTARTINFO &&
                    c.Stage == progressData.Stage &&
                    (string.IsNullOrWhiteSpace(progressData.ObjectName) || c.ObjectName == progressData.ObjectName));
                if (startItem != null)
                {
                    progressData.StartStageProgressData = startItem;
                }

                //if (progressData.Stage == ExportProgressDataStage.PROCESS_MAIN)
                //{
                //    //TODO cумма по ошибкам всей выгрузки
                //    //progressData.ErrorsCount =  
                //}
                //if (progressData.Stage == ExportProgressDataStage.PROCESS_SCHEMA)
                //{
                //    //TODO cумма по ошибкам выгрузки схемы
                //    //progressData.ErrorsCount =  
                //}
                //if (progressData.Stage == ExportProgressDataStage.PROCESS_OBJECT_TYPE)
                //{
                //    //TODO cумма по ошибкам выгрузки типа
                //    //progressData.ErrorsCount =  
                //}
                //if (progressData.Stage == ExportProgressDataStage.PROCESS_OBJECT)
                //{
                //    //TODO cумма по ошибкам выгрузки объекта
                //    //progressData.ErrorsCount =  
                //}
            }
            ProgressDataList.Add(progressData);
            _progressReporter?.Report(progressData);
        }

        //public void ReportCurrentProgressOld(ExportProgressDataLevel level, ExportProgressDataStage stage, string objectName, int current, int totalObjects, /*bool threadFinished,*/ string textAddInfo, int objectNumAddInfo, string error, string errorDetails)
        //{
        //    var item = new ExportProgressData(level, stage, objectName, current, totalObjects, /*threadFinished,*/ textAddInfo, objectNumAddInfo, _processId, _currentConnection, error, errorDetails);
        //    if (level == ExportProgressDataLevel.STAGEENDINFO)
        //    {
        //        var startItem = _progressDataList.FirstOrDefault(c =>
        //            c.Level == ExportProgressDataLevel.STAGESTARTINFO &&
        //            c.Stage == stage &&
        //            (string.IsNullOrWhiteSpace(objectName) || c.ObjectName == objectName));
        //        if (startItem != null)
        //        {
        //            item.StartStageProgressData = startItem;
        //        }

        //        if (stage == ExportProgressDataStage.PROCESS_SCHEMA)
        //        {
        //            //cумма по ошибкам всей выгрузки
        //            item.AllProcessErrorsCount = ErrorsCount;
        //        }
        //    }

        //    //if (level == ExportProgressDataLevel.CANCEL)
        //    //{
        //    //    var k = stage;
        //    //}
        //    _progressDataList.Add(item);
        //    _progressReporter?.Report(item);
        //}
    }
}