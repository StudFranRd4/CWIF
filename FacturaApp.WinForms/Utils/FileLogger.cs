using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace FacturaApp.WinForms
{
    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private static object _lock = new object();
        public FileLogger(string path) => _filePath = path;
        public IDisposable BeginScope<TState>(TState state) => null!;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logLevel}: {formatter(state, exception)}{Environment.NewLine}";
            lock(_lock)
            {
                File.AppendAllText(_filePath, msg);
            }
        }
    }

    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _path;
        public FileLoggerProvider(string path) { _path = path; }
        public ILogger CreateLogger(string categoryName) => new FileLogger(_path);
        public void Dispose() { }
    }
}