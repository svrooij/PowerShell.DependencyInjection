using System;

namespace Svrooij.PowerShell.DependencyInjection;

/// <summary>
/// <see cref="ServiceDependencyAttribute"/> is used to mark properties or fields that need to come from dependency injection"/>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ServiceDependencyAttribute : Attribute
{
    /// <summary>
    /// Should this service exist in the <see cref="IServiceProvider"/>?
    /// </summary>
    /// <remarks>If this is true and a service isn't avaialable it will throw an error.</remarks>
    public bool Required { get; set; } = true;
}