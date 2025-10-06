using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceCheck.Core
{
    public static class GrantsOuterManager
    {
        public static void SaveGrantsForCurrentSchemaAndForPublic(List<GrantAttributes> grants, string grantsFolder, string dbName, string userName)
        {
            
            var grantsListFolder = Path.Combine(grantsFolder, dbName);
            if (!Directory.Exists(grantsListFolder))
                Directory.CreateDirectory(grantsListFolder);
            var grantsCurrUserFile = Path.Combine(grantsListFolder, $"{userName}.csv");
            var grantsPublicFile = Path.Combine(grantsListFolder, $"PUBLIC.csv");
            var grantsUnknownFile = Path.Combine(grantsListFolder, $"UNKNOWN_SCHEMA!!!.csv");
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
            if (newCurrUserGrands.Any())
            {
                foreach (var grant in newCurrUserGrands)
                {
                    var strAr = new List<string>
                    {
                        grant.ObjectSchema, grant.ObjectName, grant.Privilege, grant.Grantable, userName
                    };
                    newData.Add(strAr);
                }

                CSVWorker.WriteCsv(newData, ";", grantsCurrUserFile);
            }

            if (newPublicGrands.Any())
            {
                newData.Clear();
                foreach (var grant in newPublicGrands)
                {
                    var strAr = new List<string>
                    {
                        grant.ObjectSchema, grant.ObjectName, grant.Privilege, grant.Grantable, "PUBLIC"
                    };
                    newData.Add(strAr);
                }
                CSVWorker.WriteCsv(newData, ";", grantsPublicFile);
            }

            if (newUnknownUserGrands.Any())
            {
                //не должны сюда попасть
                newData.Clear();
                foreach (var grant in newUnknownUserGrands)
                {
                    var strAr = new List<string>
                    {
                        grant.ObjectSchema, grant.ObjectName, grant.Privilege, grant.Grantable, grant.Grantee
                    };
                    newData.Add(strAr);
                }

                CSVWorker.WriteCsv(newData, ";", grantsUnknownFile);
            }
        }



        public static List<GrantAttributes> GetGrants(string grantsFolder, string dbName)
        {
            var res = new List<GrantAttributes>();
            var grantsListFolder = Path.Combine(grantsFolder, dbName);
            if (!Directory.Exists(grantsListFolder))
                return null;

            foreach (var grantFile in Directory.GetFiles(grantsListFolder))      
            {
               var data = CSVWorker.ReadCsv( grantFile, ";");
               foreach (var row in data)
               {
                   var grant = new GrantAttributes
                   {
                       ObjectSchema = row[0],
                       ObjectName = row[1],
                       Privilege = row[2],
                       Grantable = row[3],
                       Grantee = row[4]
                   };
                   
                   res.Add(grant);
               }
            }
            return res;
        }
    }
}
