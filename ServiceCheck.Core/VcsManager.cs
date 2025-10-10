using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceCheck.Core.Settings;

namespace ServiceCheck.Core
{
    public class VcsManager
    {
        public static string GetCommitName(DateTime commitDate, string commitNum)
        {
           return $"{commitDate.ToString("yyyy-MM-dd")}_{commitNum}";
        }

        public static string GetRepoNameForFileNames(string dbId, string userName)
        {
            return $"{dbId}_{userName}";
        }

        public bool CreateRepoSnapshot(string repoName, string vcsFolder, string snapshotFolder, int? commitNum, DateTime? commitDate, out List<CommitShortInfo> commitShortInfoList)
        {
            commitShortInfoList = new List<CommitShortInfo>();
            // Нормализация путей
            repoName = repoName.Trim('\\');
            string initialRepoPath = Path.Combine(vcsFolder, "initial");
            if (!Directory.Exists(initialRepoPath)) Directory.CreateDirectory(initialRepoPath);
            string commitsPath = Path.Combine(vcsFolder, "commits");
            if (!Directory.Exists(commitsPath)) Directory.CreateDirectory(commitsPath);

            // Поиск исходного снимка
            CommitShortInfo initCommitShortInfo;
            string initialCommitDir = FindInitialCommitDir(initialRepoPath, repoName, out initCommitShortInfo);
            if (initialCommitDir == null) return false;
            commitShortInfoList.Add(initCommitShortInfo);

            // Копирование исходной версии
            string sourceInitialPath = Path.Combine(initialCommitDir, repoName);
            //string destPath = Path.Combine(snapshotFolder, repoName);
            FilesManager.CopyDirectory(sourceInitialPath, snapshotFolder);
            //CopyDirectory(sourceInitialPath, destPath, out filesCount);

            // Применение коммитов
            var commitsToApply = GetCommitsToApply(commitsPath, repoName, commitNum, commitDate);
            foreach (var commitDir in commitsToApply)
            {
                string commitRepoPath = Path.Combine(commitDir, repoName);
                if (!Directory.Exists(commitRepoPath)) continue;
                var commitShortInfo = new CommitShortInfo();
                int addOrUpdCnt, delCnt;
                GetFilesCountInCommitFolder(commitRepoPath, out addOrUpdCnt, out delCnt);
                commitShortInfo.FilesAddOrUpdateCount = addOrUpdCnt;
                commitShortInfo.FilesDeleteCount = delCnt;
                commitShortInfo.IsInitial = false;
                commitShortInfo.FolderName = Path.GetFileName(commitDir);
                commitShortInfoList.Add(commitShortInfo);
                ApplyCommitChanges(commitRepoPath, snapshotFolder);
            }

            return true;
        }

        private static void GetFilesCountInCommitFolder(string commitFolderFullPath, out int addOrUpdFiles, out int deleteFiles)
        {
            addOrUpdFiles = deleteFiles = 0;
            // Проверяем существование директории
            if (!Directory.Exists(commitFolderFullPath))
                return;
            // Получаем все файлы в директории
            string[] files;
            files = Directory.GetFiles(commitFolderFullPath, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    // Проверяем условия подсчета
                    if (fileInfo.Length == 0)
                        deleteFiles++;
                    else if (fileInfo.Length > 0)
                        addOrUpdFiles++;
                }
                catch (FileNotFoundException)
                {
                    // Пропускаем файлы, которые были удалены после получения списка
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    // Пропускаем файлы без доступа
                    continue;
                }
            }
            return;
        }

        private string FindInitialCommitDir(string initialPath, string repoName, out CommitShortInfo commitShortInfo)
        {
            commitShortInfo = new CommitShortInfo();
            foreach (var commitDir in Directory.GetDirectories(initialPath))
            {
                string repoPath = Path.Combine(commitDir, repoName);
                if (Directory.Exists(repoPath) && !FilesManager.DirectoryIsEmpty(repoPath))
                {
                    int addOrUpdCount;
                    GetFilesCountInCommitFolder(repoPath, out addOrUpdCount, out int deleteCount);
                    commitShortInfo.FilesAddOrUpdateCount = addOrUpdCount;
                    commitShortInfo.IsInitial = true;
                    commitShortInfo.FolderName = Path.GetFileName(commitDir);
                    return commitDir;
                }
            }
            return null;
        }

        private IEnumerable<string> GetCommitsToApply(string commitsPath, string repoName, int? commitNum, DateTime? commitDate)
        {
            /*var commitDirs = Directory.GetDirectories(commitsPath)
                .Select(d => new { Path = d, Name = Path.GetFileName(d) })
                .Where(d => TryParseCommitName(d.Name, out DateTime date, out int num))
                .OrderBy(d => d.Name)
                .ToList();*/
            var repoNameParts = repoName.Split('\\');
            List<Tuple<int, DateTime, string>> commitDirs = new List<Tuple<int, DateTime, string>>();
            foreach (var commitDir in Directory.GetDirectories(commitsPath))
            {
                var commitName = Path.GetFileName(commitDir);
                DateTime tmpcommitDate;
                int tmpcommitId;
                if (TryParseCommitName(commitName, out tmpcommitDate, out tmpcommitId))
                {
                    var repoNameDir = Path.Combine(commitDir, repoNameParts[0], repoNameParts[1]);
                    if (Directory.Exists(repoNameDir))
                        commitDirs.Add(new Tuple<int, DateTime, string>(tmpcommitId, tmpcommitDate, commitDir));
                }
            }

            if (commitNum.HasValue)
            {
                //return commitDirs.Where(d => d.Name.EndsWith($"_{commitNum.Value}")).Select(d => d.Path);

                return commitDirs.OrderBy(c => c.Item1).TakeWhile(c => c.Item1 <= commitNum.Value).Select(c => c.Item3);
            }

            if (commitDate.HasValue)
            {
                //var lastCommit = commitDirs.LastOrDefault(d => d.Name.StartsWith(commitDate.Value.ToString("yyyy-MM-dd")));
                //return lastCommit != null ? new[] { lastCommit.Path } : Enumerable.Empty<string>();
                return commitDirs.OrderBy(c => c.Item2).ThenBy(c=>c.Item1).TakeWhile(c => c.Item2 <= commitDate.Value).Select(c => c.Item3);
            }

            return Enumerable.Empty<string>();
        }

        private bool TryParseCommitName(string name, out DateTime date, out int num)
        {
            date = default;
            num = 0;
            var parts = name.Split('_');
            if (parts.Length != 2) return false;
            return DateTime.TryParse(parts[0], out date) && int.TryParse(parts[1], out num);
        }

        private void ApplyCommitChanges(string commitRepoPath, string snapshotRepoPath)
        {
            foreach (var file in Directory.GetFiles(commitRepoPath, "*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(commitRepoPath.Length + 1);
                string destFile = Path.Combine(snapshotRepoPath, relativePath);

                if (new FileInfo(file).Length == 0)
                {
                    if (File.Exists(destFile)) File.Delete(destFile);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                    File.Copy(file, destFile, true);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputFolder"></param>
        /// <param name="dbSubfolder"></param>
        /// <param name="userNameSubfolder"></param>
        /// <param name="vcsFolder"></param>
        /// <param name="processId"></param>
        /// <param name="commitDate"></param>
        /// <param name="ignoreDifferences"></param>
        /// <param name="ignoreRemovingItems">Если true, то не удаляем в репо файл, даже если он пропал в исходной папке. Необходимости в данном параметре пока нет, поэтому рекомендуется выставлять false</param>
        /// <param name="changesCount"></param>
        /// <param name="repoChanges"></param>
        public void CreateCommit(string inputFolder, string dbSubfolder, string userNameSubfolder, string vcsFolder,
            int processId, DateTime commitDate, IgnoreDifferences ignoreDifferences, bool ignoreRemovingItems, out int changesCount, out List<RepoChangeItem> repoChanges)
        {
            var repo = $"{dbSubfolder}\\{userNameSubfolder}";
            changesCount = 0;
            repoChanges = new List<RepoChangeItem>();
            string tmpPath = Path.Combine(vcsFolder, "tmp");
            var commitName = GetCommitName(commitDate, processId.ToString());
            string currentCommitTmpDir = Path.Combine(tmpPath, commitName);

            //FilesManager.CleanDirectory(tmpPath);

            //foreach (var repo in repolist)
            //{
            string repoInputPath = Path.Combine(inputFolder, repo);
            if (!Directory.Exists(repoInputPath)) return;

            string previousSnapshotPath = Path.Combine(currentCommitTmpDir, "previous", repo);
            FilesManager.DeleteDirectory(previousSnapshotPath);
            List<CommitShortInfo> commitShortInfoList;
            bool snapshotExists = CreateRepoSnapshot(repo, vcsFolder, previousSnapshotPath, null,
                DateTime.Now.AddDays(1), out commitShortInfoList);

            if (!snapshotExists)
            {
                // Добавление нового репозитория в initial
                string initialCommitDir = Path.Combine(vcsFolder, "initial", commitName, repo);
                //CopyDirectory(repoInputPath, Path.Combine(initialCommitDir, repo), out changesCount);
                //changesCount = FilesManager.CopyDirectory(repoInputPath, initialCommitDir);
                CommitShortInfo initialCommitShortInfo;
                changesCount = CreateInitialCommit(repoInputPath, initialCommitDir, processId, commitDate, dbSubfolder, userNameSubfolder, out repoChanges, out initialCommitShortInfo);
                commitShortInfoList.Add(initialCommitShortInfo);
                //FilesManager.CleanDirectory(tmpPath);
            }
            else
            {
                // Сравнение и создание дельты
                string tmpDeltaPath = Path.Combine(currentCommitTmpDir, "new", repo);
                CommitShortInfo commitShortInfo;
                FilesManager.DeleteDirectory(tmpDeltaPath);
                CompareAndCreateDelta(repoInputPath, previousSnapshotPath, tmpDeltaPath, processId, commitDate, dbSubfolder, userNameSubfolder, ignoreDifferences, ignoreRemovingItems, out repoChanges, out commitShortInfo);
                

                if (repoChanges.Any(c=>!c.MaskWorked))
                {
                    // Перенос дельты в commits
                    string finalCommitPath = Path.Combine(vcsFolder, "commits", commitName, repo);
                    //MoveDirectory(Path.Combine(currentCommitDir, "new"), finalCommitPath, out changesCount);
                    changesCount = FilesManager.MoveDirectory(tmpDeltaPath, finalCommitPath);

                    commitShortInfoList.Add(commitShortInfo);
                }
            }

            // Удаляем из временной папку папку коммита
            FilesManager.DeleteDirectory(currentCommitTmpDir);
            //FilesManager.DeleteDirectory(tmpDeltaPath);
            //FilesManager.DeleteDirectory(previousSnapshotPath);


            //}

            //создаем список истории всех коммитов, включая последний для синхронизации с работой GitLab сервиса
            var commitsJournalFilePath = Path.Combine(vcsFolder, "journal");
            if (!Directory.Exists(commitsJournalFilePath)) Directory.CreateDirectory(commitsJournalFilePath);
            var commitsJournalFileFullName = Path.Combine(commitsJournalFilePath,
                GetRepoNameForFileNames(dbSubfolder, userNameSubfolder) + ".csv");
            SaveCommitShortInfoList(commitShortInfoList, commitsJournalFileFullName);
        }

        /// <summary>
        /// Список в формате DBId/UserName
        /// </summary>
        /// <param name="repoFolder"></param>
        /// <returns></returns>
        public static List<string> GetDBAndUserNameListFromRepoFolder(string repoFolder)
        {
            var res = new List<string>();
            if (string.IsNullOrWhiteSpace(repoFolder)) return res;
            //считаем, что в initial должны быть все варианты, ищем только там
            var pathToInitial = Path.Combine(repoFolder, "initial");
            if (!Directory.Exists(pathToInitial))
                return res;
            foreach (var commitFolder in Directory.GetDirectories(pathToInitial))
            {
                foreach (var dbFolder in Directory.GetDirectories(commitFolder))
                {
                    foreach (var userFolder in Directory.GetDirectories(dbFolder))
                    {
                        if (Directory.GetFiles(userFolder, "*", SearchOption.AllDirectories).Any())
                        {
                            res.Add($"{Path.GetFileName(dbFolder)}\\{Path.GetFileName(userFolder)}");
                        }
                    }
                }
            }
            return res;
        }

        public static void SaveCommitShortInfoList(List<CommitShortInfo> shortInfoList, string fileName)
        {
            var data = new List<List<string>>();
            var header = new List<string>
            {
                "Commit", "AddOrUpd", "Del", "IsInit"
            };
            data.Add(header);
            foreach (var commitShortInfo in shortInfoList)
            {
                var row = new List<string>
                {
                    commitShortInfo.FolderName, commitShortInfo.FilesAddOrUpdateCount.ToString(), commitShortInfo.FilesDeleteCount.ToString(), commitShortInfo.IsInitial?"да":"нет"
                };
                data.Add(row);
            }
            CSVWorker.WriteCsv(data, ";", fileName);
        }

        OracleObjectType GetOracleObjTypeByFolderName(string folderName)
        {
            switch (folderName.ToLower())
            {
                case "dblinks": return OracleObjectType.DBLINK;
                case "dbms_jobs": return OracleObjectType.DBMS_JOB;
                case "functions": return OracleObjectType.FUNCTION;
                case "packages": return OracleObjectType.PACKAGE;
                case "procedures": return OracleObjectType.PROCEDURE;
                case "scheduler_jobs": return OracleObjectType.SCHEDULER_JOB;
                case "sequences": return OracleObjectType.SEQUENCE;
                case "synonyms": return OracleObjectType.SYNONYM;
                case "tables": return OracleObjectType.TABLE;
                case "triggers": return OracleObjectType.TRIGGER;
                case "types": return OracleObjectType.TYPE;
                case "views": return OracleObjectType.VIEW;
                default: return OracleObjectType.UNKNOWN;
            }
        }

        private int CreateInitialCommit(string sourceDir, string destDir, int processId, DateTime commitDate, string dbSubfolder, string userNameSubfolder, out List<RepoChangeItem> repoChanges, out CommitShortInfo commitShortInfo)
        {
            repoChanges = new List<RepoChangeItem>();
            commitShortInfo = new CommitShortInfo();
            commitShortInfo.IsInitial = true;
            commitShortInfo.FolderName = GetCommitName(commitDate, processId.ToString());
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            var filesCounter = 0;
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var currentFileFolder = FilesManager.GetCurrentFolderName(file);
                var addItem = new RepoChangeItem
                {
                    FileName = Path.GetFileName(file),
                    DBId = dbSubfolder,
                    UserName = userNameSubfolder,
                    ProcessId = processId,
                    CommitCommonDate = commitDate,
                    ObjectType = GetOracleObjTypeByFolderName(currentFileFolder),
                    Operation = RepoOperation.ADD,
                    IsInitial = true,
                    CommitCurFileTime = FilesManager.GetLastFileDate(file),
                    FileSize = FilesManager.GetFileSizeInBytes(file)
                };

                string relativePath = file.Substring(sourceDir.Length + 1);
                string destFile = Path.Combine(destDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                File.Copy(file, destFile, true);
                filesCounter++;

                repoChanges.Add(addItem);
            }

            commitShortInfo.FilesAddOrUpdateCount = filesCounter;
            return filesCounter;
        }

        private void CompareAndCreateDelta(string inputPath, string previousPath, string deltaPath, int processId, DateTime commitDate, string dbSubfolder, string userNameSubfolder, IgnoreDifferences ignoreDifferences, bool ignoreRemovingItems, out List<RepoChangeItem> repoChanges, out CommitShortInfo commitShortInfo)
        {
            repoChanges = new List<RepoChangeItem>();
            commitShortInfo = new CommitShortInfo();
            commitShortInfo.IsInitial = false;
            commitShortInfo.FolderName = GetCommitName(commitDate, processId.ToString());

            var inputFiles = Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories);
            var previousFiles = Directory.GetFiles(previousPath, "*", SearchOption.AllDirectories);

            // Обработка новых и измененных файлов
            foreach (var file in inputFiles)
            {
                string relativePath = file.Substring(inputPath.Length + 1);
                string prevFile = Path.Combine(previousPath, relativePath);
                string deltaFile = Path.Combine(deltaPath, relativePath);
                var fileName = Path.GetFileName(file);
                var currentFileFolder = FilesManager.GetCurrentFolderName(file);
                bool maskWorked = false;
                bool filesAreEqual = false;
                bool prevFileExists = File.Exists(prevFile);
                if (prevFileExists)
                    filesAreEqual = FilesAreEqual(file, prevFile, dbSubfolder, userNameSubfolder, currentFileFolder, fileName, ignoreDifferences, out maskWorked);
                if (!prevFileExists || !filesAreEqual || maskWorked)
                {
                    if (!maskWorked)
                    {
                        //копируем файл в коммит только если были фактические изменения
                        Directory.CreateDirectory(Path.GetDirectoryName(deltaFile));
                        File.Copy(file, deltaFile);
                        commitShortInfo.FilesAddOrUpdateCount++;
                    }

                    var addOrUpdItem = new RepoChangeItem
                    {
                        FileName = fileName, DBId = dbSubfolder, UserName = userNameSubfolder,
                        ProcessId = processId, CommitCommonDate = commitDate,
                        ObjectType = GetOracleObjTypeByFolderName(currentFileFolder),
                        IsInitial = false,
                        CommitCurFileTime = FilesManager.GetLastFileDate(file),
                        FileSize = FilesManager.GetFileSizeInBytes(file)
                    };
                    if (!File.Exists(prevFile))
                    {
                        //новый файл
                        addOrUpdItem.Operation = RepoOperation.ADD;
                    }
                    else
                    {
                        //измененный файл
                        addOrUpdItem.Operation = RepoOperation.UPD;
                        addOrUpdItem.MaskWorked = maskWorked;
                    }
                    repoChanges.Add(addOrUpdItem);
                }
            }

            // Обработка удаленных файлов
            if (!ignoreRemovingItems)
            {
                foreach (var file in previousFiles)
                {
                    string relativePath = file.Substring(previousPath.Length + 1);
                    string inputFile = Path.Combine(inputPath, relativePath);

                    if (!File.Exists(inputFile))
                    {
                        string deltaFile = Path.Combine(deltaPath, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(deltaFile));
                        File.WriteAllBytes(deltaFile, new byte[0]); // Пустой файл как флаг удаления
                        commitShortInfo.FilesDeleteCount++;
                        var currentFileFolder = FilesManager.GetCurrentFolderName(file);
                        //удаленный файл
                        var delItem = new RepoChangeItem
                        {
                            FileName = Path.GetFileName(file),
                            DBId = dbSubfolder,
                            UserName = userNameSubfolder,
                            ProcessId = processId,
                            CommitCommonDate = commitDate,
                            ObjectType = GetOracleObjTypeByFolderName(currentFileFolder),
                            Operation = RepoOperation.DEL,
                            IsInitial = false,
                            CommitCurFileTime = FilesManager.GetLastFileDate(file),
                            //здесь считаем размер прошлой версии удаленного файла, так как новая версия - всегда нулевая
                            FileSize = FilesManager.GetFileSizeInBytes(file)
                        };
                        repoChanges.Add(delItem);
                    }
                }
            }
        }

        private bool FilesAreEqual(string path1, string path2, string dbId, string userName, string folderName, string fileName, IgnoreDifferences ignoreDifferences, out bool maskWorked)
        {
            maskWorked = false;
            var filesContentEqual = File.ReadAllBytes(path1).SequenceEqual(File.ReadAllBytes(path2));
            if (filesContentEqual)
                return true;
            
            if (ignoreDifferences != null)
            {
                //если файлы не идентичны, но в конфигурации есть настройка игнора, пробуем использовать шанс
                var connForIgnore = ignoreDifferences.ConnectionsForIgnoreDiff.FirstOrDefault(c =>
                    c.DbId.ToUpper() == dbId.ToUpper() && c.UserName.ToUpper() == userName.ToUpper());
                if (connForIgnore != null)
                {
                    var filesToIgnore = connForIgnore.FilesForIgnoreDiff.Where(c =>
                        (c.FolderName=="*" || c.FolderName.ToUpper() == folderName.ToUpper()) 
                        && (c.FileName=="*" || c.FileName.ToUpper() == fileName.ToUpper())).ToList();
                    if (filesToIgnore.Any())
                    {
                        var file1Lines = new List<string>(File.ReadAllLines(path1));
                        var file2Lines = new List<string>(File.ReadAllLines(path2));
                        
                        //если будут реализованы другие правила, кроме LineRulesForIgnoreDiff, то доработать это условие
                        if (file1Lines.Count != file2Lines.Count) return false;

                        var lineRules = filesToIgnore.SelectMany(c => c.LineRulesForIgnoreDiff).ToList();
                        if (lineRules.Any())
                        {
                            for (int i = 0; i < file1Lines.Count; i++)
                            {
                                var file1Line = file1Lines[i];
                                var file2Line = file2Lines[i];
                                if (file1Line == file2Line) continue;
                                var maskInLineFound = false;
                                foreach (var lineRuleForIgnoreDiff in lineRules)
                                {
                                    var maskParts = lineRuleForIgnoreDiff.StaticMask.Split(new []{"{!!!VARIABLE_VALUE!!!}"}, StringSplitOptions.None);
                                    var file1LineTmp = lineRuleForIgnoreDiff.TrimEmptySpacesBeforeAndAfter
                                        ? file1Line.Trim()
                                        : file1Line;
                                    var file2LineTmp = lineRuleForIgnoreDiff.TrimEmptySpacesBeforeAndAfter
                                        ? file2Line.Trim()
                                        : file2Line;
                                    if ((!string.IsNullOrWhiteSpace(maskParts[0]) && 
                                         (!file1LineTmp.StartsWith(maskParts[0]) || !file2LineTmp.StartsWith(maskParts[0]))) ||
                                        (!string.IsNullOrWhiteSpace(maskParts[1]) &&
                                         (!file1LineTmp.EndsWith(maskParts[1]) || !file2LineTmp.EndsWith(maskParts[1])))) continue;
                                    maskInLineFound = true;
                                    maskWorked = true;
                                    break;
                                }
                                if (!maskInLineFound) return false;
                            }
                            //если дошли сюда, значит все строки с учетом масок идентичны
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        //private void CopyDirectory(string sourceDir, string destDir, out int filesCount)
        //{
        //    if (!Directory.Exists(destDir))
        //        Directory.CreateDirectory(destDir);
        //    filesCount = 0;
        //    foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        //    {
        //        string relativePath = file.Substring(sourceDir.Length + 1);
        //        string destFile = Path.Combine(destDir, relativePath);
        //        Directory.CreateDirectory(Path.GetDirectoryName(destFile));
        //        File.Copy(file, destFile);
        //        filesCount++;
        //    }
        //}

        //private void MoveDirectory(string sourceDir, string destDir, out  int filesCount)
        //{
        //    Directory.CreateDirectory(destDir);
        //    filesCount = 0;
        //    if (Directory.Exists(sourceDir))
        //    {
        //        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        //        {
        //            string relativePath = file.Substring(sourceDir.Length + 1);
        //            string destFile = Path.Combine(destDir, relativePath);
        //            Directory.CreateDirectory(Path.GetDirectoryName(destFile));
        //            File.Move(file, destFile);
        //            filesCount++;
        //        }
        //        Directory.Delete(sourceDir, true);
        //    }
            
        //}

        //private void CleanDirectory(string path)
        //{
        //    if (Directory.Exists(path)) Directory.Delete(path, true);
        //    Directory.CreateDirectory(path);
        //}

        //private bool DirectoryIsEmpty(string path)
        //{
        //    return !Directory.GetFiles(path).Any() && !Directory.GetDirectories(path).Any();
        //}
    }
}
