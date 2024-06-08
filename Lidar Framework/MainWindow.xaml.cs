using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using plotLib = ScottPlot; // Установка псевдонима для ScottPlot
using System.IO.Ports;


namespace Lidar_Framework
{
   /// <summary>
   /// Логика взаимодействия для MainWindow.xaml
   /// </summary>

   public partial class MainWindow : Window
   {
      private FileManager fileManager = new FileManager();

      public MainWindow()
      {
         InitializeComponent();
         DirectionalLight directionalLight = new DirectionalLight(Colors.White, new Vector3D(0, 0, -1));
         ModelVisual3D lightModel = new ModelVisual3D();
         lightModel.Content = directionalLight;
         viewport3D.Children.Add(lightModel);

         UpdateRecentFilesListBox(lbLastLoad, fileManager.RecentLoadFiles);
         UpdateRecentFilesListBox(lbLastSave, fileManager.RecentSaveFiles);

      }

      private void MainWindow_Loaded(object sender, RoutedEventArgs e)
      {
         // Создание коллекции точек
         var points = new Point3DCollection();
         Random random = new Random();
         for (int i = 0; i < 255000; i++)
         {
            // Генерация случайных координат точек в пределах [-10, 10]
            double x = random.NextDouble() * 20 - 1;
            double y = random.NextDouble() * 20 - 1;
            double z = random.NextDouble() * 20 - 1;

            points.Add(new Point3D(x, y, z));
         }

         // Создание модели точек
         var visual = new ModelVisual3D();

         var pointsVisual = new PointsVisual3D
         {
            Points = points,
            Size = 3,
            Color = Colors.Red
         };

         visual.Children.Add(pointsVisual);

         viewport3D.Children.Add(visual);
      }

      private void Points_2D()
      {
         // Преобразование точек в массивы для ScottPlot
         double[] xs = fileManager.points.Select(p => p.X).ToArray();
         double[] ys = fileManager.points.Select(p => p.Y).ToArray();

         // Построение графика
         var plt = wpfPlot.Plot;
         plt.Clear();
         plt.Add.ScatterPoints(xs, ys, color: plotLib.Color.FromColor(System.Drawing.Color.FromArgb(40, 144, 255))); // Используем метод Add.Scatter
         plt.Axes.SquareUnits();

         plt.Axes.Margins(left: 0.1, right: 0.9, top: 0.9, bottom: 0.1);
         plt.Layout.Frameless();
         // Установка квадратных единиц измерения
         plt.Axes.SquareUnits();

         wpfPlot.Refresh();
      }

      private void Points_3D()
      {
         viewport3D.Children.Clear();

         var pointsVisual = new PointsVisual3D
         {
            Points = new Point3DCollection(fileManager.points),
            Size = 4,
            Color = Colors.DodgerBlue
         };

         viewport3D.Children.Add(pointsVisual);
      }

      private void btnSave_Click(object sender, RoutedEventArgs e)
      {
         SaveFileDialog saveFileDialog = new SaveFileDialog();
         saveFileDialog.Filter = "PCD files (*.pcd)|*.pcd|All files (*.*)|*.*";

         bool? result = saveFileDialog.ShowDialog();

         if (result == true)
         {
            string filePath = saveFileDialog.FileName;
            try
            {
               fileManager.SavePCDAsync(filePath);
               fileManager.AddToRecentFiles(fileManager.RecentSaveFiles, filePath);
               UpdateRecentFilesListBox(lbLastSave, fileManager.RecentSaveFiles);
               MessageBox.Show("Файл успешно сохранен.");
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.Message);
            }
         }
      }



      private async void btnLoad_Click(object sender, RoutedEventArgs e)
      {
         OpenFileDialog openFileDialog = new OpenFileDialog();
         openFileDialog.Filter = "PCD files (*.pcd)|*.pcd|All files (*.*)|*.*";

         // Открытие диалогового окна в UI-потоке
         bool? result = openFileDialog.ShowDialog();

         if (result == true)
         {
            string fullPath = openFileDialog.FileName;
            string fileName = Path.GetFileName(fullPath);
            string filePath = Path.GetDirectoryName(fullPath);

            try
            {
               // Загрузка выбранного файла и обновление интерфейса
               await LoadPointAsync(fileName, filePath, fullPath);
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.Message);
            }
         }
      }


      private async Task LoadPointAsync(string fileName, string filePath, string fullPath)
      {
         UpdateFileInfoExpander(fullPath); // Обновление информации о файле в Expander
         await fileManager.LoadPCDAsync(fullPath); // Загрузка выбранного файла
         fileManager.AddToRecentFiles(fileManager.RecentLoadFiles, fullPath); // Добавление файла в список последних загруженных файлов
         UpdateRecentFilesListBox(lbLastLoad, fileManager.RecentLoadFiles); // Обновление списка последних загруженных файлов в интерфейсе
         Points_2D(); // Обновление 2D графика
         Points_3D(); // Обновление 3D графика
      }

      private void UpdateFileInfoExpander(string fullPath)
      {
         FileInfo fileInfo = new FileInfo(fullPath);
         // Обновление имени файла
         FileNameTextBlock.Text = "Имя файла: " + Path.GetFileNameWithoutExtension(fullPath);
         // Обновление пути файла
         FilePathTextBlock.Text = "Путь: " + Path.GetDirectoryName(fullPath);
         // Обновление расширения файла
         FileExtensionTextBlock.Text = "Расширение: " + Path.GetExtension(fullPath);
         // Получение размера файла в байтах
         long fileSizeInBytes = fileInfo.Length;
         // Получение форматированного размера файла
         string formattedFileSize = fileManager.GetFormattedFileSize(fileSizeInBytes);
         // Обновление размера файла
         FileWeightTextBlock.Text = "Размер: " + formattedFileSize;
      }



      private async void UpdateRecentFilesListBox(ListBox listBox, List<string> recentFiles)
      {
         listBox.Items.Clear();
         foreach (string file in recentFiles)
         {
            string fileName = Path.GetFileName(file);
            string path = Path.GetDirectoryName(file).ToLower();

            listBox.Items.Add(new { FileName = fileName, Path = path});
         }
      }

      private async void lbLastLoad_SelectionChanged(object sender, SelectionChangedEventArgs e)
      {
         ListBox listBox = sender as ListBox;
         if (listBox.SelectedItem != null)
         {
            var selectedItem = listBox.SelectedItem as dynamic;
            string filePath = selectedItem.Path;
            string fileName = selectedItem.FileName;
            string fullPath = Path.Combine(filePath, fileName);
            if (!File.Exists(fullPath))
            {
               MessageBox.Show($"Файл {fullPath} не найден.", "Файл отсутствует", MessageBoxButton.OK);
            }
            else
            {
               // Загрузка выбранного файла и обновление интерфейса
               await LoadPointAsync(fileName, filePath, fullPath);
            }
         }
      }



   }
}





