using System;

namespace Svrooij.PowerShell.DependencyInjection.Extensions;

internal static class ServiceProviderExtensions
{
    /// <summary>
    /// Use reflection to find all properties and fields with the <see cref="ServiceDependencyAttribute"/> and set the value of the property or field using the service provider.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> to use to resolve dependencies</param>
    /// <param name="obj">The object where the dependencies have to be set</param>
    /// <exception cref="ArgumentNullException">if required arguments are not set</exception>
    /// <exception cref="NotImplementedException">if a required dependency is not found</exception>"
    internal static void BindDepencencies(this IServiceProvider serviceProvider, object obj)
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
        var properties = type.GetProperties(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttributes(typeof(ServiceDependencyAttribute), true);
            if (attribute.Length == 0)
                continue;

            var serviceDependencyAttribute = (ServiceDependencyAttribute)attribute[0];
            var service = serviceProvider.GetService(property.PropertyType);
            if (service == null && serviceDependencyAttribute.Required)
                throw new NotImplementedException($"Could not find service for {property.PropertyType.Name}.");

            property.SetValue(obj, service);
        }
    }

    private static void BindFieldDependencies(this IServiceProvider serviceProvider, object obj)
    {
        var type = obj.GetType();
        var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttributes(typeof(ServiceDependencyAttribute), true);
            if (attribute.Length == 0)
                continue;

            var serviceDependencyAttribute = (ServiceDependencyAttribute)attribute[0];
            var service = serviceProvider.GetService(field.FieldType);
            if (service == null && serviceDependencyAttribute.Required)
                throw new NotImplementedException($"Could not find service for {field.FieldType.Name}.");

            field.SetValue(obj, service);
        }
    }
}