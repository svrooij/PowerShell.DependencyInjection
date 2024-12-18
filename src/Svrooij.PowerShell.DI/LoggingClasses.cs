namespace Svrooij.PowerShell.DI.Generator;

internal class LoggingClasses
{
    internal const string Classes = @"
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mel = Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
namespace Svrooij.PowerShell.DI.Logging
{
    /// <summary>
    /// <see cref=""Mel.ILogger""/> that outputs to the PowerShell <see cref=""PSCmdlet""/>
    /// </summary>
    public class PowerShellLogger : Mel.ILogger
    {
        /// <summary>
        /// Constructor for <see cref=""PowerShellLogger""/>
        /// </summary>
        /// <remarks>Called automatically by the <see cref=""PowerShellLoggerProvider""/></remarks>
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
        public void Log<TState>(Mel.LogLevel logLevel, Mel.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                _cmdlet?.WriteLog(logLevel, eventId.Id, FormatMessage(state, exception, formatter), exception);
            }

        }

        /// <inheritdoc/>
        public bool IsEnabled(Mel.LogLevel logLevel)
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
                    ? $""[{_name.Substring(_name.LastIndexOf('.') + 1)}] {formatter(state, exception)}""
                    : $""[{_name}] {formatter(state, exception)}"";
            }
            return formatter(state, exception);
        }
    }

    /// <summary>
    /// Configuration for the <see cref=""PowerShellLoggerProvider""/>
    /// </summary>
    public sealed class PowerShellLoggerConfiguration
    {
        private const string DefaultLogLevelKey = ""Default"";

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
        public Dictionary<string, Mel.LogLevel> LogLevel { get; set; } = new Dictionary<string, Mel.LogLevel>();

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

    /// <summary>
    /// <see cref=""Mel.ILoggerProvider""/> that outputs to the PowerShell <see cref=""PSCmdlet""/>
    /// </summary>
    [Mel.ProviderAlias(""Powershell"")]
    public sealed class PowerShellLoggerProvider : Mel.ILoggerProvider
    {
        private PSCmdlet? cmdlet;
        private readonly IDisposable? _onChangeToken;
        private PowerShellLoggerConfiguration _currentConfig;

        private readonly ConcurrentDictionary<string, PowerShellLogger> _loggers =
            new ConcurrentDictionary<string, PowerShellLogger>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a new instance of <see cref=""PowerShellLoggerProvider""/>
        /// </summary>
        /// <param name=""config"">Auto loaded configuration</param>
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
        /// Creates a new <see cref=""Mel.ILogger""/> instance.
        /// </summary>
        /// <param name=""categoryName"">Category name to use</param>
        /// <returns><see cref=""Mel.ILogger""/></returns>
        public Mel.ILogger CreateLogger(string categoryName)
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
    internal static class PsCmdletExtensions
    {
    #pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        /// <summary>
        /// Write a log message to the provider PowerShell <see cref=""PSCmdlet""/>
        /// </summary>
        /// <param name=""cmdlet""><see cref=""PSCmdlet""/> that is used for the log message</param>
        /// <param name=""logLevel""><see cref=""Mel.LogLevel""/> for the message, will be put in from the message</param>
        /// <param name=""eventId"">The ID for this specific event</param>
        /// <param name=""message"">Log message</param>
        /// <param name=""e"">(optional) <see cref=""Exception""/></param>
        public static void WriteLog(this PSCmdlet cmdlet, Mel.LogLevel logLevel, int eventId, string message, Exception? e = null)
    #pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            switch (logLevel)
            {
                case Mel.LogLevel.Trace:
                    cmdlet.WriteVerbose(message);
                    break;

                case Mel.LogLevel.Debug:
                    cmdlet.WriteDebug(message);
                    break;

                case Mel.LogLevel.Information:
                    cmdlet.WriteInformation(message, new string[] { });
                    // The line above does not work, so we use this workaround
                    Console.WriteLine($""INFO: {message}"");
                    break;

                case Mel.LogLevel.Warning:
                    cmdlet.WriteWarning(message);
                    break;

                case Mel.LogLevel.Error:
                    cmdlet.WriteError(new ErrorRecord(e ?? new Exception(message), eventId.ToString(), ErrorCategory.InvalidOperation, null));
                    break;
            }
        }
    }

    internal static class PowershellLoggingBuilderExtensions
    {
        /// <summary>
        /// Adds a PowerShell logger named 'PowerShell' to the logger factory.
        /// </summary>
        /// <param name=""builder""><see cref=""Mel.ILoggingBuilder""/> that you get when you call serviceCollection.AddLogging(builder =>)</param>
        /// <param name=""cmdlet"">The <see cref=""PSCmdlet""/> that is used to output the log info</param>
        /// <returns><see cref=""Mel.ILoggingBuilder""/> to support chaining</returns>
        public static Mel.ILoggingBuilder AddPowerShellLogging(this Mel.ILoggingBuilder builder, PSCmdlet cmdlet)
        {
            builder.AddConfiguration();

            // Register provider (to use DI for the properties)
            builder.Services.AddSingleton<PowerShellLoggerProvider>();

            // Register the previous registered provider ILoggerProvider, so the SetCmdlet() method can be called.
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<Mel.ILoggerProvider, PowerShellLoggerProvider>(fac =>
            {
                var p = fac.GetRequiredService<PowerShellLoggerProvider>();
                p.SetCmdlet(cmdlet);
                return p;
            }));

            // Register the logger options
            LoggerProviderOptions.RegisterProviderOptions
                <PowerShellLoggerConfiguration, PowerShellLoggerProvider>(builder.Services);
            return builder;
        }
    }

    internal static class ServiceProviderLoggerExtensions
    {
        /// <summary>
        /// Adds a PowerShell logger named 'PowerShell' to the service collection.
        /// </summary>
        /// <param name=""services""><see cref=""IServiceCollection""/> you want to add logging to</param>
        /// <param name=""cmdlet""><see cref=""PSCmdlet""/> that is used to output the log info</param>
        /// <param name=""configure"">(optional) action to configure the <see cref=""PowerShellLoggerConfiguration""/></param>
        /// <returns><see cref=""IServiceCollection""/> to support chaining</returns>
        /// <exception cref=""ArgumentNullException"">When one of the required arguments are not set</exception>
        internal static IServiceCollection AddPowerShellLogging(this IServiceCollection services, PSCmdlet cmdlet, Action<PowerShellLoggerConfiguration>? configure = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (cmdlet is null)
            {
                throw new ArgumentNullException(nameof(cmdlet));
            }

            services.AddLogging(builder =>
            {
                builder.AddPowerShellLogging(cmdlet);
                if (configure != null)
                {
                    builder.Services.Configure(configure);
                }
            });

            return services;
        }
    }
}";
}