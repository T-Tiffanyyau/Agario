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
/// The CustomFileLogProvider class implements ILoggerProvider to create and manage instances of the FileLogger class.
/// </summary>
using Microsoft.Extensions.Logging;
using System;

namespace FileLogger
{
    /// <summary>
    /// CustomFileLogProvider creates a new instance of FileLogger each time ILoggerProvider.CreateLogger is called, 
    /// and disposes of the most recently created instance of FileLogger when ILoggerProvider.Dispose is called.
    /// </summary>
    public class CustomFileLogProvider : ILoggerProvider
    {
        private readonly string logFilePath;
        private FileLogger fileLogger;

        /// <summary>
        /// Constructor for CustomFileLogProvider class.
        /// </summary>
        /// <param name="logFilePath">The path of the log file.</param>
        public CustomFileLogProvider(string logFilePath)
        {
            this.logFilePath = logFilePath;
        }

        /// <summary>
        /// Creates a new instance of the FileLogger class for the specified category name.
        /// </summary>
        /// <param name="categoryName">The category name for the logger.</param>
        /// <returns>A new instance of the FileLogger class.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            fileLogger = new FileLogger(categoryName);
            return fileLogger;
        }

        /// <summary>
        /// Disposes the FileLogger object.
        /// </summary>
        public void Dispose()
        {
            fileLogger?.Dispose();
        }
    }
}
