using MathNet.Numerics.Distributions;
using MihStatLibrary;
using MihStatLibrary.Calculators;
using MihStatLibrary.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MetodikaLib.Constants;

namespace MetodikaLib.Tests
{
    internal class PolynomialAgreement : ITestable
    {
        private List<double> _statistics;
        private List<double> _pValues;
        private MarkTable _markTableParent;
        private bool? _isSuccess;
        private bool _isFirstCalculationLoop;
        private int _kMax;
        private List<bool> _results;

        /// <summary>
        /// Событие изменения прогресса проведения теста
        /// </summary>
        public event EventHandler<int>? ProgressChanged;

        /// <summary>
        /// Событие изменения этапа проведения теста
        /// </summary>
        public event EventHandler<string>? ProcessChanged;

        public PolynomialAgreement()
        {
            _isSuccess = null;
            _kMax = 0;
            _results = new List<bool>();
            _pValues = new List<double>();
            _statistics = new List<double>();
            _isFirstCalculationLoop = true;
            _markTableParent = new MarkTable(Constants.MAX_S, 1);
            _markTableParent.ProcessChanged += _onProcessChanged!;
            _markTableParent.ProgressChanged += _onProgressChanged!;
        }

        //ToDelete
        //public void Print()
        //{
        //    Console.WriteLine($"Тест 2.3: Согласие");
        //    for (int i = 0; i < _lstStatistic.Count; i++)
        //    {
        //        Console.WriteLine($"k = {i + 2}; Pk = {_lstStatistic[i]}");
        //    }
        //}

        public void Test(string pFileName, double pAlpha)
        {
            _isSuccess = true;
            try
            {
                _kMax = _getK(pFileName);
                _calculate(pFileName, _kMax);
                foreach (var element in _pValues)
                {
                    if (element < pAlpha)
                    {
                        _results.Add(false);
                        _isSuccess = false;
                    }
                    else
                    {
                        _results.Add(true);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                _isSuccess = false;
            }
        }

        /// <summary>
        /// Формирование строки результата тестирования
        /// </summary>
        /// <returns>Строка результата тестирования</returns>
        public override string ToString()
        {
            string strResult = string.Empty;
            strResult = $"\nТест 2.3: Проверка согласия распределения числа k-грамм в исходной последовательности с полиномиальным законом\t";
            strResult = _isSuccess == true ? $"{strResult}Kmax = {_kMax}\t" : $"{strResult}Kmax = Не определено\t";
            strResult = $"{strResult}K\tS^(3)_k\tP^(1)_k\nРезультат\t";
            strResult = _isSuccess == true ? $"{strResult}Тест пройден\t" : $"{strResult}Тест не пройден\t";

            strResult = $"{strResult}{2}\t{_statistics[0]}\t{_pValues[0]}";
            for (int i = 1; i < _statistics.Count; i++)
            {
                strResult = $"{strResult}\n\t\t{i + 2}\t{_statistics[i]}\t{_pValues[i]}\t";
                strResult = _results[i] == true ? $"{strResult}" : $"{strResult}Fail";
            }

            strResult = $"{strResult}\n\n";
            return strResult;
        }

        //ToDelete
        //public void PrintInFile(string pFileName)
        //{
        //    Stream fs = new FileStream(pFileName, FileMode.Append, FileAccess.Write);
        //    using (BinaryWriter bw = new BinaryWriter(fs))
        //    {
        //        string str = $"Test 2.3: ";
        //        for (int i = 0; i < _statistics.Count; i++)
        //        {
        //            str += $"\t{i + 2}\t{_statistics[i]}\t{_pValues[i]}";
        //        }
        //        bw.Write(str);
        //        fs.Close();
        //    }
        //}

        /// <summary>
        /// Получение k
        /// </summary>
        /// <param name="pFileName">Файл с гаммой</param>
        /// <returns>Значение k</returns>
        private int _getK(string pFileName)
        {
            _checkGamma(pFileName);

            return _getSMax(pFileName);
        }

        /// <summary>
        /// Получение максимального s
        /// </summary>
        /// <returns>Максимального s</returns>
        private int _getSMax(String pFileName)
        {
            object locker = new object();

            int nmTokenSource = Constants.MAX_S - Constants.MIN_S;
            CancellationTokenSource[] ctsArray = new CancellationTokenSource[nmTokenSource];
            CancellationToken[] tokenArray = new CancellationToken[nmTokenSource];

            int sMax = Constants.MIN_S;

            for (int i = 0; i < nmTokenSource; i++)
            {
                ctsArray[i] = new CancellationTokenSource();
                tokenArray[i] = ctsArray[i].Token;
            }

            List<Task> taskList = new List<Task>();

            for (int dimension = Constants.MAX_S, i = 0; dimension > Constants.MIN_S; dimension--, i++)
            {
                int dimTemp = dimension;
                MarkTable fqSingleThread = new MarkTable(dimTemp);
                fqSingleThread.ProcessChanged += _onProcessChanged!;
                fqSingleThread.ProgressChanged += _onProgressChanged!;
                taskList.Add(Task.Run(() =>
                {
                    int threadIndexSaver = fqSingleThread.Dimension - (Constants.MIN_S + 1);
                    fqSingleThread.Calculate(pFileName, tokenArray[threadIndexSaver]);

                    if (fqSingleThread.Table.Min() >= 20)
                    {
                        lock (locker)
                        {
                            if (sMax < fqSingleThread.Dimension)
                            {
                                sMax = fqSingleThread.Dimension;
                                for (int j = threadIndexSaver; j >= 0; j--)
                                {
                                    ctsArray[j].Cancel();
                                }
                            }
                        }
                    }
                }));
            }

            Task.WaitAll(taskList.ToArray());
            foreach (var cts in ctsArray)
            {
                cts.Dispose();
            }
            _isFirstCalculationLoop = false;
            return sMax;
        }

        /// <summary>
        /// Проверка гаммы наличие более 20 k-грамм каждого вида, при k=2..6
        /// </summary>
        private void _checkGamma(string pFileName)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            List<Task> taskList = new List<Task>();
            ProcessChanged?.Invoke(this, "Проверка для k = 2..6");
            for (int dimension = Constants.MIN_K_PA; dimension <= Constants.MIN_S; dimension++)
            {
                int dimTemp = dimension;
                MarkTable fqSingleThread = new MarkTable(dimTemp);
                fqSingleThread.ProcessChanged += _onProcessChanged!;
                fqSingleThread.ProgressChanged += _onProgressChanged!;
                taskList.Add(Task.Run(() =>
                {
                    fqSingleThread.Calculate(pFileName, token);

                    if (fqSingleThread.Table.Min() < 20)
                    {
                        cts.Cancel();
                        _isSuccess = false;
                        throw new PolynomialAgreementException($"Количество знаков при k = {dimTemp} < 20. ФДСЧ не забракован. Требуется тестирование на новой последовательности");
                    }
                }));
            }
            Task.WaitAll(taskList.ToArray());
            cts.Dispose();
        }

        /// <summary>
        /// Вычисление статистик
        /// </summary>
        /// <param name="pFileName">Файл с гаммой</param>
        /// <param name="pKMax">Максимальная размерность k</param>
        private void _calculate(string pFileName, int pKMax)
        {
            _pValues = new List<double>();
            _statistics = new List<double>();
            for (int i = 0; i < pKMax - 1; i++)
            {
                _pValues.Add(0);
                _statistics.Add(0);
            }

            List<Task> taskList = new List<Task>();

            for (int i = 2; i <= pKMax; i++)
            {
                MarkTable fqChild = new MarkTable(i);
                fqChild.ProcessChanged += _onProcessChanged!;
                fqChild.ProgressChanged += _onProgressChanged!;

                Task task = new Task(() => calcFreqHistForSingleThread(fqChild, pFileName));
                taskList.Add(task);
                task.Start();

            }

            Task.WaitAll(taskList.ToArray());


        }

        private void calcFreqHistForSingleThread(MarkTable fqChild, string pFileName)
        {
            double dTetta = 0;
            double dSk = 0;
            double dPi = 0;

            int dimension = fqChild.Dimension;

            fqChild.Calculate(pFileName);

            for (int j = 0; j < fqChild.Table.Length; j++)
            {
                dTetta += fqChild.Table[j] * OnesCalculator.Calculate(j);
            }
            dTetta /= (fqChild.Dimension * fqChild.NmVectors);



            for (int j = 0; j < (1 << dimension) - 1; j++)
            {
                dPi = Math.Pow(dTetta, OnesCalculator.Calculate(j)) * Math.Pow((1 - dTetta), dimension - OnesCalculator.Calculate(j));
                dSk += Math.Pow((fqChild.Table[j] - dPi * fqChild.NmVectors), 2) / (dPi * fqChild.NmVectors);
#if (!DEBUG)
				//EventProgressPercent(this, SupportFunction.GetPercent(j + 1, (1 << dimension) - 1));
#endif
            }
            _statistics[dimension - 2] = dSk;
            _pValues[dimension - 2] = (1 - ChiSquared.CDF((1 << dimension) - 2, dSk));
        }

        private void _onProgressChanged(object sender, int e)
        {

            if (((MarkTable)sender).Dimension == 3 /*|| ((FreqHistogram)sender).IDimension == 15*/)
            {
                ProgressChanged?.Invoke(this, e);
            }
        }

        private void _onProcessChanged(object sender, string e)
        {
            if (((MarkTable)sender).Dimension == 3 || (((MarkTable)sender).Dimension == 15 && _isFirstCalculationLoop == true))
            {
                ProcessChanged?.Invoke(this, e);
            }
        }
    }
}
