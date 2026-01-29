using IAFTS.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IAFTS.Services
{
    public class View3DSercive
    {
        private readonly string _scriptPath;

        public View3DSercive(string scriptPath)
        {
            // Преобразуем путь к скрипту в формат macOS
            _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptPath);
        }

        public async Task ProcessDataAsync(LidarData data)
        {
            var lasPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, data.LasFilePath);
            var csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{data.OutputPath}\\output.csv" ?? "output");

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",  // На macOS обычно python3 вместо python
                Arguments = $"{Path.GetFullPath(_scriptPath)} {Path.GetFullPath(lasPath)} {Path.GetFullPath(csvPath)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // Чтение вывода
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            // Добавляем логирование для отладки
            Console.WriteLine($"Python скрипт завершился успешно");
            Console.WriteLine($"Вывод: {output}");
        }
    }
}
