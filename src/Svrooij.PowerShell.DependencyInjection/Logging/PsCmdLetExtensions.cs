using Microsoft.Extensions.Logging;
using System;
using System.Management.Automation;

namespace Svrooij.PowerShell.DependencyInjection.Logging;

internal static class PsCmdletExtensions
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

    /// <summary>
    /// Write a log message to the provider PowerShell <see cref="PSCmdlet"/>
    /// </summary>
    /// <param name="cmdlet"><see cref="PSCmdlet"/> that is used for the log message</param>
    /// <param name="logLevel"><see cref="LogLevel"/> for the message, will be put in from the message</param>
    /// <param name="category">The log category</param>
    /// <param name="eventId">The ID for this specific event</param>
    /// <param name="message">Log message</param>
    /// <param name="e">(optional) <see cref="Exception"/></param>
    public static void WriteLog(this PSCmdlet cmdlet, LogLevel logLevel, string category, int eventId, string message, Exception? e = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                cmdlet.WriteVerbose(message);
                break;

            case LogLevel.Debug:
                cmdlet.WriteDebug(message);
                break;

            case LogLevel.Information:
                cmdlet.WriteInformation(message, new string[] { });
                // The line above does not work, so we use this workaround
                Console.WriteLine($"INFO: {message}");
                break;

            case LogLevel.Warning:
                cmdlet.WriteWarning(message);
                break;

            case LogLevel.Error:
                cmdlet.WriteError(new ErrorRecord(e ?? new Exception(message), eventId.ToString(), ErrorCategory.InvalidOperation, null));
                break;
        }
    }
}