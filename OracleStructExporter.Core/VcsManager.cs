using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace OracleStructExporter.Core
{
    public class VcsManager
    {
        public static string GetCommitName(DateTime commitDate, string commitNum)
        {
           return $"{commitDate.ToString("yyyy-MM-dd")}_{commitNum}";
        }

        public bool CreateRepoSnapshot(string repoName, string vcsFolder, string snapshotFolder, int? commitNum, DateTime? commitDate, out int filesCount)
        {
            filesCount = 0;
            // Нормализация путей
            repoName = repoName.Trim('\\');
            string initialRepoPath = Path.Combine(vcsFolder, "initial");
            if (!Directory.Exists(initialRepoPath)) Directory.CreateDirectory(initialRepoPath);
            string commitsPath = Path.Combine(vcsFolder, "commits");
            if (!Directory.Exists(commitsPath)) Directory.CreateDirectory(commitsPath);

            // Поиск исходного снимка
            string initialCommitDir = FindInitialCommitDir(initialRepoPath, repoName);
            if (initialCommitDir == null) return false;

            // Копирование исходной версии
            string sourceInitialPath = Path.Combine(initialCommitDir, repoName);
            //string destPath = Path.Combine(snapshotFolder, repoName);
            filesCount = FilesManager.CopyDirectory(sourceInitialPath, snapshotFolder);
            //CopyDirectory(sourceInitialPath, destPath, out filesCount);

            // Применение коммитов
            var commitsToApply = GetCommitsToApply(commitsPath, repoName, commitNum, commitDate);
            foreach (var commitDir in commitsToApply)
            {
                string commitRepoPath = Path.Combine(commitDir, repoName);
                if (!Directory.Exists(commitRepoPath)) continue;

                ApplyCommitChanges(commitRepoPath, snapshotFolder);
            }

            return true;
        }

        private string FindInitialCommitDir(string initialPath, string repoName)
        {
            foreach (var commitDir in Directory.GetDirectories(initialPath))
            {
                string repoPath = Path.Combine(commitDir, repoName);
                if (Directory.Exists(repoPath) && !FilesManager.DirectoryIsEmpty(repoPath)) return commitDir;
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

        public void CreateCommit(string inputFolder, string dbSubfolder, string userNameSubfolder, string vcsFolder,
            int processId, DateTime commitDate, out int changesCount, out List<RepoChangeItem> repoChanges)
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
            int snapshotFilesCount;
            bool snapshotExists = CreateRepoSnapshot(repo, vcsFolder, previousSnapshotPath, null,
                DateTime.Now.AddDays(1), out snapshotFilesCount);

            if (!snapshotExists)
            {
                // Добавление нового репозитория в initial
                string initialCommitDir = Path.Combine(vcsFolder, "initial", commitName, repo);
                //CopyDirectory(repoInputPath, Path.Combine(initialCommitDir, repo), out changesCount);
                //changesCount = FilesManager.CopyDirectory(repoInputPath, initialCommitDir);
                changesCount = CreateInitialCommit(repoInputPath, initialCommitDir, processId, commitDate, dbSubfolder, userNameSubfolder, out repoChanges);
                //FilesManager.CleanDirectory(tmpPath);
            }
            else
            {
                // Сравнение и создание дельты
                string tmpDeltaPath = Path.Combine(currentCommitTmpDir, "new", repo);
                FilesManager.DeleteDirectory(tmpDeltaPath);
                CompareAndCreateDelta(repoInputPath, previousSnapshotPath, tmpDeltaPath, processId, commitDate, dbSubfolder, userNameSubfolder, out repoChanges);

                if (repoChanges.Any())
                {
                    // Перенос дельты в commits
                    string finalCommitPath = Path.Combine(vcsFolder, "commits", commitName, repo);
                    //MoveDirectory(Path.Combine(currentCommitDir, "new"), finalCommitPath, out changesCount);
                    changesCount = FilesManager.MoveDirectory(tmpDeltaPath, finalCommitPath);
                }
            }

            // Удаляем из временной папку папку коммита
            FilesManager.DeleteDirectory(currentCommitTmpDir);
            //FilesManager.DeleteDirectory(tmpDeltaPath);
            //FilesManager.DeleteDirectory(previousSnapshotPath);


            //}

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

        private int CreateInitialCommit(string sourceDir, string destDir, int processId, DateTime commitDate, string dbSubfolder, string userNameSubfolder, out List<RepoChangeItem> repoChanges)
        {
            repoChanges = new List<RepoChangeItem>();
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
            return filesCounter;
        }

        private void CompareAndCreateDelta(string inputPath, string previousPath, string deltaPath, int processId, DateTime commitDate, string dbSubfolder, string userNameSubfolder, out List<RepoChangeItem> repoChanges)
        {
            repoChanges = new List<RepoChangeItem>();

            var inputFiles = Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories);
            var previousFiles = Directory.GetFiles(previousPath, "*", SearchOption.AllDirectories);

            // Обработка новых и измененных файлов
            foreach (var file in inputFiles)
            {
                string relativePath = file.Substring(inputPath.Length + 1);
                string prevFile = Path.Combine(previousPath, relativePath);
                string deltaFile = Path.Combine(deltaPath, relativePath);

                var currentFileFolder = FilesManager.GetCurrentFolderName(file);

                if (!File.Exists(prevFile) || !FilesAreEqual(file, prevFile))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(deltaFile));
                    File.Copy(file, deltaFile);
                    var addOrUpdItem = new RepoChangeItem
                    {
                        FileName = Path.GetFileName(file), DBId = dbSubfolder, UserName = userNameSubfolder,
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
                    }
                    repoChanges.Add(addOrUpdItem);
                }
            }

            // Обработка удаленных файлов
            foreach (var file in previousFiles)
            {
                string relativePath = file.Substring(previousPath.Length + 1);
                string inputFile = Path.Combine(inputPath, relativePath);

                if (!File.Exists(inputFile))
                {
                    string deltaFile = Path.Combine(deltaPath, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(deltaFile));
                    File.WriteAllBytes(deltaFile, new byte[0]); // Пустой файл как флаг удаления
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

        private bool FilesAreEqual(string path1, string path2)
        {
            return File.ReadAllBytes(path1).SequenceEqual(File.ReadAllBytes(path2));
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
