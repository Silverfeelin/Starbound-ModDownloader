using System;
using System.Collections.Generic;
using System.Text;

namespace StarboundModDownloader
{
    public static class Logger
    {
        public static string Format { get; set; } = "{TYPE}: {MESSAGE}";

        public static ConsoleColor InfoColor { get; set; } = ConsoleColor.White;
        public static ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;
        public static ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;

        public static void LogInfo(string formatMessage, params object[] args)
        {
            formatMessage = Format.Replace("{TYPE}", "Info").Replace("{MESSAGE}", formatMessage);
            WriteColored(InfoColor, formatMessage, args);
        }

        public static void LogWarning(string formatMessage, params object[] args)
        {
            formatMessage = Format.Replace("{TYPE}", "Warning").Replace("{MESSAGE}", formatMessage);
            WriteColored(WarningColor, formatMessage, args);
        }

        public static void LogError(string formatMessage, params object[] args)
        {
            formatMessage = Format.Replace("{TYPE}", "Error").Replace("{MESSAGE}", formatMessage);
            WriteColored(ErrorColor, formatMessage, args);
        }

        private static void WriteColored(ConsoleColor color, string formatMessage, params object[] args)
        {
            ConsoleColor c = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(formatMessage, args);
            Console.ForegroundColor = c;
        }
    }
}
