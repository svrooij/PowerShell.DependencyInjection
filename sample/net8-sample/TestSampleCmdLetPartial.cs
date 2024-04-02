// using Microsoft.Extensions.DependencyInjection;
// using System;
// using System;
// using Microsoft.Extensions.Logging;
// using System.Management.Automation;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Svrooij.PowerShell.DependencyInjection.Sample
// {
//     public partial class TestSampleCmdletCommand
//     {
//         protected override Action<DependencyCmdlet<Startup>, IServiceProvider> BindDependencies => (obj, serviceProvider) =>
//         {
//             if (obj is TestSampleCmdletCommand cmdlet)
//             {
//                 cmdlet.TestService = (ITestService)serviceProvider.GetService(typeof(ITestService));
//                 cmdlet._logger = (ILogger<TestSampleCmdletCommand>)serviceProvider.GetService(typeof(ILogger<TestSampleCmdletCommand>));
//             }
//         };
//     }
// }