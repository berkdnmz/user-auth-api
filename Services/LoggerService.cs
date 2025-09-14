using Serilog;
using System;

namespace UserAuthApi.Services
{
    public class LoggerService : ILoggerService
    {
        public void LogInformation(string message, params object[] args)
        {
            Log.Information(message, args);
            Console.WriteLine($"INFO: {string.Format(message, args)}");
        }

        public void LogWarning(string message, params object[] args)
        {
            Log.Warning(message, args);
            Console.WriteLine($"WARN: {string.Format(message, args)}");
        }

        public void LogError(Exception ex, string message, params object[] args)
        {
            Log.Error(ex, message, args);
            Console.WriteLine($"ERROR: {string.Format(message, args)} - {ex.Message}");
        }

        public void LogError(string message, params object[] args)
        {
            Log.Error(message, args);
            Console.WriteLine($"ERROR: {string.Format(message, args)}");
        }

        public void LogDebug(string message, params object[] args)
        {
            Log.Debug(message, args);
            Console.WriteLine($"DEBUG: {string.Format(message, args)}");
        }
    }
}