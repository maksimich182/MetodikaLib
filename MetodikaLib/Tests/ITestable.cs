using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetodikaLib.Tests
{
    /// <summary>
    /// Интерфейс теста
    /// </summary>
    public interface ITestable
    {
        /// <summary>
		/// Событие изменения прогресса выполнения тестирования
		/// </summary>
		event EventHandler<int>? ProgressChanged;

        /// <summary>
        /// Событие изменения этапа выполнения тестирования
        /// </summary>
        event EventHandler<string>? ProcessChanged;

        /// <summary>
        /// Результат теста
        /// </summary>
        bool? IsSuccess { get;}

        /// <summary>
        /// Проведение тестирования
        /// </summary>
        /// <param name="fileName">Файл с данными</param>
        /// <param name="alpha">Альфа</param>
        void Test(string fileName, double alpha);
    }
}
