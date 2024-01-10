using Microsoft.Extensions.Logging;

namespace Svrooij.PowerShell.DependencyInjection.Logging;

/// <summary>
/// Configuration for the <see cref="PowerShellLoggerProvider"/>
/// </summary>
public sealed class PowerShellLoggerConfiguration
{
    /// <summary>
    /// Minimum level of log messages to output
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}