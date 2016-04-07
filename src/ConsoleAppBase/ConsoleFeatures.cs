using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppBase
{
    public static class ConsoleFeatures
    {
        static ConsoleFeatures()
        {
            DefaultColor = Console.ForegroundColor;
            ErrorColor = ConsoleColor.Red;
            WarningColor = ConsoleColor.Yellow;
        }

        public static ConsoleColor DefaultColor { get; set; }
        public static ConsoleColor ErrorColor { get; set; }
        public static ConsoleColor WarningColor { get; set; }

        public static void WriteMessage(string message, bool includeTime = false, bool includeDate = false)
        {
            WriteOutput(message, null, includeTime, includeDate, Console.Out, DefaultColor);
        }

        public static void WriteError(string errorMessage, string errorDetails = "", bool includeTime = false, bool includeDate = false)
        {
            WriteOutput(errorMessage, errorDetails, includeTime, includeDate, Console.Error, ErrorColor);
        }

        public static void WriteWarning(string warningMessage, string warningDetails = "", bool includeTime = false, bool includeDate = false)
        {
            WriteOutput(warningMessage, warningDetails, includeTime, includeDate, Console.Error, WarningColor);
        }

        private static void WriteOutput(string message, string details, bool includeTime, bool includeDate, TextWriter targetStream, ConsoleColor targetColor)
        {
            var textBuilder = new StringBuilder();
            if (includeDate) textBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd "));
            if (includeTime) textBuilder.Append(DateTime.Now.ToString("HH:mm:ss "));
            textBuilder.Append(message);
            if (!string.IsNullOrEmpty(details))
            {
                var lines = details.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var l in lines)
                {
                    textBuilder.AppendFormat("\t{0}", l);
                }
            }

            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = targetColor;
            targetStream.WriteLine(textBuilder.ToString());
            Console.ForegroundColor = currentColor;
        }

    }
}
