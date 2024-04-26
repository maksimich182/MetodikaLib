using MathNet.Numerics.Distributions;
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
    /// Класс проверки согласия распределения числа k-грамм с полиномиальным законом (Тест 2.3/3.4)
    /// </summary>
    public class PolynomialAgreement : ITestable
    {
        private List<double> _statistics;
        private List<double> _pValues;
        private MarkTable _markTableParent;
        private bool? _isSuccess;
        private int _kMax;
        private List<bool> _results;
        private bool _isNewSeuence;
        private string _newSequenceMessage;
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
        /// Конструктор проверки согласия распределения с полиномиальным законом
        /// </summary>
        /// <param name="type">Тип гаммы</param>
        public PolynomialAgreement(GammaType type = GammaType.InputGamma)
        {
            _isSuccess = null;
            _kMax = 0;
            _results = new List<bool>();
            _pValues = new List<double>();
            _statistics = new List<double>();
            _markTableParent = new MarkTable(Constants.MAX_S, 1);
            _markTableParent.ProcessChanged += _onProcessChanged!;
            _markTableParent.ProgressChanged += _onProgressChanged!;
            _isNewSeuence = false;
            _newSequenceMessage = string.Empty;
            _type = type;
        }

        /// <summary>
        /// Функция проверки согласия распределения с полиномиальным законом
        /// </summary>
        /// <param name="fileName">Имя файла с данными</param>
        /// <param name="alpha">Альфа</param>
        public void Test(string fileName, double alpha)
        {
            if(_type == GammaType.InputGamma)
            {
                _checkGamma(fileName);
                _getKMax(fileName);
            }
            else
            {
                _kMax = Constants.MAX_K;
            }
            
            _calculate(fileName);
            _checkResult(alpha);
        }

        /// <summary>
        /// Формирование строки результата тестирования
        /// </summary>
        /// <returns>Строка результата тестирования</returns>
        public override string ToString()
        {
            if (_isSuccess is null)
            {
                return _type == GammaType.InputGamma ? "Тест 2.3 не проводился!\n" : "Тест 3.4 не проводился!\n";
            }
            string strResult = string.Empty;
            strResult = _type == GammaType.InputGamma ? $"\nТест 2.3: Проверка согласия распределения числа k-грамм в исходной " +
                $"последовательности с полиномиальным законом\t" :
                $"\nТест 3.4: Проверка согласия распределения числа k-грамм в выходной последовательности с полиномиальным законом\t";
            if (_isNewSeuence)
            {
                strResult += $"\n{_newSequenceMessage}\n\n";
                return strResult;
            }
            strResult = _type == GammaType.InputGamma ? 
                _isSuccess == true ? $"{strResult}Kmax = {_kMax}\t" : $"{strResult}Kmax = Не определено\t" :
                $"{strResult}Kmax = 16\t";
            strResult = _type == GammaType.InputGamma ? $"{strResult}K\tS^(3)_k\tP^(1)_k\nРезультат\t" :
                $"{strResult}K\tT^(3)_k\tR^(3)_k\nРезультат\t";
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

        /// <summary>
        /// Проверка полученных результатов на предмет справедливости гипотезы
        /// </summary>
        /// <param name="alpha">Альфа</param>
        private void _checkResult(double alpha)
        {
            _isSuccess = true;
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
                        _isNewSeuence = true;
                        _newSequenceMessage = $"Количество знаков при k = {dimTemp} < 20. ФДСЧ не забракован. Требуется тестирование на новой последовательности";
                        throw new PolynomialAgreementException(_newSequenceMessage);
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
                    _calculateSP(markTable, fileName);
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Рассчет значения статистики S и p-value для маркировочной таблицы с определенным значением размерности
        /// </summary>
        /// <param name="markTable">Маркировочная таблица</param>
        /// <param name="fileName">Файл с гаммой</param>
        private void _calculateSP(MarkTable markTable, string fileName)
        {
            double tetta = 0;
            double Sk = 0;
            double Pi = 0;

            int dimension = markTable.Dimension;

            markTable.Calculate(fileName);

            if(_type == GammaType.InputGamma)
            {
                for (int j = 0; j < markTable.Table.Length; j++)
                {
                    tetta += markTable.Table[j] * OnesCalculator.Calculate(j);
                }
                tetta /= (markTable.Dimension * markTable.NmVectors);
            }

            for (int j = 0; j < (1 << dimension) - 1; j++)
            {
                Pi = _type == GammaType.InputGamma ?
                    Math.Pow(tetta, OnesCalculator.Calculate(j)) * Math.Pow((1 - tetta), dimension - OnesCalculator.Calculate(j)) :
                    Math.Pow(2, -j);
                Sk += Math.Pow((markTable.Table[j] - Pi * markTable.NmVectors), 2) / (Pi * markTable.NmVectors);
                ProgressChanged?.Invoke(this, Tools.GetPercent(j + 1, (1 << dimension) - 1));
            }
            _statistics[dimension - 2] = Sk;
            _pValues[dimension - 2] = (1 - ChiSquared.CDF((1 << dimension) - 2, Sk));
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
    }
}
