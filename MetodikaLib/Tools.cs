using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetodikaLib
{
    public class Tools
    {
        static public int BITS_IN_BYTE = 8;
        static public long SIZE_BLOCK_BYTES = 90090000;
        static public long SIZE_BLOCK_BITS = BITS_IN_BYTE * SIZE_BLOCK_BYTES;
        static public double QUANTILE_1_A2 = 2.80703;
        static public int MIN_MAX_K = 24;
        static public double WEAK_BORDER = 20;

        //Тест 2.2
        static public int MIN_K = 40;
        static public int SZ_SAMPLE_INPUT = 20;

        //Тест 3.3
        static public int SZ_SAMPLE_EXIT = 100;

        //Тест 2.3
        static public int MIN_S = 6;
        static public int MAX_S = 16;
        static public int MIN_K_PA = 2;

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
