using System;
using System.IO;
using System.Linq;

namespace OracleStructExporter.Core
{
    public static class FilesManager
    {
        

        public static int CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            var filesCounter = 0;
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(sourceDir.Length + 1);
                string destFile = Path.Combine(destDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                File.Copy(file, destFile,true);
                filesCounter++;
            }
            return filesCounter;
        }

        public static int MoveDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            Directory.CreateDirectory(destDir);
            var filesCounter = 0;
            if (Directory.Exists(sourceDir))
            {
                foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                {
                    string relativePath = file.Substring(sourceDir.Length + 1);
                    string destFile = Path.Combine(destDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                    File.Copy(file, destFile, true);
                    File.Delete(file);
                    filesCounter++;
                }
                Directory.Delete(sourceDir, true);
            }

            return filesCounter;
        }

        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
            //Directory.CreateDirectory(path);
        }

        public static bool DirectoryIsEmpty(string path)
        {
            return !Directory.GetFiles(path).Any() && !Directory.GetDirectories(path).Any();
        }

        public static string GetCurrentFolderName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            try
            {
                string directoryPath = Path.GetDirectoryName(filePath);

                if (string.IsNullOrEmpty(directoryPath))
                    return string.Empty;

                DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

                if (directoryInfo.Parent == null) // Корневой диск или сетевая папка
                    return string.Empty;

                return directoryInfo.Name;
            }
            catch (ArgumentException)
            {
                // Недопустимые символы в пути
                return string.Empty;
            }
            catch (PathTooLongException)
            {
                // Слишком длинный путь
                return string.Empty;
            }
            catch (NotSupportedException)
            {
                // Неподдерживаемый формат пути
                return string.Empty;
            }
        }

        /// <summary>
        /// Получает последнюю дату создания или изменения файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Последняя дата создания или изменения файла</returns>
        /// <exception cref="FileNotFoundException">Файл не существует</exception>
        /// <exception cref="UnauthorizedAccessException">Нет доступа к файлу</exception>
        /// <exception cref="ArgumentException">Неверный путь к файлу</exception>
        public static DateTime GetLastFileDate(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден", filePath);

            FileInfo fileInfo = new FileInfo(filePath);

            // Получаем дату создания и дату изменения файла
            DateTime creationTime = fileInfo.CreationTime;
            DateTime lastWriteTime = fileInfo.LastWriteTime;

            // Возвращаем более позднюю дату
            return creationTime > lastWriteTime ? creationTime : lastWriteTime;
        }

        /// <summary>
        /// Получает размер файла в байтах
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Размер файла в байтах</returns>
        /// <exception cref="FileNotFoundException">Файл не существует</exception>
        /// <exception cref="UnauthorizedAccessException">Нет доступа к файлу</exception>
        /// <exception cref="ArgumentException">Неверный путь к файлу</exception>
        public static long GetFileSizeInBytes(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу не может быть пустым");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден", filePath);

            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }

        /// <summary>
        /// Форматирует размер файла в удобочитаемом виде (Б, КБ, МБ, ГБ)
        /// </summary>
        /// <param name="fileSizeInBytes">Размер файла в байтах</param>
        /// <returns>Строка с форматированным размером файла</returns>
        public static string FormatFileSize(long fileSizeInBytes)
        {
            string[] sizes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            double size = fileSizeInBytes;
            int order = 0;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }
}
