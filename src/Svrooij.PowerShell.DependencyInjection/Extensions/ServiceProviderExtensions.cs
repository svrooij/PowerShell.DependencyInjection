using System;
using System.Linq;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Svrooij.PowerShell.DependencyInjection.Tests")]
namespace Svrooij.PowerShell.DependencyInjection.Extensions;

internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Use reflection to find all properties and fields with the <see cref="ServiceDependencyAttribute"/> and set the value of the property or field using the service provider.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to use to resolve dependencies</param>
    /// <param name="obj">The object where the dependencies have to be set</param>
    /// <exception cref="ArgumentNullException">if required arguments are not set</exception>
    /// <exception cref="InvalidOperationException">if a required dependency is not found</exception>"
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
            if (service is not null)
            {
                property.SetValue(obj, service);
            } else if (serviceDependencyAttribute.Required) 
            {
                throw new InvalidOperationException($"Could not find service for {property.PropertyType.Name}.");
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
            if (service is not null)
            {
                field.SetValue(obj, service);
            } else if (serviceDependencyAttribute.Required) 
            {
                throw new InvalidOperationException($"Could not find service for {field.FieldType.Name}.");
            }
        }
    }
}