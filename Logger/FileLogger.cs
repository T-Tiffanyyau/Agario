/// <summary>
/// Author:    Man Wai Lam & Tiffany Yau
/// Date:      14 Apr 2023
/// Course:    CS 3500, University of Utah, School of Computing
/// Copyright: CS 3500 and Man Wai Lam & Tiffany Yau - This work may not 
///            be copied for use in Academic Coursework.
///
/// We, Man Wai Lam & Tiffany Yau, certify that we wrote this code from scratch and
/// did not copy it in part or whole from another source.  All 
/// references used in the completion of the assignments are cited 
/// in my README file.
///
/// File Contents:
/// This FileLogger class provides file logging functionality by writing log messages to a log file located in the user's application data folder.  
/// </summary>
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace FileLogger
{
    /// <summary>
    /// A custom logger implementation that logs messages to a file on disk.
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string categoryName;
        private readonly string Filename;
        private StreamWriter? streamWriter;
        private readonly object Writelock = new object();

        /// <summary>
        /// The constructor of the FileLogger class
        /// </summary>
        /// <param name="categoryName"></param>
        public FileLogger(string categoryName)
        {
            this.categoryName = categoryName;
            Filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"CS3500-{categoryName}.log");
            streamWriter = new StreamWriter(Filename, true); // true for appending to the existing file
        }

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This function writes the log file.
        /// </summary>
        /// <typeparam name="TState"> the state </typeparam>
        /// <param name="logLevel"> the log level </param>
        /// <param name="eventId"> the event ID </param>
        /// <param name="state"> the state </param>
        /// <param name="exception"> the exception </param>
        /// <param name="formatter"> the formatter </param>
        /// <exception cref="ArgumentNullException"> Throw the exception if no formatter is given </exception>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            string message = formatter(state, exception);
            if (!string.IsNullOrEmpty(message))
            {
                string logLevelShort = logLevel.ToString().Substring(0, 5).PadRight(5);
                int threadId = Thread.CurrentThread.ManagedThreadId;

                string logMessage = $"{DateTime.Now:yyyy-MM-dd h:mm:ss tt} ({threadId}) - {logLevelShort} - {message}{Environment.NewLine}";

                lock (Writelock)
                {
                    streamWriter.Write(logMessage);
                    streamWriter.Flush();
                }
            }
        }

        /// <summary>
        /// Performs the necessary cleanup operations to release unmanaged resources used by the object.
        /// </summary>
        public void Dispose()
        {
            if (streamWriter != null)
            {
                streamWriter.Dispose();
                streamWriter = null;
            }
        }
    }
}
