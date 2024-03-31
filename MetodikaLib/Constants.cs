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

        //Тест 2.2
        public const int MIN_K = 40;
        public const int SZ_SAMPLE_INPUT = 20;

        //Тест 3.3
        public const int SZ_SAMPLE_EXIT = 100;

        //Тест 2.3
        public const int MIN_S = 6;
        public const int MAX_S = 16;
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
