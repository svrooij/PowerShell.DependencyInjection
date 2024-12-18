BeforeAll {
  Import-Module ./sample/net8-sample/bin/Release/net8.0/Svrooij.PowerShell.DependencyInjection.Sample.dll -Force
}

Describe "SampleModule" {
  Context "Test-SampleCmdlet" {
    It "Should be available" {
      Get-Command -Name Test-SampleCmdlet -Module Svrooij.PowerShell.DependencyInjection.Sample | Should -Not -BeNull
    }

    It "Should return input" {
      $result = Test-SampleCmdlet 42 -FavoritePet Cat
      # Check if the result is an object with the correct properties
      $result | Should -BeAnObject -And -HaveProperty 'Number' -And -HaveProperty 'FavoritePet'
    }
  }
}
