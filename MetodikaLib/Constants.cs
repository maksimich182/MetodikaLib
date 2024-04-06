using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetodikaLib
{
    public class Constants
    {
        public const int BITS_IN_BYTE = 8;
        public const long SIZE_BLOCK_BYTES = 90090000;
        public const long SIZE_BLOCK_BITS = BITS_IN_BYTE * SIZE_BLOCK_BYTES;
        public const double QUANTILE_1_A2 = 2.80703;

        /// <summary>
        /// Тест 2.1/3.2 - граничное значение размерности маркировочной таблицы
        /// </summary>
        public const int MIN_MAX_K = 24;

        /// <summary>
        /// Тест 2.1/3.2 - граничное значение содержимого маркировочной таблицы
        /// </summary>
        public const double WEAK_BORDER = 20;

        /// <summary>
        /// Тест 2.2/3.3 - k с которого начинается проверка значений k 
        /// </summary>
        public const int MIN_K = 40;

        /// <summary>
        /// Тест 2.2 - значение минимального количества единиц и нулей в каждом векторе разбиения исходной последоватиельности
        /// </summary>
        public const int SZ_SAMPLE_INPUT = 20;

        /// <summary>
        /// Тест 3.3 - значение минимального количества единиц и нулей в каждом векторе разбиения выходной последоватиельности
        /// </summary>
        public const int SZ_SAMPLE_EXIT = 100;

        //Тест 2.3
        /// <summary>
        /// Тест 2.3 - минимальное значение s
        /// </summary>
        public const int MIN_S = 6;
        /// <summary>
        /// Тест 2.3 - максимальное значение s
        /// </summary>
        public const int MAX_S = 16;
        /// <summary>
        /// Тест 2.3 - k с которого начинается проверка значений k
        /// </summary>
        public const int MIN_K_PA = 2;

        /// <summary>
        /// Тип гаммы
        /// </summary>
        public enum GammaType
        {
            InputGamma, 
            ExitGamma
        }
    }
}
