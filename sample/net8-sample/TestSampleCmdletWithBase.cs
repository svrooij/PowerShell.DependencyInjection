using Microsoft.Extensions.Logging;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Svrooij.PowerShell.DI;

namespace Svrooij.PowerShell.DependencyInjection.Sample;

/// <summary>
/// Base cmdlet class for dependency injection sample, using the Startup class for service configuration.
/// </summary>
public class DepCmdlet : DependencyCmdlet<Startup>
{

}

/// <summary>
/// Cmdlet for testing sample dependency injection and logging in PowerShell, using a custom base class.
/// </summary>
[Cmdlet(VerbsDiagnostic.Test, "SampleCmdletWithBase")]
[OutputType(typeof(FavoriteStuff))]
[GenerateBindings]
public partial class TestSampleCmdletWithBaseCommand : DepCmdlet
{
    /// <summary>
    /// Gets or sets the user's favorite number. This value is required and can be provided via the pipeline.
    /// </summary>
    [Parameter(
        Mandatory = true,
        Position = 0,
        ValueFromPipeline = true,
        ValueFromPipelineByPropertyName = true)]
    public int FavoriteNumber { get; set; }

    /// <summary>
    /// Gets or sets the user's favorite pet. Valid values are "Cat", "Dog", or "Horse". Defaults to "Dog".
    /// </summary>
    [Parameter(
        Position = 1,
        ValueFromPipelineByPropertyName = true)]
    [ValidateSet("Cat", "Dog", "Horse")]
    public string FavoritePet { get; set; } = "Dog";

    // Give your dependencies the ServiceDependency attribute
    // Only private/internal properties and fields with this attribute will be resolved
    // public properties and fields will be ignored because they might also be exposed to powershell.
    [ServiceDependency]
    private Svrooij.PowerShell.DependencyInjection.Sample.ITestService _testService;

    // Logging using Microsoft.Extensions.Logging is supported (and configured automatically)
    // You can alse use the regular WriteVerbose(), WriteDebug(), WriteInformation(), WriteWarning() and WriteError() methods
    [ServiceDependency(Required = true)]
    private Microsoft.Extensions.Logging.ILogger<TestSampleCmdletWithBaseCommand> _logger;

    /// <summary>
    /// Processes each record received from the pipeline. Logs information and calls the test service.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting ProcessRecordAsync()");

        // In the startup class we configured the logging level for this namespace to Debug
        _logger.LogDebug("FavoriteNumber: {FavoriteNumber}, FavoritePet: {favoritePet}", FavoriteNumber, FavoritePet);

        //_logger.LogWarning("This is a warning");
        //_logger.LogError("This is an error");

        await _testService.DoSomethingAsync(cancellationToken);

        WriteObject(new FavoriteStuff
        {
            FavoriteNumber = this.FavoriteNumber,
            FavoritePet = this.FavoritePet
        });
    }
}
