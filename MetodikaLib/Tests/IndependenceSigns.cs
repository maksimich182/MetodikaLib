using MathNet.Numerics.Distributions;
using MihStatLibrary;
using MihStatLibrary.Tables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static MetodikaLib.Constants;

namespace MetodikaLib.Tests
{
    /// <summary>
    /// Класс проверки гипотезы независимости знаков (Тест 2.1/3.2)
    /// </summary>
    public class IndependenceSigns : ITestable
    {
        private int _beginK;
        private List<double> _pValues;
        private List<double> _statistics;
        private bool? _isSuccess;
        private GammaType _type;
        private bool _autoBreak;

        /// <summary>
        /// Событие изменения прогресса проведения теста
        /// </summary>
        public event EventHandler<int>? ProgressChanged;

        /// <summary>
        /// Событие изменения этапа проведения теста
        /// </summary>
        public event EventHandler<string>? ProcessChanged;

        /// <summary>
        /// Конструктор класса проверки гипотезы независимости знаков 
        /// </summary>
        /// <param name="beginK">Значение k с которого начинаем рассчеты</param>
        /// <param name="type">Тип гаммы</param>
        /// <param name="autoBreak">Если true - рассчеты завершатся в момент провала теста, если false - рассчеты будут проходить до вычисленного kMax</param>
        public IndependenceSigns(int beginK = 1, GammaType type = GammaType.InputGamma, bool autoBreak = true)
        {
            _isSuccess = null;
            _beginK = beginK;
            _type = type;
            _pValues = new List<double>();
            _statistics = new List<double>();
            _autoBreak = autoBreak;
        }

        /// <summary>
        /// Функция тестирования независимости знаков
        /// </summary>
        /// <param name="fileName">Имя файла с данными</param>
        public void Test(string fileName, double alpha)
        {
            double Pk = 0;

            MarkTable markTableOriginal = new MarkTable(Constants.MIN_MAX_K + 1, 1);
            MarkTable markTableBuffer;
            markTableOriginal.ProgressChanged += _onProgressChanged!;
            markTableOriginal.ProcessChanged += _onProcessChanged!;
            markTableOriginal.Calculate(fileName);
            for (int dimension = _beginK; dimension <= Constants.MIN_MAX_K; dimension++)
            {

                markTableBuffer = new MarkTable(dimension + 1);
                markTableOriginal.Reduce(markTableBuffer);
                if (_autoBreak == true && _isDimensionMax(markTableBuffer) == true)
                {
                    _isSuccess = true;
                    return;

                }
                //ComOleg TODELETE
                //FreqHistogram fqTemp = new FreqHistogram(dimension + 1);
                //fqTemp.EventReportProgress += _onProgressChanged;
                //fqTemp.EventReportProcess += _onProcessChanged;
                //fqTemp.Calculate(pFileName);
                //if (_isDimensionMax(fqTemp) == true)
                //{
                //	_bIsSuccess = true;
                //	return true;
                //}

                Pk = _getPk(markTableBuffer);

                if (Pk < alpha)
                {
                    _isSuccess = false;
                    return;
                }
            }
            _isSuccess = true;
            return;
        }

        /// <summary>
        /// Запись статистик в файл (TODELETE)
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        public void PrintInFile(string fileName)
        {
            Stream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write);
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                string str = $"Test 2.1: ";
                for (int i = 0; i < _statistics.Count; i++)
                {
                    str += $"\t{i + 1}\t{_statistics[i]}\t{_pValues[i]}";
                }
                bw.Write(str);
                fs.Close();
            }
        }

        /// <summary>
        /// Формирование строки результата тестирования
        /// </summary>
        /// <returns>Строка результата тестирования</returns>
        public override string ToString()
        {
            if(_isSuccess is null)
            {
                return _type == GammaType.InputGamma ? "Тест 2.1 не проводился!\n" : "Тест 3.2 не проводился!\n";
            }

            string strResult = string.Empty;
            strResult = _type == GammaType.InputGamma ? $"\nТест 2.1: Проверка гипотезы независимости знаков в исходной последовательности\t" : $"\nТест 3.2: Проверка гипотезы независимости знаков в выходной последовательности\t";
            strResult = _isSuccess == true ? $"{strResult}Kmax = {_statistics.Count}\t" : $"{strResult}Kmax = Не определено\t";
            strResult = _type == GammaType.InputGamma ? $"{strResult}K\tS^(1)_k\tP^(1)_k\nРезультат\t" : $"{strResult}K\tT^(1)_k\tR^(1)_k\nРезультат\t";
            strResult = _isSuccess == true ? $"{strResult}Тест пройден\t" : $"{strResult}Тест не пройден\t";

            strResult = $"{strResult}{1}\t{_statistics[0]}\t{_pValues[0]}";
            for (int i = 1; i < _statistics.Count; i++)
            {
                strResult = $"{strResult}\n\t\t{i + 1}\t{_statistics[i]}\t{_pValues[i]}";
            }

            strResult = _isSuccess == true ? $"{strResult}\n\n" : $"{strResult}\tFail\n\n";
            return strResult;
        }

        /// <summary>
        /// Рассчет p-value
        /// </summary>
        /// <param name="markTable">Маркировочная таблица</param>
        /// <returns>Значение p-value</returns>
        private double _getPk(MarkTable markTable)
        {
            ProcessChanged?.Invoke(this, $"Рассчет массивов u и v для k = {markTable.Dimension}");
            int dimensionV = markTable.Dimension - 1;
            double Pk;
            long[] Vs = new long[1 << dimensionV];
            long[] Us = new long[2];

            for (int i = 0; i < Vs.Length; i++)
            {
                Vs[i] = markTable.Table[i * 2] + markTable.Table[i * 2 + 1];
                Us[0] += markTable.Table[i * 2];
                Us[1] += markTable.Table[i * 2 + 1];
                ProgressChanged?.Invoke(this, Tools.GetPercent(i + 1, Vs.Length));
            }


            double dStatistic = _getStatistic(markTable, Vs, Us);
            _statistics.Add(dStatistic);
            Pk = 1 - ChiSquared.CDF((1 << dimensionV) - 1, dStatistic);
            _pValues.Add(Pk);

            return Pk;
        }

        /// <summary>
        /// Рассчет статистики
        /// </summary>
        /// <param name="markTable">Маркировочная таблица</param>
        /// <param name="Vs">Массив v</param>
        /// <param name="Us">Массив u</param>
        /// <returns>Значение статистики</returns>
        private double _getStatistic(MarkTable markTable, long[] Vs, long[] Us)
        {
            ProcessChanged?.Invoke(this, $"Рассчет статистики для k = {markTable.Dimension}");
            double statistic = 0;
            long bufVector = 0;

            BigInteger firstProduct;
            BigInteger secondProduct;
            BigInteger elementSquare;

            for (int i = 0; i < Vs.Length; i++)
            {
                bufVector = i << 1;
                for (int j = 0; j < 2; j++)
                {
                    firstProduct = (BigInteger)markTable.NmVectors * markTable.Table[bufVector];
                    secondProduct = (BigInteger)Vs[i] * Us[j];
                    elementSquare = firstProduct - secondProduct;
                    statistic += Tools.BigIntDevider((BigInteger.Pow(firstProduct - secondProduct, 2)), secondProduct);
                    bufVector |= 1;
                }
                ProgressChanged?.Invoke(this, Tools.GetPercent(i + 1, Vs.Length));
            }
            statistic /= markTable.NmVectors;

            return statistic;
        }

        /// <summary>
        /// Функция проверки максимального значения размерности векторов
        /// </summary>
        /// <param name="markTable">Маркировочная таблица</param>
        /// <returns>Результат проверки</returns>
        private bool _isDimensionMax(MarkTable markTable)
        {
            ProcessChanged?.Invoke(this, $"Проверка k={markTable.Dimension} на максимальность");
            for (int i = 0; i < markTable.Table.Length; i++)
            {
                if (markTable.Table[i] < Constants.WEAK_BORDER)
                {
                    return true;
                }
                ProgressChanged?.Invoke(this, Tools.GetPercent(i + 1, markTable.Table.Length));
            }

            return false;
        }

        /// <summary>
        /// Функция вызова события изменения прогресса проведения теста
        /// </summary>
        /// <param name="sender">Объект изменения</param>
        /// <param name="e">Процент прогресса</param>
        private void _onProgressChanged(object sender, int e)
        {
            ProgressChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Функция вызова события изменения этапа проведения теста
        /// </summary>
        /// <param name="sender">Объект изменения</param>
        /// <param name="e">Название этапа</param>
        private void _onProcessChanged(object sender, string e)
        {
            ProcessChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Функция рассчета статистики независимости знаков (ToDelete)
        /// </summary>
        /// <param name="pFileName">Имя файла с данными</param>
        /// <returns>Результат теста</returns>
        //public bool TestOld(string pFileName, double pAlpha)
        //{
        //    double dPk = 0;

        //    Stopwatch stopWatch = new Stopwatch();


        //    try
        //    {
        //        for (int dimension = _beginK; dimension < Constants.MIN_MAX_K; dimension++)
        //        {
        //            stopWatch.Start();

        //            Console.WriteLine($"k = {dimension}");
        //            FreqHistogram freqHistogram = new FreqHistogram(dimension + 1, 1);
        //            freqHistogram.Calculate(pFileName);

        //            if (_isDimensionMax(freqHistogram) == true)
        //            {
        //                _isSuccess = true;
        //                return true;
        //            }

        //            dPk = _getPk(freqHistogram);
        //            Console.WriteLine($"k = {dimension}; Pk = {dPk}");

        //            if (dPk < pAlpha)
        //            {
        //                _isSuccess = false;
        //                return false;
        //            }

        //            stopWatch.Stop();
        //            TimeSpan ts = stopWatch.Elapsed;
        //            string eclapsedTime = String.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        //            Console.WriteLine("RunTime " + eclapsedTime);
        //        }
        //        _isSuccess = true;
        //        return true;
        //    }
        //    catch (FileNotFoundException ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        return false;
        //    }
        //}
    }
}
