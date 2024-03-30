using MihStatLibrary.Tables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static MetodikaLib.Tools;

namespace MetodikaLib.Tests
{
    /// <summary>
    /// Класс проверки гипотезы независимости знаков (Тест 2.1)
    /// </summary>
    public class IndependenceSigns
    {
        private int _beginK;
        private List<double> _pValues;
        private List<double> _statistics;
        private bool _isSuccess;
        private GammaType _type;

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
        public IndependenceSigns(int beginK = 1, GammaType type = GammaType.InputGamma)
        {
            _isSuccess = false;
            _beginK = beginK;
            _type = type;
            _pValues = new List<double>();
            _statistics = new List<double>();
        }

        /// <summary>
        /// Вывод данных в консоль
        /// </summary>
        //public void Print()
        //{
        //    Console.WriteLine($"Тест 2.1: Независимость");
        //    for (int i = 0; i < _statistics.Count; i++)
        //    {
        //        Console.WriteLine($"k = {i + 1}; Pk = {_statistics[i]}");
        //    }
        //}

        /// <summary>
        /// Функция рассчета статистики независимости знаков
        /// </summary>
        /// <param name="pFileName">Имя файла с данными</param>
        /// <returns>Результат теста</returns>
        public bool Test(string pFileName, double pAlpha)
        {
            double dPk = 0;

            try
            {
                MarkTable fqSource = new MarkTable(Tools.MIN_MAX_K + 1, 1);
                MarkTable fqTemp;
                fqSource.ProgressChanged += _onProgressChanged;
                fqSource.ProcessChanged += _onProcessChanged;
                fqSource.Calculate(pFileName);
                for (int dimension = _beginK; dimension <= Tools.MIN_MAX_K; dimension++)
                {

                    Console.WriteLine($"k = {dimension}");
                    //ComOleg 
                    fqTemp = new MarkTable(dimension + 1);
                    fqSource.Reduce(fqTemp);
                    //FreqHistogram fqTemp = new FreqHistogram(dimension + 1);
                    //fqTemp.EventReportProgress += _onProgressChanged;
                    //fqTemp.EventReportProcess += _onProcessChanged;
                    //fqTemp.Calculate(pFileName);
                    //if (_isDimensionMax(fqTemp) == true)
                    //{
                    //	_bIsSuccess = true;
                    //	return true;
                    //}

                    dPk = _getPk(fqTemp);
                    Console.WriteLine($"k = {dimension}; Pk = {dPk}");

                    if (dPk < pAlpha)
                    {
                        _isSuccess = false;
                        return false;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string eclapsedTime = String.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    Console.WriteLine("RunTime " + eclapsedTime);
                }
                _isSuccess = true;
                return true;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Запись статистик в файл (ToDelete)
        /// </summary>
        /// <param name="pFileName">Имя файла</param>
        public void PrintInFile(string pFileName)
        {
            Stream fs = new FileStream(pFileName, FileMode.Append, FileAccess.Write);
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

        public override string ToString()
        {
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

            strResult = _isSuccess == true ? strResult = $"{strResult}\n\n" : strResult = $"{strResult}\tFail\n\n";
            return strResult;
        }

        /// <summary>
        /// Рассчет p-value
        /// </summary>
        /// <param name="pFreqHistogram">Гистограмма частот</param>
        /// <returns>Значение p-value</returns>
        private double _getPk(MarkTable pFreqHistogram)
        {
            ProcessChanged(this, $"Рассчет массивов u и v для k = {pFreqHistogram.Dimension}");
            int iDimensionV = pFreqHistogram.Dimension - 1;
            double dPk;
            long[] arV = new long[1 << iDimensionV];
            long[] arU = new long[2];

            for (int i = 0; i < arV.Length; i++)
            {
                arV[i] = pFreqHistogram.Table[i * 2] + pFreqHistogram.Table[i * 2 + 1];
                arU[0] += pFreqHistogram.Table[i * 2];
                arU[1] += pFreqHistogram.Table[i * 2 + 1];
                //EventProgressPercent(this, SupportFunction.GetPercent(i + 1, arV.Length));
            }


            double dStatistic = _getStatistic(pFreqHistogram, arV, arU);
            _statistics.Add(dStatistic);
            dPk = 1 - ChiSquared.CDF((1 << iDimensionV) - 1, dStatistic);
            _pValues.Add(dPk);

            return dPk;
        }

        /// <summary>
        /// Рассчет статистики
        /// </summary>
        /// <param name="pFreqHistogram">Гистограмма частот</param>
        /// <param name="pV">Массив v</param>
        /// <param name="pU">Массив u</param>
        /// <returns>Значение статистики</returns>
        private double _getStatistic(FreqHistogram pFreqHistogram, long[] pV, long[] pU)
        {
            ProcessChanged(this, $"Рассчет статистики для k = {pFreqHistogram.IDimension}");
            double dStatistic = 0;
            long lBufVector = 0;

            BigInteger biFirstProduct;
            BigInteger biSecondProduct;
            BigInteger biElementSquare;

            for (int i = 0; i < pV.Length; i++)
            {
                lBufVector = i << 1;
                for (int j = 0; j < 2; j++)
                {
                    biFirstProduct = (BigInteger)pFreqHistogram.LNmVectors * pFreqHistogram.ArHistogram[lBufVector];
                    biSecondProduct = (BigInteger)pV[i] * pU[j];
                    biElementSquare = biFirstProduct - biSecondProduct;
                    dStatistic += SupportFunction.BigIntDevider((BigInteger.Pow(biFirstProduct - biSecondProduct, 2)), biSecondProduct);
                    lBufVector |= 1;
                }
                ProgressChanged(this, SupportFunction.GetPercent(i + 1, pV.Length));
            }
            dStatistic /= pFreqHistogram.LNmVectors;

            return dStatistic;
        }

        /// <summary>
        /// Функция проверки максимального значения размерности векторов
        /// </summary>
        /// <param name="pFreqHistogram">Гистограмма частот</param>
        /// <returns>Результат проверки</returns>
        private bool _isDimensionMax(FreqHistogram pFreqHistogram)
        {
            ProcessChanged(this, $"Проверка k={pFreqHistogram.IDimension} на максимальность");
            for (int i = 0; i < pFreqHistogram.ArHistogram.Length; i++)
            {
                if (pFreqHistogram.ArHistogram[i] < Tools.WEAK_BORDER)
                {
                    return true;
                }
                ProgressChanged(this, SupportFunction.GetPercent(i + 1, pFreqHistogram.ArHistogram.Length));
            }

            return false;
        }

        private void _onProgressChanged(object sender, int e)
        {
            ProgressChanged(this, e);
        }

        private void _onProcessChanged(object sender, string e)
        {
            ProcessChanged(this, e);
        }

        /// <summary>
        /// Функция рассчета статистики независимости знако (ToDelete)
        /// </summary>
        /// <param name="pFileName">Имя файла с данными</param>
        /// <returns>Результат теста</returns>
        public bool TestOld(string pFileName, double pAlpha)
        {
            double dPk = 0;

            Stopwatch stopWatch = new Stopwatch();


            try
            {
                for (int dimension = _beginK; dimension < Tools.MIN_MAX_K; dimension++)
                {
                    stopWatch.Start();

                    Console.WriteLine($"k = {dimension}");
                    FreqHistogram freqHistogram = new FreqHistogram(dimension + 1, 1);
                    freqHistogram.Calculate(pFileName);

                    if (_isDimensionMax(freqHistogram) == true)
                    {
                        _isSuccess = true;
                        return true;
                    }

                    dPk = _getPk(freqHistogram);
                    Console.WriteLine($"k = {dimension}; Pk = {dPk}");

                    if (dPk < pAlpha)
                    {
                        _isSuccess = false;
                        return false;
                    }

                    stopWatch.Stop();
                    TimeSpan ts = stopWatch.Elapsed;
                    string eclapsedTime = String.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    Console.WriteLine("RunTime " + eclapsedTime);
                }
                _isSuccess = true;
                return true;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
