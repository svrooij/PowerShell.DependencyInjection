namespace Svrooij.PowerShell.DI.Generator;

internal class SourceGenerationHelper
{
    internal const string Attributes = @"using System;
namespace Svrooij.PowerShell.DI
{
    /// <summary>
    /// Tell the source generator to generate code to bind the dependencies instead of using reflection.
    /// </summary>
    /// <remarks>Your class has to be a partial class and extend <see cref=""DependencyCmdlet{T}""/>.</remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class GenerateBindingsAttribute : Attribute
    {
    }


    /// <summary>
    /// Mark a field or property as a dependency that has to be resolved by the <see cref=""IServiceProvider""/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ServiceDependencyAttribute : Attribute
    {
        /// <summary>
        /// Should the dependency be required, if it is not found an exception will be thrown.
        /// </summary>
        public bool Required { get; set; }
    }
}";

    internal const string Classes = @"
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Svrooij.PowerShell.DI.Logging;
namespace Svrooij.PowerShell.DI
{
    internal static class ServiceProviderCache
    {
        /// <summary>
        /// Cache for service providers, keyed by the startup type.
        /// </summary>
        internal static readonly ConcurrentDictionary<Type, IServiceProvider> ProviderCache = new ConcurrentDictionary<Type, IServiceProvider>();
    }
    #nullable enable
    /// <summary>
    /// Base class for startup classes for PowerShell cmdlets.
    /// Create a class that extends this class and override <see cref=""ConfigureServices(IServiceCollection)""/>.
    /// </summary>
    /// <remarks>
    /// This class is called automatically by the <see cref=""DependencyCmdlet{T}""/> constructor.
    /// Logging is automatically added to the <see cref=""IServiceCollection""/>, please don't mess with the logging part. 
    /// </remarks>
    public abstract class PsStartup
    {
        internal void ConfigurePowerShellServices(IServiceCollection services)
        {
            services.AddPowerShellLogging(ConfigurePowerShellLogging());
        }

        /// <summary>
        /// Override this method to configure the <see cref=""IServiceCollection""/> needed by your application.
        /// </summary>
        /// <param name=""services""><see cref=""IServiceCollection""/> that you can add services to.</param>
        /// <remarks>Logging is setup for you, overriding it breaks stuff!</remarks>
        public virtual void ConfigureServices(IServiceCollection services)
        {
        }

        /// <summary>
        /// Override this method to configure the <see cref=""PowerShellLoggerConfiguration""/> needed by your application.
        /// </summary>
        /// <code>
        /// return builder =>
        /// {
        ///    builder.MinimumLevel = LogLevel.Information;
        /// };
        /// </code>
        public virtual Action<PowerShellLoggerConfiguration>? ConfigurePowerShellLogging()
        {
            // Default implementation, you can override this method to change the default behavior.
            return null;
        }
    }

    /// <summary>
    /// Base class for cmdlets that use dependency injection.
    /// <para>Use the <see cref=""GenerateBindingsAttribute""/> to tell the compiler to generate binding code instead of using reflection</para>
    /// </summary>
    /// <typeparam name=""TStartup"">Your startup class that has to extend <see cref=""PsStartup""/> and extend <see cref=""PsStartup.ConfigureServices(IServiceCollection)""/>.</typeparam>
    /// <remarks>You should override <see cref=""DependencyCmdlet{T}.ProcessRecordAsync(CancellationToken)""/>. A lot of other methods are blocked from overriding.</remarks>
    public abstract class DependencyCmdlet<TStartup> : PSCmdlet where TStartup : PsStartup, new()
    {
        private readonly IServiceProvider _serviceProvider;
        private IServiceScope? _serviceScope;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// <see cref=""DependencyCmdlet{T}""/> constructor, called by PowerShell.
        /// </summary>
        protected DependencyCmdlet()
        {
            var startupType = typeof(TStartup);
            _serviceProvider = ServiceProviderCache.ProviderCache.GetOrAdd(startupType, _ =>
            {
                var startup = new TStartup();
                var services = new ServiceCollection();
                startup.ConfigurePowerShellServices(services);
                startup.ConfigureServices(services);
                return services.BuildServiceProvider();
            });
        }

        /// <summary>
        /// Override this method to process each record.
        /// </summary>
        /// <param name=""cancellationToken"">The cancellation token will be called when the user presses CTRL+C during execution</param>
        /// <remarks>Your overridden method will be called automatically!</remarks>
        /// <exception cref=""NotImplementedException"">When not overridden</exception>
        /// <exception cref=""InvalidOperationException"">When one or more dependencies marked as required are not found</exception>
        public virtual Task ProcessRecordAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException($""You'll need to override {nameof(ProcessRecordAsync)}()"");
        }

        /// <summary>
        /// Override this property to bind dependencies manually, the service provider is provided by the base library.
        /// </summary>
        protected virtual Action<object, IServiceProvider> BindDependencies
        {
            get
            {
                return (obj, serviceProvider) => serviceProvider.BindDependencies(obj);
            }
        }

        /// <summary>
        /// You can call ProcessRecord, but you cannot override it!
        /// </summary>
        /// <remarks>Override the <see cref=""ProcessRecordAsync""/> method, which is called automatically.</remarks>
        protected sealed override void ProcessRecord()
        {
            ThreadAffinitiveSynchronizationContext.RunSynchronized(() => ProcessRecordAsync(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// You can call BeginProcessing, but you cannot override it!
        /// </summary>
        /// <remarks>This is called by PowerShell automatically. And is used to bind dependencies.</remarks>
        protected sealed override void BeginProcessing()
        {
            _serviceScope = _serviceProvider.CreateScope();
            _serviceScope.ServiceProvider.GetRequiredService<PowerShellLoggerContainer>().Cmdlet = this;
            BindDependencies(this, _serviceScope.ServiceProvider);
            base.BeginProcessing();
        }

        /// <summary>
        /// You can call StopProcessing, but you cannot override it!
        /// </summary>
        /// <remarks>This is called by PowerShell if the user cancels the request. If is used to trigger the cancellationToken on <see cref=""ProcessRecordAsync(CancellationToken)""/>.</remarks>
        protected sealed override void StopProcessing()
        {
            _cancellationTokenSource.Cancel();
            base.StopProcessing();
        }

        /// <summary>
        /// You can call EndProcessing, but you cannot override it!
        /// </summary>
        /// <remarks>This is called by PowerShell automatically.</remarks>
        protected sealed override void EndProcessing()
        {
            _serviceScope?.Dispose();
            base.EndProcessing();
        }
    }

    internal static class ServiceProviderExtensions
    {
        /// <summary>
        /// Use reflection to find all properties and fields with the <see cref=""ServiceDependencyAttribute""/> and set the value of the property or field using the service provider.
        /// </summary>
        /// <param name=""serviceProvider""><see cref=""IServiceProvider""/> to use to resolve dependencies</param>
        /// <param name=""obj"">The object where the dependencies have to be set</param>
        /// <exception cref=""ArgumentNullException"">if required arguments are not set</exception>
        /// <exception cref=""InvalidOperationException"">if a required dependency is not found</exception>""
        internal static void BindDependencies(this IServiceProvider serviceProvider, object obj)
        {
            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            serviceProvider.BindPropertyDependencies(obj);
            serviceProvider.BindFieldDependencies(obj);
        }

        private static void BindPropertyDependencies(this IServiceProvider serviceProvider, object obj)
        {
            var type = obj.GetType();
            var attrType = typeof(ServiceDependencyAttribute);
            var properties = type.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Where(p => Attribute.IsDefined(p, attrType));
            foreach (var property in properties)
            {
                var serviceDependencyAttribute = (ServiceDependencyAttribute)property.GetCustomAttributes(attrType, true)[0];
                var service = serviceProvider.GetService(property.PropertyType);
                if (service != null)
                {
                    property.SetValue(obj, service);
                }
                else if (serviceDependencyAttribute.Required)
                {
                    throw new InvalidOperationException($""Could not find service for {property.PropertyType.Name}."");
                }

            }
        }

        private static void BindFieldDependencies(this IServiceProvider serviceProvider, object obj)
        {
            var type = obj.GetType();
            var attrType = typeof(ServiceDependencyAttribute);
            var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Where(p => Attribute.IsDefined(p, attrType));
            foreach (var field in fields)
            {
                var serviceDependencyAttribute = (ServiceDependencyAttribute)field.GetCustomAttributes(attrType, true)[0];
                var service = serviceProvider.GetService(field.FieldType);
                if (service != null)
                {
                    field.SetValue(obj, service);
                }
                else if (serviceDependencyAttribute.Required)
                {
                    throw new InvalidOperationException($""Could not find service for {field.FieldType.Name}."");
                }
            }
        }
    }
}";

}
