using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Newtonsoft.Json;
namespace Lidar_Framework
{
   public class FileManager
   {
      private const string RecentFilesPath = "D:\\OneDrive\\Курсач\\Проект\\Lidar Framework\\Lidar Framework\\recentFiles.json";
      public List<Point3D> points { get; private set; } = new List<Point3D>();
      public List<string> RecentLoadFiles { get; private set; } = new List<string>();
      public List<string> RecentSaveFiles { get; private set; } = new List<string>();
      private const int MaxRecentFilesCount = 5;
      public FileManager()
      {
         LoadRecentFiles();
      }
      public async Task LoadPCDAsync(string filePath)
      {
         points.Clear();
         if (!File.Exists(filePath))
         {
            throw new FileNotFoundException($"File {filePath} not found.");
         }

         string[] lines = await Task.Run(() => File.ReadAllLines(filePath));

         var newPoints = new List<Point3D>();
         for (int i = 11; i < lines.Length; i++)
         {
            string[] values = lines[i].Split(' ');
            if (values.Length < 3) continue;

            if (double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y) &&
                double.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
            {
               newPoints.Add(new Point3D(x, y, z));
            }
         }

         // Операция присвоения выполнится быстро, так как points уже очищен
         points.AddRange(newPoints);
      }
      public async Task SavePCDAsync(string filePath)
      {
         using (StreamWriter writer = new StreamWriter(filePath))
         {
            // Add headers if needed
            await writer.WriteLineAsync("# .PCD v0.7 - Point Cloud Data file format");
            await writer.WriteLineAsync("VERSION 0.7");
            await writer.WriteLineAsync("FIELDS x y z");
            await writer.WriteLineAsync("SIZE 4 4 4");
            await writer.WriteLineAsync("TYPE F F F");
            await writer.WriteLineAsync("COUNT 1 1 1");
            await writer.WriteLineAsync("WIDTH " + points.Count);
            await writer.WriteLineAsync("HEIGHT 1");
            await writer.WriteLineAsync("VIEWPOINT 0 0 0 1 0 0 0");
            await writer.WriteLineAsync("POINTS " + points.Count);
            await writer.WriteLineAsync("DATA ascii");

            foreach (var point in points)
            {
               await writer.WriteLineAsync($"{point.X.ToString(CultureInfo.InvariantCulture)} {point.Y.ToString(CultureInfo.InvariantCulture)} {point.Z.ToString(CultureInfo.InvariantCulture)}");
            }
         }
      }
      private void LoadRecentFiles()
      {
         if (File.Exists(RecentFilesPath))
         {
            string json = File.ReadAllText(RecentFilesPath);
            var recentFiles = JsonConvert.DeserializeObject<RecentFiles>(json);
            if (recentFiles != null)
            {
               RecentLoadFiles = recentFiles.Load ?? new List<string>();
               RecentSaveFiles = recentFiles.Save ?? new List<string>();
            }
         }
      }
      public class RecentFiles
      {
         public List<string> Load { get; set; }
         public List<string> Save { get; set; }
      }
      private void SaveRecentFiles()
      {
         var recentFiles = new Dictionary<string, List<string>>
        {
            { "Load", RecentLoadFiles },
            { "Save", RecentSaveFiles }
        };
         string json = JsonConvert.SerializeObject(recentFiles);
         File.WriteAllText(RecentFilesPath, json);
      }
      public void AddToRecentFiles(List<string> recentFiles, string filePath)
      {
         // Приводим filePath к нижнему регистру для регистронезависимого сравнения
         string lowerCaseFilePath = filePath.ToLower();

         // Проверяем наличие файла в списке, используя регистронезависимое сравнение
         if (!recentFiles.Any(f => f.ToLower() == lowerCaseFilePath))
         {
            recentFiles.Insert(0, filePath);
            if (recentFiles.Count > MaxRecentFilesCount)
            {
               recentFiles.RemoveAt(recentFiles.Count - 1);
            }
            SaveRecentFiles();
         }
      }
      public string GetFormattedFileSize(long fileSizeInBytes)
      {
         double fileSize = 0;
         string fileSizeText = "";

         if (fileSizeInBytes < 1024)
         {
            fileSize = fileSizeInBytes;
            fileSizeText = " Б";
         }
         else if (fileSizeInBytes < 1024 * 1024)
         {
            fileSize = (double)fileSizeInBytes / 1024;
            fileSizeText = " КБ";
         }
         else if (fileSizeInBytes < 1024 * 1024 * 1024)
         {
            fileSize = (double)fileSizeInBytes / (1024 * 1024);
            fileSizeText = " МБ";
         }
         else
         {
            fileSize = (double)fileSizeInBytes / (1024 * 1024 * 1024);
            fileSizeText = " ГБ";
         }

         return fileSize.ToString("0.##") + fileSizeText;
      }

   }
}