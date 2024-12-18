BeforeAll {
  Import-Module ./sample/net8-sample/bin/Release/net8.0/Svrooij.PowerShell.DependencyInjection.Sample.dll -Force
}

Describe "SampleModule" {
  Context "Test-SampleCmdlet" {
    It "Should be available" {
      Get-Command -Name Test-SampleCmdlet -Module Svrooij.PowerShell.DependencyInjection.Sample | Should -Not -BeNull
    }

    It "Should return input" {
      $result = Test-SampleCmdlet -FavoriteNumber 42 -FavoritePet Cat
      # Check if the result is an object with the correct properties
      $result.FavoriteNumber | Should -Be 42
      $result.FavoritePet | Should -Be 'Cat'
    }
  }
}
