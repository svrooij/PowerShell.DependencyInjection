using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Svrooij.PowerShell.DependencyInjection.Sample;

internal interface ITestService
{
    Task DoSomethingAsync(CancellationToken cancellationToken);
}

internal class TestService : ITestService
{
    private readonly ILogger<TestService> _logger;

    public TestService(ILogger<TestService> logger)
    {
        _logger = logger;
    }

    public async Task DoSomethingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting DoSomethingAsync() which can be cancelled with CTRL+C");
        _logger.LogInformation("Executing await Task.Delay(5000, cancellationToken);");
        await Task.Delay(5000, cancellationToken);
        _logger.LogInformation("Finsihed DoSomethingAsync()");
    }
}