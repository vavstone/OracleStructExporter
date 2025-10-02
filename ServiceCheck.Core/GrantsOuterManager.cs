using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceCheck.Core
{
    public static class GrantsOuterManager
    {
        public static void SaveGrantsForCurrentSchemaAndForPublic(List<GrantAttributes> grants, string vcsFolder, string dbName, string userName)
        {
            
            var grantsListFolder = Path.Combine(vcsFolder, "grants");
            var grantsCurrUserFile = Path.Combine(grantsListFolder, $"{dbName}_{userName}.csv");
            var grantsPublicFile = Path.Combine(grantsListFolder, $"{dbName}_PUBLIC.csv");
            var grantsUnknownFile = Path.Combine(grantsListFolder, $"{dbName}_UNKNOWN_SCHEMA!!!.csv");
            var newCurrUserGrands = new List<GrantAttributes>();
            var newPublicGrands = new List<GrantAttributes>();
            var newUnknownUserGrands = new List<GrantAttributes>();

            foreach (var grant in grants.Where(c=>c.Grantee.ToUpper()==userName.ToUpper()))
            {
                newCurrUserGrands.Add(grant);
            }
            foreach (var grant in grants.Where(c=>c.Grantee.ToUpper()=="PUBLIC"))
            {
                newPublicGrands.Add(grant);
            }
            foreach (var grant in grants.Where(c=>c.Grantee.ToUpper()!=userName.ToUpper() && c.Grantee.ToUpper()!="PUBLIC"))
            {
                newUnknownUserGrands.Add(grant);
            }

            var newData = new List<List<string>>();
            foreach (var grant in newCurrUserGrands)
            {
                var strAr = new List<string>
                {
                    grant.ObjectName, grant.Privilege
                };
                newData.Add(strAr);
            }
            CSVWorker.WriteCsv(newData, ";", grantsCurrUserFile);

            newData.Clear();
            foreach (var grant in newPublicGrands)
            {
                var strAr = new List<string>
                {
                    grant.ObjectName, grant.Privilege
                };
                newData.Add(strAr);
            }
            CSVWorker.WriteCsv(newData, ";", grantsPublicFile);

            newData.Clear();
            //не должны сюда попасть
            foreach (var grant in newUnknownUserGrands)
            {
                var strAr = new List<string>
                {
                    grant.ObjectName, grant.Privilege, grantsCurrUserFile
                };
                newData.Add(strAr);
            }
            CSVWorker.WriteCsv(newData, ";", grantsUnknownFile);
        }
    }
}
