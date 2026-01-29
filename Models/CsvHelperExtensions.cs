using CsvHelper;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
using CsvHelper.Configuration;

public static class CsvHelperExtensions
{
    public static List<T> ReadCsv<T>(string filePath)
    {
        try
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",", // Разделитель - точка с запятой
                HasHeaderRecord = true, // Первая строка - заголовки
                                        // Другие настройки
            };

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<T>().ToList();
                return records;
            }
        }
        catch (Exception ex)
        {
            // Обработка ошибок при чтении CSV файла (логирование, вывод сообщения)
            Console.WriteLine($"Error reading CSV file: {ex.Message}");
            return new List<T>(); // Возвращаем пустой список в случае ошибки
        }
    }
}
