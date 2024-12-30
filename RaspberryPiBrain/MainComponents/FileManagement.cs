using Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MainComponents
{
    public class FileManagement
    {
        private const string directoryOfLog = @"service";

        public static void SaveLog(string fileNameAndDirectory, string fileBody) => SaveFile(fileNameAndDirectory, fileNameAndDirectory, fileBody, false);
        public static void SaveLog(string fileDirectory, string fileName, string fileBody) => SaveFile(fileDirectory, fileName, fileBody, false);
        protected static void SaveFile(string fileDirectory, string fileName, string fileBody, bool createNew = true, string extension = "txt")
        {
            try
            {
                string pathDirectory  = Directory.GetCurrentDirectory() + "\\" + directoryOfLog + "\\" + DateTime.Now.ToString("yyyy.MM") + "\\" + fileDirectory + "\\";
                if (!Directory.Exists(pathDirectory)) Directory.CreateDirectory(pathDirectory);

                fileName = Path.Combine(pathDirectory, fileName);

                if (createNew)
                {
                    if (File.Exists(fileName + "." + extension))
                    {
                        int index = 1;

                        while (File.Exists(fileName + "(" + index + ")." + extension)) index++;

                        fileName = fileName + "(" + index + ")";
                    }

                    File.WriteAllText(fileName + "." + extension, fileBody, Encoding.UTF8);
                }
                else
                {
                    using StreamWriter file = new(fileName + "." + extension, append: true);
                    file.WriteLine(fileBody);
                }
            }
            catch (Exception ex)
            {
                ExceptionManagement.MainException(ex, "FileManagement_SaveFile");
            }
        }

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true
        };

        public static void SaveModelToFile<TModel>(TModel model, string filePath)
        {
            File.WriteAllText(Directory.GetCurrentDirectory() + filePath, JsonSerializer.Serialize(model, jsonOptions));
        }

        public static TModel? LoadModelFromFile<TModel>(string filePath)
        {
            // Plik nie istnieje, zwróć null
            if (!File.Exists(Directory.GetCurrentDirectory() + filePath)) return default;

            return JsonSerializer.Deserialize<TModel>(File.ReadAllText(Directory.GetCurrentDirectory() + filePath));
        }
    }
}