using System;
using System.IO;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        //Смена кодировки для корректного считывания кириллицы
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        int rangeCount = 0; //Количество диапазонов
        while  (true)
        {
            Console.WriteLine("Введите количество диапазонов (1-16):");
            rangeCount = int.Parse(Console.ReadLine());
            if (rangeCount > 0 & rangeCount <= 16)
            {
                break;
            }
            else Console.WriteLine("Введёное число выходит за диапозон");
        }
        Console.WriteLine("Введите множитель для сдвига значения диапазона (например 3):");
        double rangeMultiplier = double.Parse(Console.ReadLine()); //Множитель сдвига (например при указании 3 значения для диапазона сдвинутся влево на 3)
        if (rangeMultiplier == 0) 
        { 
            rangeMultiplier=1; 
        }
        else {
            rangeMultiplier = Math.Pow(10, rangeMultiplier);
        }

        double[] ranges = new double[rangeCount * 2];   //Нижние и верхние границы диапазона

        for (int i = 0; i < rangeCount; i++)
        {
            Console.WriteLine($"Введите нижнюю границу диапазона {i + 1}:");
            ranges[i * 2] = ParseInput(Console.ReadLine(), rangeMultiplier);

            Console.WriteLine($"Введите верхнюю границу диапазона {i + 1}:");
            ranges[i * 2 + 1] = ParseInput(Console.ReadLine(), rangeMultiplier);
        }
/*
        //Путь к фалу с данными
        Console.WriteLine("Введите путь к файлу .csv (без кавычек):");
        string filePath = Console.ReadLine();
*/
        string filePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Templates", "lobz0.csv");

        ProcessData(filePath, ranges);
    }

    static double ParseInput(string input, double multiplier)
    {
        // Преобразуем строку в число, используя экспоненциальный формат
        if (double.TryParse(input, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double result))
        {
            return result / multiplier;
        }
        else
        {
            throw new FormatException("Неверный формат числа. Пожалуйста, используйте правильный формат.");
        }
    }

    static void ProcessData(string filePath, double[] ranges)
    {
        var spikeCounts = new int[ranges.Length / 2];   //Количество всплесков в диапозоне

        try
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                int countLines = 0;
                double previousPoint = 0;
                double currentPoint = 0;
                double nextPoint = 0;
                int column = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    countLines += 1;
                    var values = line.Split(',');
                    if (values.Length < 2) continue;

                    if (countLines == 1)
                    {
                        Console.WriteLine($"Введите номер столбца с данными для обработки [1-{values.Length}]:");
                        column = int.Parse(Console.ReadLine()) - 1;
                    }

                    // Парсим значение V
                    if (double.TryParse(values[column], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double voltage))
                    {
                        if (countLines == 1)
                        {
                            previousPoint = voltage;
                        }
                        if (countLines == 2)
                        {
                            currentPoint = voltage;
                        }
                        if (countLines >= 3)
                        {
                            if (countLines == 3)
                            {
                                nextPoint = voltage;
                            }
                            if (countLines > 3)
                            {
                                previousPoint = currentPoint;
                                currentPoint = nextPoint;
                                nextPoint = voltage;
                                for (int i = 0; i < ranges.Length / 2; i++)
                                {
                                    if (currentPoint >= ranges[i * 2] && currentPoint <= ranges[i * 2 + 1])
                                    {
                                        if (previousPoint < currentPoint && currentPoint > nextPoint)
                                        {
                                            spikeCounts[i]++;
                                        }
                                    }
                                }
                            }                    
                        }
                    }
                }
            }

            // Вывод результатов
            for (int i = 0; i < spikeCounts.Length; i++)
            {
                Console.WriteLine($"Количество всплесков в диапазоне [{ranges[i * 2]}, {ranges[i * 2 + 1]}]: {spikeCounts[i]}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке файла: {ex.Message}");
        }
    }
}
