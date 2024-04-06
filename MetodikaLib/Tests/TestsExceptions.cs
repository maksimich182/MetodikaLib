using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetodikaLib.Tests
{
    /// <summary>
    /// Исключение, возникающее при тестировании однородности знаков
    /// </summary>
    public class UniformitySignsExceptions : Exception
    {
        public UniformitySignsExceptions(string pMessage) : base(pMessage) { }
    }

    /// <summary>
    /// Исключение теста согласия распределения числа k-грамм с полиномиальным законом
    /// </summary>
    public class PolynomialAgreementException : Exception 
    {
        public PolynomialAgreementException(string pMessage) : base(pMessage) { }
    }
}
