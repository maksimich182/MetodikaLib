using MathNet.Numerics.Distributions;
using MihStatLibrary;
using MihStatLibrary.Calculators;
using MihStatLibrary.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MetodikaLib.Tests
{
    /// <summary>
	/// Класс проверки соответствия частот знаков в выходной последовательности ТВМ модели(Тест 3.1)
	/// </summary>
    public class AccordancePTM : ITestable
    {
        private double _statistic;
        private bool? _isSuccess;
        double _leftBorder;
        double _rightBorder;

        /// <summary>
        /// Результат теста
        /// </summary>
        public bool? IsSuccess { get => _isSuccess; }

        /// <summary>
        /// Конструктор проверки соответствия частот знаков в выходной последовательности
        /// </summary>
        public AccordancePTM()
        {
            _statistic = 0;
            _isSuccess = null;
            _leftBorder = 0;
            _rightBorder = 0;
        }

        /// <summary>
        /// Событие изменения прогресса проведения теста
        /// </summary>
        public event EventHandler<int>? ProgressChanged;

        /// <summary>
        /// Событие изменения этапа проведения теста
        /// </summary>
        public event EventHandler<string>? ProcessChanged;

        /// <summary>
        /// Функция проверки соответствия частот знаков в выходной последовательности
        /// </summary>
        /// <param name="fileName">Имя файла с данными</param>
        /// <param name="alpha">Альфа</param>
        public void Test(string fileName, double alpha)
        {
            BigInteger nmOnes = 0;

            MarkTable markTable = new MarkTable(Constants.BITS_IN_BYTE);
            markTable.ProcessChanged += _onProcessChanged!;
            markTable.ProgressChanged += _onProgressChanged!;
            markTable.Calculate(fileName);

            ProcessChanged?.Invoke(this, $"Рассчет статистики");
            for (int i = 0; i < markTable.Table.Length; i++)
            {
                nmOnes += (BigInteger)OnesCalculator.Calculate(i) * markTable.Table[i];
                ProgressChanged?.Invoke(this, Tools.GetPercent(i + 1, markTable.Table.Length));
            }
            _statistic = Tools.BigIntDevider(nmOnes, markTable.NmVectors * Tools.BITS_IN_BYTE);
            _leftBorder = ((double)1 / 2) - (Normal.InvCDF(0, 1, (1 - alpha / 2)) / (2 * Math.Sqrt(markTable.NmVectors * Tools.BITS_IN_BYTE)));
            _rightBorder = ((double)1 / 2) + (Normal.InvCDF(0, 1, (1 - alpha / 2)) / (2 * Math.Sqrt(markTable.NmVectors * Tools.BITS_IN_BYTE)));

            if (_leftBorder <= _statistic && _statistic <= _rightBorder)
            {
                _isSuccess = true;
            }
            _isSuccess = false;
        }

        /// <summary>
        /// Формирование строки результата тестирования
        /// </summary>
        /// <returns>Строка результата тестирования</returns>
        public override string ToString()
        {
            if (_isSuccess is null)
            {
                return "Тест 3.1 не проводился!\n";
            }
            string strResult = string.Empty;
            strResult = $"\nТест 3.1: Проверка соответствия частот знаков в выходной последовательности теоретико-вероятностной модели образца ФГСЧ\t\t";
            strResult = $"{strResult}Начало интервала\tКонец интервала\tT^(0)\t\nРезультат\t";
            strResult = _isSuccess == true ? $"{strResult}Тест пройден\t" : $"{strResult}Тест не пройден\t";

            strResult = $"{strResult}[{_leftBorder}\t;{_rightBorder}]\t{_statistic}\t";
            strResult = _isSuccess == true ? strResult = $"{strResult}\n\n" : strResult = $"{strResult}Fail\n\n";
            return strResult;
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
