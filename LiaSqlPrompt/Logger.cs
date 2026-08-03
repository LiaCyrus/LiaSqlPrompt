using System;
using System.IO;

namespace LiaSqlPrompt
{
    public static class Logger
    {
        private static readonly string LogDirectory = @"C:\LiaSqlPrompt";
        private static readonly string LogFile =
            Path.Combine(LogDirectory, "LiaSqlPrompt.log");

        public static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);

                File.AppendAllText(
                    LogFile,
                    $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch
            {
                // Never let logging crash the extension
            }
        }
    }
}