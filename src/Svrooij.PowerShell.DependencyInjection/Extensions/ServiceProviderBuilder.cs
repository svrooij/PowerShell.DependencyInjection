using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Svrooij.PowerShell.DependencyInjection;

internal class ServiceProviderBuilder
{
    private static readonly Lazy<ServiceProviderBuilder> lazyInstance = new(() => new ServiceProviderBuilder());
    public static ServiceProviderBuilder Instance => lazyInstance.Value;

    private readonly ConcurrentDictionary<Type, IServiceProvider> providers;
    private ServiceProviderBuilder()
    {
        providers = new();
    }

    public IServiceProvider GetServiceProvider<T>() where T : PsStartup, new()
    {
        if (providers.TryGetValue(typeof(T), out var provider))
        {
            return provider;
        }
        else
        {
            var startup = new T();
            var services = new ServiceCollection();
            startup.ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            providers.TryAdd(typeof(T), serviceProvider);
            return serviceProvider;
        }
    }


}
