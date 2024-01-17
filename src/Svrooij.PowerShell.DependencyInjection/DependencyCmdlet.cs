using Microsoft.Extensions.DependencyInjection;
using Svrooij.PowerShell.DependencyInjection.Extensions;
using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace Svrooij.PowerShell.DependencyInjection;

/// <summary>
/// Base class for cmdlets that use dependency injection.
/// </summary>
/// <typeparam name="T">Your startup class that has to extend <see cref="PsStartup"/> and extend <see cref="PsStartup.ConfigureServices(IServiceCollection)"/>.</typeparam>
/// <remarks>You should override <see cref="DependencyCmdlet{T}.ProcessRecordAsync(CancellationToken)"/>. A lot of other methods are blocked from overriding.</remarks>
public abstract class DependencyCmdlet<T> : PSCmdlet where T : PsStartup, new()
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// <see cref="DependencyCmdlet{T}"/> constructor, called by PowerShell.
    /// </summary>
    protected DependencyCmdlet()
    {
        var startup = new T();
        var services = new ServiceCollection();
        startup.ConfigurePowerShellServices(services, this);
        startup.ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Override this method to process each record.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token will be called when the user presses CTRL+C during execution</param>
    /// <remarks>Your overridden method will be called automatically!</remarks>
    /// <exception cref="NotImplementedException">When not overridden</exception>
    /// <exception cref="InvalidOperationException">When one or more dependencies marked as required are not found</exception>
    public virtual Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException($"You'll need to override {nameof(ProcessRecordAsync)}()");
    }
    
    /// <summary>
    /// Override this property to bind dependencies manually, the service provider is provided by the base library.
    /// </summary>
    protected virtual Action<DependencyCmdlet<T>, IServiceProvider> BindDependencies {
        get
        {
            return (obj, serviceProvider) => serviceProvider.BindDependencies(obj);
        }
    }

    /// <summary>
    /// You can call ProcessRecord, but you cannot override it!
    /// </summary>
    /// <remarks>Override the <see cref="ProcessRecordAsync"/> method, which is called automatically.</remarks>
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
        BindDependencies(this, _serviceProvider);
        base.BeginProcessing();
    }

    /// <summary>
    /// You can call StopProcessing, but you cannot override it!
    /// </summary>
    /// <remarks>This is called by PowerShell if the user cancels the request. If is used to trigger the cancellationToken on <see cref="ProcessRecordAsync(CancellationToken)"/>.</remarks>
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
        base.EndProcessing();
    }
}