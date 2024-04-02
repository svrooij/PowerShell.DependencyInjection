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
        this._name = name;
        this._getConfig = getConfig;
        this._cmdlet = cmdlet;
    }

    private readonly string _name;
    private readonly Func<PowerShellLoggerConfiguration> _getConfig;
    private readonly PSCmdlet _cmdlet;

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            _cmdlet?.WriteLog(logLevel, eventId.Id, FormatMessage(state, exception, formatter), exception);
        }

    }

    /// <inheritdoc/>
    //public bool IsEnabled(LogLevel logLevel) => logLevel >= getConfig().GetLogLevel(name);
    public bool IsEnabled(LogLevel logLevel)
    {
        var minLevel = _getConfig().GetLogLevel(_name);
        return logLevel >= minLevel;
    }


    /// <inheritdoc/>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

    private string FormatMessage<TState>(TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var typeName = _cmdlet.GetType().FullName;
        var config = _getConfig();
        if (config.IncludeCategory && typeName != _name)
        {
            return config.StripNamespace
                ? $"[{_name.Substring(_name.LastIndexOf('.') + 1)}] {formatter(state, exception)}"
                : $"[{_name}] {formatter(state, exception)}";
        }
        return formatter(state, exception);
    }
}