using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetodikaLib.Tests
{
    /// <summary>
    /// Исключение, возникающее при тестировании
    /// </summary>
    public class TestExceptions : Exception
    {
        public TestExceptions(string pMessage) : base(pMessage) { }
    }
}
