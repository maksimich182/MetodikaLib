﻿using MathNet.Numerics.Distributions;
using MihStatLibrary;
using MihStatLibrary.Calculators;
using MihStatLibrary.Tables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MetodikaLib.Constants;

namespace MetodikaLib.Tests
{
    /// <summary>
    /// Класс проверки согласия распределения числа k-грамм в исходной последовательности с полиномиальным законом (Тест 2.3)
    /// </summary>
    public class PolynomialAgreement : ITestable
    {
        private List<double> _statistics;
        private List<double> _pValues;
        private MarkTable _markTableParent;
        private bool? _isSuccess;
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

        public void Test(string fileName, double alpha)
        {
            _checkGamma(fileName);
            _getKMax(fileName);
            _calculate(fileName);
            foreach (var element in _pValues)
            {
                if (element < alpha)
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
        /// Вычисление максимального k
        /// </summary>
        /// <param name="fileName">Файл с гаммой</param>
        /// <returns>Значение k</returns>
        private void _getKMax(string fileName)
        {
            int nmTokenSource = Constants.MAX_S - Constants.MIN_S;
            CancellationTokenSource[] ctss = new CancellationTokenSource[nmTokenSource];
            CancellationToken[] tokens = new CancellationToken[ctss.Length];

            for (int i = 0; i < ctss.Length; i++)
            {
                ctss[i] = new CancellationTokenSource();
                tokens[i] = ctss[i].Token;
            }

            List<Task> tasks = new List<Task>();
            int sMax = Constants.MIN_S;
            int startDimension = Constants.MIN_S + 1;
            object locker = new object();
            for (int dimension = startDimension; dimension <= Constants.MAX_S; dimension++)
            {
                int dimTemp = dimension;
                int threadIndexSaver = dimTemp - startDimension;
                tasks.Add(Task.Run(() =>
                {
                    MarkTable markTable = new MarkTable(dimTemp);
                    if (markTable.Dimension == 15)
                    {
                        markTable.ProcessChanged += _onProcessChanged!;
                        markTable.ProgressChanged += _onProgressChanged!;
                    }

                    markTable.Calculate(fileName, tokens[threadIndexSaver]);

                    if (markTable.Table.Min() >= Constants.WEAK_BORDER)
                    {
                        lock (locker)
                        {
                            if (sMax < markTable.Dimension)
                            {
                                sMax = markTable.Dimension;
                                for (int j = threadIndexSaver; j >= 0; j--)
                                {
                                    ctss[j].Cancel();
                                }
                            }
                        }
                    }
                }, tokens[threadIndexSaver]));
            }

            Task.WaitAll(tasks.ToArray());
            foreach (var cts in ctss)
            {
                cts.Dispose();
            }
            _kMax = sMax;
        }

        /// <summary>
        /// Проверка гаммы наличие более 20 k-грамм каждого вида, при k=2..6
        /// </summary>
        /// <param name="fileName">Файл с гаммой</param>
        /// <exception cref="PolynomialAgreementException">Исключение, вылетающее в случае, если в файле существуют отрезки бит,
        /// размерностью от 2 до 6 бит, которые встречаются меньше 20-ти раз в последовательности</exception>
        private void _checkGamma(string fileName)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            List<Task> tasks = new List<Task>();
            ProcessChanged?.Invoke(this, "Проверка для k = 2..6");
            for (int dimension = Constants.MIN_K_PA; dimension <= Constants.MIN_S; dimension++)
            {
                int dimTemp = dimension;
                tasks.Add(Task.Run(() =>
                {
                    MarkTable markTable = new MarkTable(dimTemp);
                    if (dimTemp == 3)
                    {
                        markTable.ProcessChanged += _onProcessChanged!;
                        markTable.ProgressChanged += _onProgressChanged!;
                    }
                    markTable.Calculate(fileName, token);

                    if (markTable.Table.Min() < Constants.WEAK_BORDER)
                    {
                        cts.Cancel();
                        _isSuccess = false;
                        throw new PolynomialAgreementException($"Количество знаков при k = {dimTemp} < 20. ФДСЧ не забракован. Требуется тестирование на новой последовательности");
                    }
                }, token));
            }
            Task.WaitAll(tasks.ToArray());
            cts.Dispose();
        }

        /// <summary>
        /// Вычисление статистик
        /// </summary>
        /// <param name="fileName">Файл с гаммой</param>
        private void _calculate(string fileName)
        {
            _pValues = new List<double>();
            _statistics = new List<double>();
            for (int i = 0; i < _kMax - 1; i++)
            {
                _pValues.Add(0);
                _statistics.Add(0);
            }

            List<Task> tasks = new List<Task>();

            for (int i = 2; i <= _kMax; i++)
            {
                MarkTable markTable = new MarkTable(i);
                if (i == 3)
                {
                    markTable.ProcessChanged += _onProcessChanged!;
                    markTable.ProgressChanged += _onProgressChanged!;
                }
                tasks.Add(Task.Run(() =>
                {
                    calcFreqHistForSingleThread(markTable, fileName);
                }));

            }

            Task.WaitAll(tasks.ToArray());


        }

        private void calcFreqHistForSingleThread(MarkTable markTable, string fileName)
        {
            double dTetta = 0;
            double dSk = 0;
            double dPi = 0;

            int dimension = markTable.Dimension;

            markTable.Calculate(fileName);

            for (int j = 0; j < markTable.Table.Length; j++)
            {
                dTetta += markTable.Table[j] * OnesCalculator.Calculate(j);
            }
            dTetta /= (markTable.Dimension * markTable.NmVectors);



            for (int j = 0; j < (1 << dimension) - 1; j++)
            {
                dPi = Math.Pow(dTetta, OnesCalculator.Calculate(j)) * Math.Pow((1 - dTetta), dimension - OnesCalculator.Calculate(j));
                dSk += Math.Pow((markTable.Table[j] - dPi * markTable.NmVectors), 2) / (dPi * markTable.NmVectors);
#if (!DEBUG)
				//EventProgressPercent(this, SupportFunction.GetPercent(j + 1, (1 << dimension) - 1));
#endif
            }
            _statistics[dimension - 2] = dSk;
            _pValues[dimension - 2] = (1 - ChiSquared.CDF((1 << dimension) - 2, dSk));
        }

        private void _onProgressChanged(object sender, int e)
        {
            ProgressChanged?.Invoke(this, e);
        }

        private void _onProcessChanged(object sender, string e)
        {
            ProcessChanged?.Invoke(this, e);
        }
    }
}