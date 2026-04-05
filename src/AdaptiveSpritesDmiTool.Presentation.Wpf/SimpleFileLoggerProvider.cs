using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace AdaptiveSpritesDmiTool.Presentation.Wpf;

internal sealed class SimpleFileLoggerProvider(string filePath) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, SimpleFileLogger> _loggers = new(StringComparer.Ordinal);

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, static (name, path) => new SimpleFileLogger(name, path), filePath);

    public void Dispose()
    {
    }

    private sealed class SimpleFileLogger(string categoryName, string filePath) : ILogger
    {
        private static readonly object SyncRoot = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var builder = new StringBuilder()
                .Append(DateTimeOffset.UtcNow.ToString("O"))
                .Append(" [")
                .Append(logLevel)
                .Append("] ")
                .Append(categoryName)
                .Append(": ")
                .Append(formatter(state, exception));

            if (exception is not null)
            {
                builder.AppendLine().Append(exception);
            }

            lock (SyncRoot)
            {
                File.AppendAllText(filePath, builder.AppendLine().ToString());
            }
        }
    }
}
