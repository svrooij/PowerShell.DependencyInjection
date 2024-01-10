using Microsoft.Extensions.Logging;
using System;
using System.Management.Automation;

namespace Svrooij.PowerShell.DependencyInjection.Logging;

/// <summary>
/// <see cref="ILogger"/> that outputs to the PowerShell <see cref="PSCmdlet"/>
/// </summary>
public class PowerShellLogger : ILogger
{
    /// <summary>
    /// Constructor for <see cref="PowerShellLogger"/>
    /// </summary>
    /// <remarks>Called automatically by the <see cref="PowerShellLoggerProvider"/></remarks>
    public PowerShellLogger(string name, Func<PowerShellLoggerConfiguration> getConfig, PSCmdlet cmdlet)
    {
        this.name = name;
        this.getConfig = getConfig;
        this.cmdlet = cmdlet;
    }

    private readonly string name;
    private readonly Func<PowerShellLoggerConfiguration> getConfig;
    private readonly PSCmdlet cmdlet;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        cmdlet?.WriteLog(logLevel, name, eventId.Id, formatter(state, exception), exception);
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => logLevel >= getConfig().MinimumLevel;

    /// <inheritdoc/>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;
}