$pesterConfig = [PesterConfiguration]@{
    Output = @{
      Verbosity = "Normal"
      CIFormat = "Auto"
      StackTraceVerbosity = "FirstLine"
    }
    OutputFormat = "NUnitXML"
    TestResult = @{
      Enabled = $true
      OutputPath = "TestResults.xml"
      OutputFormat = "JUnitXml"
    }
    Run = @{
      Path = "./tests/Pester-SampleModule"
      Exit = $true
    }
    Should = @{
      ErrorAction = "Continue"
    }
  }
  
  Invoke-Pester -Configuration $pesterConfig