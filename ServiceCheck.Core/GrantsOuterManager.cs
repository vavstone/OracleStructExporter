using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceCheck.Core
{
    public static class GrantsOuterManager
    {
        public static void UpdateGrantsForCurrentSchema(List<GrantAttributes> grants, string vcsFolder, string dbName, string userName)
        {
            
            var grantsListFolder = Path.Combine(vcsFolder, "grants");
            var grantsFile = Path.Combine(grantsListFolder, $"{dbName}_{userName}.csv");
            var oldAllGrands = new List<GrantAttributes>();
            var newAllGrands = new List<GrantAttributes>();
            if (File.Exists(grantsFile))
            {
                var data = CSVWorker.ReadCsv(grantsFile, ";");
                //TODO 
            }

            foreach (var oldGrand in oldAllGrands.Where(c => c.Grantee != userName))
            {
                newAllGrands.Add(oldGrand);
            }

            foreach (var grant in grants)
            {
                var existPublicGrant = oldAllGrands.Any(c =>
                    c.ObjectName == grant.ObjectName && c.Privilege == grant.Privilege && c.Grantee == grant.Grantee);
                if (!existPublicGrant)
                {
                    newAllGrands.Add(grant);
                }
            }

            var newData = new List<List<string>>();
            foreach (var grant in newAllGrands)
            {
                var strAr = new List<string>
                {
                    grant.ObjectName, grant.Grantee, grant.Privilege
                };
                newData.Add(strAr);
            }
            CSVWorker.WriteCsv(newData, ";", grantsFile);
        }
    }
}
