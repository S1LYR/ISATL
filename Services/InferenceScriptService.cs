using IAFTS.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace IAFTS.Services
{
    public class InferenceScriptService
    {
        private readonly string _scriptPath;

        public InferenceScriptService(string scriptPath)
        {
            // Преобразуем путь к скрипту в формат macOS
            _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, scriptPath);
        }

        public async Task ProcessDataAsync(LidarData data)
        {
            try
            {
                var tiffPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, data.TiffFilePath);
                var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, data.OutputPath ?? "output");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "python",  // На macOS обычно python3 вместо python
                    Arguments = $"{Path.GetFullPath(_scriptPath)} {Path.GetFullPath(tiffPath)} {Path.GetFullPath(outputPath)}",
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

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception($"Ошибка обработки данных: {error}");
                }

                // Добавляем логирование для отладки
                Console.WriteLine($"Python скрипт завершился успешно");
                Console.WriteLine($"Вывод: {output}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при обработке данных: {ex.Message}", ex);
            }
        }
    }
}
