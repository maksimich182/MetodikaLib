using MathNet.Numerics.Distributions;
using MihStatLibrary.Data;
using MihStatLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static MetodikaLib.Constants;
using MihStatLibrary.Calculators;

namespace MetodikaLib.Tests
{
    /// <summary>
    /// Класс проверки гипотезы однородности знаков (Тест 2.2/3.3)
    /// </summary>
    public class UniformitySigns : ITestable
    {
        /// <summary>
		/// Событие изменения прогресса выполнения тестирования
		/// </summary>
        public event EventHandler<int>? ProgressChanged;

        /// <summary>
        /// Событие изменения этапа выполнения тестирования
        /// </summary>
        public event EventHandler<string>? ProcessChanged;

        private int _dimension;
        private int _K;
        private BigInteger[] _Ujs;
        private double _statistic;
        private double _PValue;
        private long _nmSamples;
        private bool? _isSuccess;
        private int _szSample;
        private GammaType _type;

        /// <summary>
        /// Конструктор проверки гипотезы однородности знаков
        /// </summary>
        /// <param name="szSample"></param>
        /// <param name="type">Тип гаммы</param>
        public UniformitySigns(int szSample, GammaType type = GammaType.InputGamma)
        {
            _szSample = szSample;
            _K = 0;
            _isSuccess = null;
            _statistic = 0;
            _nmSamples = 0;
            _dimension = 0;
            _Ujs = new BigInteger[2];
        }

        /// <summary>
        /// Инициализация экземпляра класса
        /// </summary>
        /// <param name="fileName">Файл с гаммой</param>
        /// <param name="dimension">Размерность</param>
        private void _initialiseUS(string fileName, int dimension)
        {
            _nmSamples = (long)Math.Floor((double)(new FileInfo(fileName).Length * Tools.BITS_IN_BYTE) / dimension);
            _dimension = dimension;
        }

        public void Print()
        {
            Console.WriteLine($"Тест 2.2: Однородность");
            Console.WriteLine($"Pk = {_statistic}");
        }

        //ToDelete
        public void PrintInFile(string pFileName)
        {
            Stream fs = new FileStream(pFileName, FileMode.Append, FileAccess.Write);
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                string str = $"Test 2.2: \t{_statistic}\t{_PValue}";
                bw.Write(str);
                fs.Close();
            }
        }

        /// <summary>
        /// Функция тестирования однородности знаков
        /// </summary>
        /// <param name="fileName">Имя файла с данными</param>
        /// <param name="alpha">Альфа</param>
        public void Test(string fileName, double alpha)
        {
            _getK(fileName);
            _calculate(fileName);
            if (_PValue < alpha)
            {
                _isSuccess = false;
                return;
            }
            _isSuccess = true;
            return;
        }

        /// <summary>
        /// Формирование строки результата тестирования
        /// </summary>
        /// <returns>Строка результата тестирования</returns>
        public override string ToString()
        {
            if (_isSuccess is null)
            {
                return _type == GammaType.InputGamma ? "Тест 2.2 не проводился!\n" : "Тест 3.3 не проводился!\n";
            }
            string strResult = string.Empty;
            strResult = _type == GammaType.InputGamma ? $"\nТест 2.2: Проверка гипотезы однородности знаков в исходной последовательности\t\t" : $"\nТест 3.3: Проверка гипотезы однородности знаков в выходной последовательности\t\t";
            strResult = _type == GammaType.InputGamma ? $"{strResult}K\tS^(2)_k\tP^(1)_k\nРезультат\t" : $"{strResult}K\tT^(2)_k\tR^(2)_k\nРезультат\t";
            strResult = _isSuccess == true ? $"{strResult}Тест пройден\t" : $"{strResult}Тест не пройден\t";

            strResult = $"{strResult}{_K}\t{_statistic}\t{_PValue}";
            strResult = _isSuccess == true ? $"{strResult}\n\n" : $"{strResult}\tFail\n\n";
            return strResult;
        }

        /// <summary>
        /// Вычbсление p-value
        /// </summary>
        /// <param name="fileName">Файл с гаммой</param>
        /// <param name="K">Размерность k</param>
        private void _calculate(string fileName)
        {
            BlockData primaryData;
            string strProcessStatus = $"k = {_K}. Вычисление статистики";
            _initialiseUS(fileName, _K);
            BigInteger remain = 0;
            int szRemain = 0;

            FileStream fsInputData = new FileStream(fileName, FileMode.Open);
            primaryData = new BlockData(new BlockDataFileSource(fsInputData));
            double nmBlocks = Math.Ceiling((double)fsInputData.Length / Tools.SIZE_BLOCK_BYTES);
            for (int j = 0; j < nmBlocks; j++)
            {
                ProcessChanged?.Invoke(this, $"{strProcessStatus} ({j + 1} блок из {nmBlocks})");
                primaryData.GetBlockData(Tools.SIZE_BLOCK_BYTES);
                //_calculateSk(primaryData, ref remain, ref szRemain);
                _processData(primaryData, ref remain, ref szRemain, _calculateData);
            }
            _statistic = _nmSamples * _statistic;
            _PValue = 1 - ChiSquared.CDF(_nmSamples - 1, _statistic);
            fsInputData.Close();
        }

        /// <summary>
        /// Получение k
        /// </summary>
        /// <param name="fileName">Файл с гаммой</param>
        private void _getK(string fileName)
        {
            BlockData primaryData;

            for (int dimension = Constants.MIN_K; ; dimension++)
            {
                string currentSLabel = $"Проверка для s = {dimension}";
                FileStream fsInputData = new FileStream(fileName, FileMode.Open);
                try
                {
                    _initialiseUS(fileName, dimension);
                    _resetUj();

                    BigInteger remain = 0;
                    int szRemain = 0;

                    primaryData = new BlockData(new BlockDataFileSource(fsInputData));
                    double nmBlocks = Math.Ceiling((double)fsInputData.Length / Tools.SIZE_BLOCK_BYTES);

                    for (int j = 0; j < nmBlocks; j++)
                    {
                        ProcessChanged?.Invoke(this, $"{currentSLabel} ({j + 1} блок из {nmBlocks})");
                        primaryData.GetBlockData(Tools.SIZE_BLOCK_BYTES);
                        //_calculateMinS(primaryData, ref remain, ref szRemain);
                        _processData(primaryData, ref remain, ref szRemain, _checkData);
                    }
                    fsInputData.Close();
                    _K = dimension;
                }
                catch (TestExceptions)
                {
                    fsInputData.Close();
                }
            }
        }

        /// <summary>
        /// Обнуление массива Uj количества 0 и 1 в гамме
        /// </summary>
        private void _resetUj()
        {
            for (int i = 0; i < _Ujs.Length; i++)
            {
                _Ujs[i] = 0;
            }
        }

        /// <summary>
        /// Делегат операции с порцией данных
        /// </summary>
        /// <param name="data">Порция данных</param>
        /// <param name="szData">Размер порции данных</param>
        private delegate void _operationWithData(ref BigInteger data, ref int szData);

        /// <summary>
        /// Функция обработки данных из блока
        /// </summary>
        /// <param name="blockData">Блок данных</param>
        /// <param name="remain">Данные с прошлых блоков, которые не влезли в последний вектор (ранее необсчитанные данные)</param>
        /// <param name="szRemain">Размер данных с прошлых блоков, которые не влезли в последний вектор (ранее необсчитанные данные)</param>
        /// <param name="operationWithData">Делегат операции с данными</param>
        private void _processData(BlockData blockData, ref BigInteger remain, ref int szRemain, _operationWithData operationWithData)
        {
            BigInteger dataBuffer = 0;
            int szBuffer = 0;

            int counterReadElements = 0;

            if (szRemain != 0)
            {
                dataBuffer = remain;
                szBuffer = szRemain;
            }

            while (counterReadElements < blockData.Data?.Length)
            {
                _getDataForCalculate(blockData, ref dataBuffer, ref szBuffer, ref counterReadElements);
                operationWithData(ref dataBuffer, ref szBuffer);
                ProgressChanged?.Invoke(this, Tools.GetPercent(counterReadElements, blockData.Data.Length));
            }

            remain = dataBuffer;
            szRemain = szBuffer;
        }

        /// <summary>
        /// Вычисление минимального s
        /// </summary>
        /// <param name="dataBlock">Блок данных</param>
        /// <param name="remain">Данные с прошлых блоков, которые не влезли в последний вектор (ранее необсчитанные данные)</param>
        /// <param name="szRemain">Размер данных с прошлых блоков, которые не влезли в последний вектор (ранее необсчитанные данные)</param>
        private void _calculateMinS(BlockData dataBlock, ref BigInteger remain, ref int szRemain)
        {
            BigInteger dataBuffer = 0;
            int szBuffer = 0;

            int counterReadElements = 0;

            if (szRemain != 0)
            {
                dataBuffer = remain;
                szBuffer = szRemain;
            }

            while (counterReadElements < dataBlock.Data?.Length)
            {
                _getDataForCalculate(dataBlock, ref dataBuffer, ref szBuffer, ref counterReadElements);
                _checkData(ref dataBuffer, ref szBuffer);
                ProgressChanged?.Invoke(this, Tools.GetPercent(counterReadElements, dataBlock.Data.Length));
            }

            remain = dataBuffer;
            szRemain = szBuffer;
        }

        /// <summary>
        /// Обсчет блока данных для вычисления Sk
        /// </summary>
        /// <param name="blockData">Блок данных</param>
        /// <param name="remain">Данные с прошлых блоков, которые не влезли в последний вектор (необсчитанные данные)</param>
        /// <param name="szRemain">Размер данных с прошлых блоков, которые не влезли в последний вектор (необсчитанные данные)</param>
        private void _calculateSk(BlockData blockData, ref BigInteger remain, ref int szRemain)
        {
            BigInteger dataBuffer = 0;
            int szBuffer = 0;

            int counterReadElements = 0;

            if (szRemain != 0)
            {
                dataBuffer = remain;
                szBuffer = szRemain;
            }

            while (counterReadElements < blockData.Data?.Length)
            {
                _getDataForCalculate(blockData, ref dataBuffer, ref szBuffer, ref counterReadElements);
                _calculateData(ref dataBuffer, ref szBuffer);
                ProgressChanged?.Invoke(this, Tools.GetPercent(counterReadElements, blockData.Data.Length));
            }

            remain = dataBuffer;
            szRemain = szBuffer;
        }

        /// <summary>
        /// Получение новой порции данных
        /// </summary>
        /// <param name="blockData">Блок данных</param>
        /// <param name="dataBuffer">Буффер с порцией данных</param>
        /// <param name="szBuffer">Количество бит данных в буффере</param>
        /// <param name="counterReadElements">Количество считанных из блока данных байт</param>
        private void _getDataForCalculate(BlockData blockData, ref BigInteger dataBuffer, ref int szBuffer, ref int counterReadElements)
        {
            while (szBuffer < _dimension)
            {
                dataBuffer |= (BigInteger)blockData.Data![counterReadElements++] << szBuffer;
                szBuffer += Tools.BITS_IN_BYTE;
                if (counterReadElements == blockData.Data.Length)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Проверка порции данных для вычисления k (количество и единиц, и нулей больше минимальной границы)
        /// </summary>
        /// <param name="dataBuffer">Буффер для порции данных</param>
        /// <param name="szBuffer">Количество бит данных в буффере</param>
        private void _checkData(ref BigInteger dataBuffer, ref int szBuffer)
        {
            long onesCounter = 0;
            long minNmSign = 0;

            while (szBuffer >= _dimension)
            {
                onesCounter = OnesCalculator.Calculate(dataBuffer, _dimension);
                _Ujs[0] += _dimension - onesCounter;
                _Ujs[1] += onesCounter;
                minNmSign = Math.Min(onesCounter, _dimension - onesCounter);
                if (minNmSign < _szSample)
                {
                    throw new TestExceptions($"Минимальное количество знаков < {_szSample}");
                }
                dataBuffer >>= _dimension;
                szBuffer -= _dimension;
            }
        }

        /// <summary>
        /// Обсчет порции данных для статистики S^(2)_k
        /// </summary>
        /// <param name="dataBuffer">Буффер для порции данных</param>
        /// <param name="szBuffer">Количество бит данных в буффере</param>
        private void _calculateData(ref BigInteger dataBuffer, ref int szBuffer)
        {
            long iOneCounter = 0;

            while (szBuffer >= _dimension)
            {
                iOneCounter = OnesCalculator.Calculate(dataBuffer, _dimension);
                _statistic += Tools.BigIntDevider(Math.Pow((_dimension - iOneCounter) - (Tools.BigIntDevider(_Ujs[0], _nmSamples)), 2), _Ujs[0]);
                _statistic += Tools.BigIntDevider(Math.Pow(iOneCounter - (Tools.BigIntDevider(_Ujs[1], _nmSamples)), 2), _Ujs[1]);
                dataBuffer >>= _dimension;
                szBuffer -= _dimension;
            }
        }
    }
}
