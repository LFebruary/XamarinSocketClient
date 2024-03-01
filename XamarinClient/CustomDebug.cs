using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace XamarinClient
{
    /// <summary>
    /// Provides additional methods to extend the debugging capabilities of <see cref="Debug"/>.
    /// </summary>
    /// <remarks>
    /// These methods are provided in a separate class since as of writing C# does not support static extensions.
    /// </remarks>
    public static class CustomDebug
    {
        #region Package File Path Separator

        private static string PackageSeparator { get; set; } = @"\XamarinClient\";

        #endregion

        #region WriteLine Extensions

        /// <summary>
        /// Writes a blank line to the trace listeners in the <see cref="System.Diagnostics.Debug.Listeners"/> collection.
        /// </summary>
        [Conditional("DEBUG")]
        public static void WriteLine()
        {
            Debug.WriteLine(string.Empty);
        }

        /// <summary>
        /// Writes a message preceded by callsite information to the trace listeners in the <see cref="System.Diagnostics.Debug.Listeners"/> collection.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="addBlankLine">Whether to write a blank line after the written message.</param>
        /// <param name="filePath">The file from which the method is called.</param>
        /// <param name="caller"> The method in which the method is called.</param>
        /// <param name="line">The line at which the method is called.</param>
        [Conditional("DEBUG")]
        public static void WriteLine(string message, bool addBlankLine = false, [CallerFilePath] string filePath = "", [CallerMemberName] string caller = "", [CallerLineNumber] int line = -1)
        {
            string fileString = filePath.Split(new string[] { PackageSeparator }, StringSplitOptions.None).Last();

            Debug.WriteLine($"{fileString}: {caller}: line {line}: {message}");

            if (addBlankLine)
            {
                WriteLine();
            }
        }

        /// <summary>
        /// Writes the value of the object's <see cref="object.ToString()"/> method preceded by callsite information to the trace listeners in the <see cref="System.Diagnostics.Debug.Listeners"/> collection.
        /// </summary>
        /// <param name="value">The object to write the string representation of.</param>
        /// <param name="addBlankLine">Whether to write a blank line after the written message.</param>
        /// <param name="filePath">The file from which the method is called.</param>
        /// <param name="caller"> The method in which the method is called.</param>
        /// <param name="line">The line at which the method is called.</param>
        [Conditional("DEBUG")]
        public static void WriteLine(object value, bool addBlankLine = false, [CallerFilePath] string filePath = "", [CallerMemberName] string caller = "", [CallerLineNumber] int line = -1)
        {
            WriteLine(value?.ToString() ?? "null", addBlankLine, filePath, caller, line);

            if (addBlankLine)
            {
                WriteLine();
            }
        }
        #endregion
    }
}
