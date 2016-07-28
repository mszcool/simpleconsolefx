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

        public static void WriteMessage(string message, bool includeTime = false, bool includeDate = false, string prefixText = "help:")
        {
            WriteOutput(message, prefixText, null, includeTime, includeDate, Console.Out, DefaultColor);
        }

        public static void WriteError(string errorMessage, string errorDetails = "", bool includeTime = false, bool includeDate = false, string prefixText = "error:")
        {
            WriteOutput(errorMessage, prefixText, errorDetails, includeTime, includeDate, Console.Error, ErrorColor);
        }

        public static void WriteWarning(string warningMessage, string warningDetails = "", bool includeTime = false, bool includeDate = false, string prefixText = "warning:")
        {
            WriteOutput(warningMessage, prefixText, warningDetails, includeTime, includeDate, Console.Error, WarningColor);
        }

        private static void WriteOutput(string message, string prefixText, string details, bool includeTime, bool includeDate, TextWriter targetStream, ConsoleColor targetColor)
        {
            var textBuilder = new StringBuilder();

            // Add the date/time fields if wished
            if (includeDate) textBuilder.Append(DateTime.Now.ToString("yyyy-MM-dd "));
            if (includeTime) textBuilder.Append(DateTime.Now.ToString("HH:mm:ss "));
            if (includeDate || includeTime) textBuilder.Append("\t");

            // Add the prefix text if passed in
            if (!string.IsNullOrEmpty(prefixText)) textBuilder.Append($"{prefixText}\t");

            // Then append the actual message
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
