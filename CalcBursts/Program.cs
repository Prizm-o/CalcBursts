using System.Text;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.ImageSharp;

class Program
{
    static void Main(string[] args)
    {
        //Смена кодировки для корректного считывания кириллицы
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        int rangeCount = 0; //Количество диапазонов
        while (true)
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

        int bufferSize = 5000;

        //Путь к фалу с данными
        /*
        Console.WriteLine("Введите путь к файлу .csv (без кавычек):");
        string filePath = Console.ReadLine();
        */
        string filePath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Templates", "lobz0.csv");

        ProcessData(filePath, ranges, bufferSize);
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

    static void ProcessData(string filePath, double[] ranges, int bufferSize)
    {
        try
        {
            var spikeCounts = new int[ranges.Length / 2];   //Количество всплесков в диапозоне
            string line;
            int countLines = 0;
            int column = 0;
            List<int> peaks = new List<int>();
            List<double> tempS = new List<double>();
            List<double> signalData = new List<double>();
            List<double> time = new List<double>();
            
            using (var reader = new StreamReader(filePath))
            {
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
                    if (countLines > 1)
                    {
                        if (double.TryParse(values[column], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double voltage))
                        { 
                            tempS.Add(voltage);
                            signalData.Add(voltage);
                        }
                        if (double.TryParse(values[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double Time))
                        { 
                            time.Add(Time);
                        }
                        if (tempS.Count() == bufferSize)
                        {
                            double[] signal = tempS.ToArray();
                            double[] filtered = tempS.ToArray();

                            filtered = FiltSignal(signal);
                            for (int i = 0; i < ranges.Length / 2; i++)
                            {
                                var peaksTemp = FindPeaks(filtered, ranges[i * 2], ranges[i * 2 + 1]);
                                peaks.AddRange(peaksTemp);
                                spikeCounts[i] = spikeCounts[i] + peaksTemp.Count();
                            }
                            tempS.Clear();
                        }
                    }
                }
            }

            double[] signalDop = signalData.ToArray();
            double[] timeData = time.ToArray();
            double[] filteredDop = tempS.ToArray();

            if (tempS.Count() > 0)
            {
                filteredDop = FiltSignal(signalDop);
                for (int i = 0; i < ranges.Length / 2; i++)
                {
                    var peaksTemp = FindPeaks(filteredDop, ranges[i * 2], ranges[i * 2 + 1]);
                    peaks.AddRange(peaksTemp);
                    spikeCounts[i] = spikeCounts[i] + peaksTemp.Count();
                }
            }

            // Вывод результатов
            for (int i = 0; i < spikeCounts.Length; i++)
            {
                Console.WriteLine($"Количество всплесков в диапазоне [{ranges[i * 2]}, {ranges[i * 2 + 1]}]: {spikeCounts[i]}");
            }
            /* 
            //Код для рисования графика
            double[] filteredSignal = FiltSignal(signalDop);
            DrawGraf(timeData, filteredSignal, peaks);
            */
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке файла: {ex.Message}");
        }
    }

    static List<int> FindPeaks(double[] signal, double edjeBot, double edjeTop)
    {
        List<int> Peaks = new List<int>();

        for (int i=1; i < signal.Count() - 1; i++)
        {
            if (signal[i] >= edjeBot && signal[i] <= edjeTop)
            {
                if (signal[i] > signal[i - 1] && signal[i] > signal[i + 1])
                {
                    Peaks.Add(i);
                }
            }
        }
        return Peaks;
    }

    static double[] FiltSignal (double[] signal)
    {
        List<double> tempFilt = new List<double>();
        List<double> tempPoints = new List<double>();

        bool spikeFinded = false;
        bool waitSpike = false;

        try
        {
            for (int i=1; i < signal.Count() - 1; i++)
            {
                if (!waitSpike)
                {
                    if (signal[i] <= signal[i-1])
                    {
                        tempPoints.Add(signal[i-1]);
                    }
                    else if (signal[i] > signal[i-1] && tempPoints.Count() != 0)
                    {
                        waitSpike = true;
                    }
                }
                else if (!spikeFinded)
                {
                    if (signal[i] <= signal[i-1])
                    {
                        tempPoints.Add(signal[i - 1]);
                        spikeFinded = true;
                    }
                }
                else if(!waitSpike && !spikeFinded)
                {
                    tempFilt.Add(signal[i]);
                }
                if (spikeFinded)
                {
                    tempFilt.Add((tempPoints[0] + tempPoints[tempPoints.Count() - 1]) / 2);
                    tempPoints.Clear();
                    spikeFinded = false;
                    waitSpike = false;
                }                
            }
            if (tempFilt.Count() != 0)
            {
                signal = tempFilt.ToArray();
            }
            
            spikeFinded = false;
            waitSpike = false;
            tempFilt.Clear();
            tempPoints.Clear();
            double tempSpike = 0;
            
            for (int i=1; i < signal.Count() - 1; i++)
            {
                if (!waitSpike)
                {
                    if (signal[i] >= signal[i-1])
                    {
                        tempPoints.Add(signal[i - 1]);
                    }
                    else if (signal[i] < signal[i-1])
                    {
                        tempSpike = signal[i - 1];
                        waitSpike = true;
                    }
                }
                else if (!spikeFinded)
                {
                    if (signal[i] > signal[i-1])
                    {
                        tempPoints.Add(signal[i - 1]);
                        spikeFinded = true;
                    }
                }
                else if(!waitSpike && !spikeFinded)
                {
                    tempFilt.Add(signal[i]);
                }
                if (spikeFinded)
                {
                    tempFilt.Add(tempSpike);
                    tempPoints.Clear();
                    tempPoints.Add(signal[i]);
                    spikeFinded = false;
                    waitSpike = false;
                }
            }
            if (tempFilt.Count() != 0)
            {
                signal = tempFilt.ToArray();
            }
            
            spikeFinded = false;
            tempFilt.Clear();
            tempPoints.Clear();

            for (int i=1; i < signal.Count() - 1; i++)
            {
                if (signal[i] >= signal[i-1])
                {
                    tempPoints.Add(signal[i-1]);
                }
                else if (signal[i] < signal[i-1] && tempPoints.Count() != 0)
                {
                    tempPoints.Add(signal[i - 1]);
                    spikeFinded = true;
                }
                else if (tempPoints.Count() == 0)
                {
                    tempFilt.Add(signal[i]);
                }
                if (spikeFinded)
                {
                    tempFilt.Add(tempPoints[0]);
                    tempFilt.Add(tempPoints[tempPoints.Count() - 1]);
                    tempPoints.Clear();
                    spikeFinded = false;
                }
            }
            if (tempFilt.Count() != 0)
            {
                signal = tempFilt.ToArray();
            }

            return signal = tempFilt.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка в цикле 3 фильтра "+ex.Message);
            return null;
        }        
    }

    static void DrawGraf (double[] time, double[] filteredSignal, List<int> peaks)
    {
        var plotModel = new PlotModel { Title = "Signal Peaks Detection" };
        //var originalSeries = new LineSeries { Title = "Original Signal", Color = OxyColors.Blue };
        var filteredSeries = new LineSeries { Title = "Filtered Signal", Color = OxyColors.Orange };
        var peaksSeries = new ScatterSeries { Title = "Detected Peaks", MarkerType = MarkerType.Circle, MarkerFill = OxyColors.Red };
        try
        {
            /*
            for (int i = 0; i < signal.Length - 1; i++)
            {   
                originalSeries.Points.Add(new DataPoint(time[i], signal[i]));
            }
            */
            for (int i = 0; i < filteredSignal.Length - 1; i++)
            {   
                filteredSeries.Points.Add(new DataPoint(time[i], filteredSignal[i]));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при построении графиков "+ex.Message);
        }

        foreach (var peak in peaks)
        {
            peaksSeries.Points.Add(new ScatterPoint(time[peak], filteredSignal[peak]));
        }

        //plotModel.Series.Add(originalSeries);
        plotModel.Series.Add(filteredSeries);
        plotModel.Series.Add(peaksSeries);

        PngExporter.Export(plotModel, @"C:\Users\Admin\Desktop\plot.png", 3840, 2160);

        Console.WriteLine("Файл сохранен по пути "+@"C:\Users\Admin\Desktop\plot.png");
    }
}
