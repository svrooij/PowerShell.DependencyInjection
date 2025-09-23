# Source generator `Code` folder

This folder contains code that will be injected into project where the `Svrooij.PowerShell.DI` source generator is used.

This part of the project file handles this:

```xml
  <!-- This is to embed all files in Code folder, instead of compiling them -->
  <ItemGroup>
    <!-- Don't compile the files in this folder -->
    <Compile Remove="Code\**\*.cs" />
    <!-- Instead, embed them as resources -->
    <EmbeddedResource Include="Code\**\*.cs">
        <LogicalName>%(Filename)%(Extension)</LogicalName>
    </EmbeddedResource>
  </ItemGroup>
```

Thanks, [Thomas Huijer](https://www.linkedin.com/in/thomas-huijer-5879bb/) for figuring this out!