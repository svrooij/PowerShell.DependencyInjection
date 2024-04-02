using System.Collections.Generic;
using System.Linq;
using Mel = Microsoft.Extensions.Logging;

namespace Svrooij.PowerShell.DependencyInjection.Logging;

/// <summary>
/// Configuration for the <see cref="PowerShellLoggerProvider"/>
/// </summary>
public sealed class PowerShellLoggerConfiguration
{
    private const string DefaultLogLevelKey = "Default";

    /// <summary>
    /// Minimum level of log messages to output
    /// </summary>
    public Mel.LogLevel DefaultLevel
    {
        get => LogLevel.ContainsKey(DefaultLogLevelKey) ? LogLevel[DefaultLogLevelKey] : Mel.LogLevel.Information;
        set => LogLevel[DefaultLogLevelKey] = value;
    }

    /// <summary>
    /// Override the minimum level for specific categories (Type Names)
    /// </summary>
    public Dictionary<string, Mel.LogLevel> LogLevel { get; set; } = new();

    /// <summary>
    /// Specify if the log message should be prefixed with the category name
    /// </summary>
    public bool IncludeCategory { get; set; } = false;

    /// <summary>
    /// Strip the namespace from the category name
    /// </summary>
    public bool StripNamespace { get; set; } = false;

    internal Mel.LogLevel GetLogLevel(string name)
    {
        if (LogLevel.TryGetValue(name, out var level))
        {
            return level;
        }

        var key = LogLevel.Keys.Where(name.StartsWith).OrderByDescending(k => k.Length).FirstOrDefault();
        // Optimize the lookup by adding the found key to the dictionary
        if (key != null)
        {
            LogLevel[name] = LogLevel[key];
            return LogLevel[key];
        }

        return DefaultLevel;
    }
}