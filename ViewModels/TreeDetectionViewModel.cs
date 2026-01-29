using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReactiveUI;
using IAFTS.Models;
using IAFTS.Services;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Linq;
using IAFTS.Views;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.IO;
using System.Collections.ObjectModel;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.ComponentModel.DataAnnotations.Schema;

namespace IAFTS.ViewModels
{
    public class TreeDetectionViewModel : ReactiveObject
    {
        private readonly SearchScriptService _searchScriptService;
        private readonly InferenceScriptService _inferenceScriptService;
        private readonly View3DSercive _view3DSercive;

        private LidarData _lidarData;

        private Window? _window;

        private Bitmap? _image;

        public Bitmap? Image
        {
            get => _image;
            set => this.RaiseAndSetIfChanged(ref _image, value);
        }

        private ObservableCollection<MyData>? _data;

        public ObservableCollection<MyData>? Data
        {
            get => _data;
            set => this.RaiseAndSetIfChanged(ref _data, value);
        }

        
        public TreeDetectionViewModel()
        {
            Console.WriteLine("TreeDetectionViewModel создан");
            _searchScriptService = new SearchScriptService("search_script/search_script.py");
            _inferenceScriptService = new InferenceScriptService("inference/inference.py");
            _view3DSercive = new View3DSercive("view_3d_script.py");
            _lidarData = new LidarData
            {
                Trees = new List<Tree>()
            };

            LoadLasCommand = ReactiveCommand.CreateFromTask(ExecuteLoadLas);
            LoadTiffCommand = ReactiveCommand.CreateFromTask(ExecuteLoadTiff);
            ProcessDataCommand = ReactiveCommand.CreateFromTask(ExecuteProcessData);
            SaveResultsCommand = ReactiveCommand.CreateFromTask(ExecuteSaveResults);
            View3DCommand = ReactiveCommand.CreateFromTask(ExecuteView3D);
        }

        public Window? Window
        {
            get => _window;
            set 
            {
                Console.WriteLine($"Устанавливаем Window в TreeDetectionViewModel: {value != null}");
                _window = value;
            }
        }

        public LidarData LidarData
        {
            get => _lidarData;
            set => this.RaiseAndSetIfChanged(ref _lidarData, value);
        }

        public ReactiveCommand<Unit, Unit> LoadLasCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadTiffCommand { get; }
        public ReactiveCommand<Unit, Unit> ProcessDataCommand { get; }

        public ReactiveCommand<Unit, Unit> View3DCommand { get; }

        private async Task ExecuteLoadLas()
        {
            Console.WriteLine("Нажата кнопка загрузки LAS");
            if (Window == null)
            {
                Console.WriteLine("Ошибка: Window не инициализирован");
                return;
            }

            Console.WriteLine("Создаем диалог выбора файла");
            var options = new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("LAS Files") { Patterns = new[] { "*.las" } }
                }
            };

            try
            {
                var result = await Window.StorageProvider.OpenFilePickerAsync(options);
                if (result.Count > 0)
                {
                    LidarData.LasFilePath = result[0].Path.LocalPath;
                    Console.WriteLine($"Выбран файл LAS: {LidarData.LasFilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выборе файла LAS: {ex.Message}");
            }
        }

        private async Task ExecuteLoadTiff()
        {
            Console.WriteLine("Нажата кнопка загрузки TIFF");
            if (Window == null)
            {
                Console.WriteLine("Ошибка: Window не инициализирован");
                return;
            }

            Console.WriteLine("Создаем диалог выбора файла");
            var options = new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("TIFF Files") { Patterns = new[] { "*.tif", "*.tiff" } }
                }
            };

            try
            {
                var result = await Window.StorageProvider.OpenFilePickerAsync(options);
                if (result.Count > 0)
                {
                    LidarData.TiffFilePath = result[0].Path.LocalPath;
                    Console.WriteLine($"Выбран файл TIFF: {LidarData.TiffFilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выборе файла TIFF: {ex.Message}");
            }
        }

        private async Task ExecuteProcessData()
        {
            try
            {
                if (string.IsNullOrEmpty(LidarData.LasFilePath) || string.IsNullOrEmpty(LidarData.TiffFilePath))
                {
                    throw new InvalidOperationException("Не выбраны входные файлы (LAS и TIFF)");
                }

                // Определяем рабочую папку — ту же, что у исходного TIFF (или LAS)
                string workDir = System.IO.Path.GetDirectoryName(LidarData.TiffFilePath) ?? System.IO.Path.GetDirectoryName(LidarData.LasFilePath) ?? Environment.CurrentDirectory;

                // Формируем пути для shp, csv, image
                LidarData.ShpFilePath = System.IO.Path.Combine(workDir, "result.shp");
                LidarData.CsvFilePath = System.IO.Path.Combine(workDir, "result.csv");
                LidarData.ImageFilePath = System.IO.Path.Combine(workDir, "result.jpg");

                Console.WriteLine($"Рабочая папка: {workDir}");
                Console.WriteLine($"Файл LAS: {LidarData.LasFilePath}");
                Console.WriteLine($"Файл TIFF: {LidarData.TiffFilePath}");
                Console.WriteLine($"Файл SHP: {LidarData.ShpFilePath}");
                Console.WriteLine($"Файл CSV: {LidarData.CsvFilePath}");
                Console.WriteLine($"Файл IMAGE: {LidarData.ImageFilePath}");

                await _inferenceScriptService.ProcessDataAsync(LidarData);
                LidarData.ShpFilePath = LidarData.OutputPath + "/output.shp";

                string name = Path.GetFileName(LidarData.TiffFilePath);
                Image = new Bitmap($"model_output\\predict\\{name.Replace(".tif", ".jpg")}");
                await _searchScriptService.ProcessDataAsync(LidarData);

                var filePath = $"{LidarData.OutputPath}\\output.csv";
                var dataList = CsvHelperExtensions.ReadCsv<MyData>(filePath);

                Data = new ObservableCollection<MyData>(dataList);

                var dialog = new InfoDialog("Данные успешно обработаны!");
                dialog.Show(Window);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                throw;
            }
        }

        public ReactiveCommand<Unit, Unit> SaveResultsCommand { get; }

        private async Task ExecuteSaveResults()
        {
            if (Window == null)
            {
                Console.WriteLine("Ошибка: Window не инициализирован");
                return;
            }

            var options = new FolderPickerOpenOptions
            {
                Title = "Выберите папку для сохранения результатов"
            };
            var folders = await Window.StorageProvider.OpenFolderPickerAsync(options);
            if (folders.Count == 0)
                return;

            string targetDir = folders[0].Path.LocalPath;
            LidarData.OutputPath = targetDir;
            //// Копируем все нужные файлы
            //void CopyIfExists(string? from, string toName)
            //{
            //    if (!string.IsNullOrEmpty(from) && System.IO.File.Exists(from))
            //    {
            //        string dest = System.IO.Path.Combine(targetDir, toName);
            //        System.IO.File.Copy(from, dest, overwrite: true);
            //        Console.WriteLine($"Скопирован {from} -> {dest}");
            //    }
            //}

            //CopyIfExists(LidarData.ShpFilePath, "result.shp");
            //CopyIfExists(LidarData.CsvFilePath, "result.csv");
            //CopyIfExists(LidarData.ImageFilePath, "result.jpg");
            //// Можно добавить копирование других файлов, если потребуется

            //// Показываем собственное окно InfoDialog
            //var dialog = new InfoDialog("Файлы успешно сохранены!");
            //// Используем Show(Window) вместо ShowDialog(Window), чтобы не блокировать UI
            //dialog.Show(Window);
        }

        private async Task ExecuteView3D()
        {
            await _view3DSercive.ProcessDataAsync(LidarData);
        }
    }
}