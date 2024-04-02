@{
    # Script module or binary module file associated with this manifest.
    RootModule = 'Svrooij.PowerShell.DependencyInjection.Sample.dll'

    # Version number of this module.
    ModuleVersion = '0.2.0'

    # ID used to uniquely identify this module.
    GUID = '565d2077-b91f-4b27-81c4-e2ae049ec958'

    # Author of this module.
    Author = 'Stephan van Rooij'

    # Company or vendor that produced this module.
    CompanyName = 'Stephan van Rooij'

    Copyright = 'Stephan van Rooij 2024, licensed under GNU GPLv3'

    # Description of this module.
    Description = 'A sample module that shows of the dependency injection in PowerShell'

    # Minimum version of the Windows PowerShell engine required by this module.
    PowerShellVersion = '7.4.0'

    # Minimum version of the .NET Framework required by this module.
    # DotNetFrameworkVersion = '4.7.2'

    # Processor architecture (None, X86, Amd64) supported by this module.
    # ProcessorArchitecture = 'None'

    # Modules that must be imported into the global environment prior to importing this module.
    # RequiredModules = @()

    # Assemblies that must be loaded prior to importing this module.
    # RequiredAssemblies = @(
    #     "Microsoft.Extensions.Logging.Abstractions.dll",
    #     "SvR.ContentPrep.dll",
    #     "System.Buffers.dll",
    #     "System.Memory.dll",
    #     "System.Numerics.Vectors.dll",
    #     "System.Runtime.CompilerServices.Unsafe.dll"
    # )

    # Script files (.ps1) that are run in the caller's environment prior to importing this module.
    # ScriptsToProcess = @()

    # Type files (.ps1xml) that are loaded into the session prior to importing this module.
    # TypesToProcess = @()

    # Format files (.ps1xml) that are loaded into the session prior to importing this module.
    # FormatsToProcess = @()

    # Modules to import as nested modules of the module specified in RootModule/ModuleToProcess.
    # NestedModules = @()

    # Functions to export from this module.
    # FunctionsToExport = @()

    # Cmdlets to export from this module.
    CmdletsToExport = @(
        "Test-SampleCmdlet"
    )

    # Variables to export from this module.
    # VariablesToExport = @()

    # Aliases to export from this module.
    # AliasesToExport = @()

    # List of all files included in this module.
    FileList = @(
        "Microsoft.Bcl.AsyncInterfaces.dll",
        "Microsoft.Extensions.DependencyInjection.Abstractions.dll",
        "Microsoft.Extensions.DependencyInjection.dll",
        "Svrooij.PowerShell.DependencyInjection.dll",
        "Svrooij.PowerShell.DependencyInjection.Sample.dll",
        "Svrooij.PowerShell.DependencyInjection.Sample.pdb",
        "Svrooij.PowerShell.DependencyInjection.Sample.psd1",
        "System.Management.Automation.dll",
        "System.Runtime.CompilerServices.Unsafe.dll",
        "System.Threading.Tasks.Extensions.dll"
    )

    # Private data to pass to the module specified in RootModule/ModuleToProcess.
    PrivateData = @{
        PSData = @{
            Tags = @('Intune', 'Win32', 'ContentPrep')

            LisenceUri = 'https://github.com/svrooij/ContentPrep/blob/main/LICENSE.txt'
            ProjectUri = 'https://github.com/svrooij/ContentPrep/'
            ReleaseNotes = 'This module is still a work-in-progress. Changes might be made without notice.'
        }
    }

    # HelpInfo URI of this module.
    HelpInfoURI = 'https://github.com/svrooij/ContentPrep/blob/main/src/SvR.ContentPrep.Cmdlet/README.md'
}