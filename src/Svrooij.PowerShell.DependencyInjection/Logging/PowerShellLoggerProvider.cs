using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Management.Automation;

namespace Svrooij.PowerShell.DependencyInjection.Logging;

#nullable enable
/// <summary>
/// <see cref="ILoggerProvider"/> that outputs to the PowerShell <see cref="PSCmdlet"/>
/// </summary>
[ProviderAlias("Powershell")]
public sealed class PowerShellLoggerProvider : ILoggerProvider
{
    private PSCmdlet? cmdlet;
    private readonly IDisposable? _onChangeToken;
    private PowerShellLoggerConfiguration _currentConfig;

    private readonly ConcurrentDictionary<string, PowerShellLogger> _loggers =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a new instance of <see cref="PowerShellLoggerProvider"/>
    /// </summary>
    /// <param name="config">Auto loaded configuration</param>
    public PowerShellLoggerProvider(IOptionsMonitor<PowerShellLoggerConfiguration> config)
    {
        _currentConfig = config.CurrentValue;
        _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
    }

    private PowerShellLoggerConfiguration GetCurrentConfig()
    {
        return _currentConfig;
    }

    internal void SetCmdlet(PSCmdlet cmdlet)
    {
        this.cmdlet = cmdlet;
    }

    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance.
    /// </summary>
    /// <param name="categoryName">Category name to use</param>
    /// <returns><see cref="ILogger"/></returns>
    public ILogger CreateLogger(string categoryName)
    {
        // What to do if cmdlet is null?
        return _loggers.AddOrUpdate(
               categoryName,
               name => new PowerShellLogger(name, GetCurrentConfig, cmdlet!),
               (name, logger) => logger
        );
    }

    /// <summary>
    /// Dispose the provider
    /// </summary>
    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
    }
}