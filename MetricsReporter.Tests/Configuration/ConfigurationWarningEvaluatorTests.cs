using System.Linq;
using MetricsReporter.Configuration;
using MetricsReporter.Services;
using NUnit.Framework;

namespace MetricsReporter.Tests.Configuration;

public class ConfigurationWarningEvaluatorTests
{
  [Test]
  public void ReplaceBaseline_Warns_When_BaselinePath_Missing()
  {
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      ReplaceMetricsBaseline = true,
      MetricsReportStoragePath = null,
      BaselinePath = null,
      AltCoverPaths = new[] { "a.xml" },
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "replaceBaseline.baselinePath"), Is.True);
  }

  [Test]
  public void ReplaceBaseline_NoWarning_When_BaselinePath_Present()
  {
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      ReplaceMetricsBaseline = true,
      BaselinePath = "baseline.json",
      MetricsReportStoragePath = "storage",
      AltCoverPaths = new[] { "a.xml" },
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "replaceBaseline.baselinePath"), Is.False);
  }

  [Test]
  public void AnalyzeSuppressedSymbols_Warns_When_OutputPath_Missing()
  {
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = null,
      SourceCodeFolders = new[] { "src" },
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "suppressedSymbols.outputPath"), Is.True);
  }

  [Test]
  public void AnalyzeSuppressedSymbols_Warns_When_SourceFolders_Missing()
  {
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = "suppressed.json",
      SourceCodeFolders = new string[0],
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "suppressedSymbols.sourceFolders"), Is.True);
  }

  [Test]
  public void MetricsInputs_Warns_When_All_Sources_Missing_And_No_InputJson()
  {
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      InputJsonPath = null,
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "metricsInputs.missingAll"), Is.True);
  }

  [Test]
  public void MetricsInputs_NoWarning_When_InputJson_Provided()
  {
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      InputJsonPath = "report.json",
      OutputHtmlPath = "out.html",
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "metricsInputs.missingAll"), Is.False);
  }

  [Test]
  public void MetricsInputs_NoWarning_When_AtLeastOne_Source_Present()
  {
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      InputJsonPath = null,
      AltCoverPaths = new[] { "a.xml" },
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "metricsInputs.missingAll"), Is.False);
  }

  [Test]
  public void BaselineReference_Warns_When_No_Baseline()
  {
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      BaselineReference = "commit-123",
      BaselinePath = null,
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "baselineReference.noBaseline"), Is.True);
  }

  [Test]
  public void BaselineStorage_Warns_When_Directory_Missing()
  {
    var missingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      ReplaceMetricsBaseline = true,
      BaselinePath = "baseline.json",
      MetricsReportStoragePath = missingDir,
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "replaceBaseline.storagePath"), Is.True);
  }

  [Test]
  public void BaselineStorage_NoWarning_When_Directory_Exists()
  {
    var existingDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
    try
    {
      var options = new MetricsReporterOptions
      {
        CommandName = "generate",
        ReplaceMetricsBaseline = true,
        BaselinePath = "baseline.json",
        MetricsReportStoragePath = existingDir,
        OutputJsonPath = "out.json",
        MetricsDirectory = "metrics"
      };

      var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

      Assert.That(warnings.Any(w => w.RuleId == "replaceBaseline.storagePath"), Is.False);
    }
    finally
    {
      Directory.Delete(existingDir, recursive: true);
    }
  }

  [Test]
  public void CoverageHtml_Warns_When_Directory_Missing()
  {
    var missingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      CoverageHtmlDir = missingDir,
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "coverageHtml.missingDir"), Is.True);
  }

  [Test]
  public void CoverageHtml_NoWarning_When_Directory_Exists()
  {
    var existingDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
    try
    {
      var options = new MetricsReporterOptions
      {
        CommandName = "generate",
        CoverageHtmlDir = existingDir,
        OutputJsonPath = "out.json",
        MetricsDirectory = "metrics"
      };

      var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

      Assert.That(warnings.Any(w => w.RuleId == "coverageHtml.missingDir"), Is.False);
    }
    finally
    {
      Directory.Delete(existingDir, recursive: true);
    }
  }

  [Test]
  public void SuppressedSymbols_Warns_When_Parent_Directory_Missing()
  {
    var missingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "sub");
    var filePath = Path.Combine(missingDir, "suppressed.json");
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = filePath,
      SourceCodeFolders = new[] { "src" },
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "suppressedSymbols.parentDirectory"), Is.True);
  }

  [Test]
  public void SuppressedSymbols_NoWarning_When_Parent_Directory_Exists()
  {
    var existingDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
    var filePath = Path.Combine(existingDir, "suppressed.json");
    try
    {
      var options = new MetricsReporterOptions
      {
        CommandName = "generate",
        AnalyzeSuppressedSymbols = true,
        SuppressedSymbolsPath = filePath,
        SourceCodeFolders = new[] { "src" },
        SolutionDirectory = existingDir,
        OutputJsonPath = "out.json",
        MetricsDirectory = "metrics"
      };

      var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

      Assert.That(warnings.Any(w => w.RuleId == "suppressedSymbols.parentDirectory"), Is.False);
    }
    finally
    {
      Directory.Delete(existingDir, recursive: true);
    }
  }

  [Test]
  public void SolutionDirectory_Warns_When_Missing_With_Analyze()
  {
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      AnalyzeSuppressedSymbols = true,
      SuppressedSymbolsPath = Path.Combine(Path.GetTempPath(), "suppressed.json"),
      SourceCodeFolders = new[] { "src" },
      OutputJsonPath = "out.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "suppressedSymbols.solutionDirectory"), Is.True);
  }

  [Test]
  public void SolutionDirectory_NoWarning_When_Present_With_Analyze()
  {
    var existingDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).FullName;
    try
    {
      var options = new MetricsReporterOptions
      {
        CommandName = "generate",
        AnalyzeSuppressedSymbols = true,
        SuppressedSymbolsPath = Path.Combine(existingDir, "suppressed.json"),
        SourceCodeFolders = new[] { "src" },
        SolutionDirectory = existingDir,
        OutputJsonPath = "out.json",
        MetricsDirectory = "metrics"
      };

      var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

      Assert.That(warnings.Any(w => w.RuleId == "suppressedSymbols.solutionDirectory"), Is.False);
    }
    finally
    {
      Directory.Delete(existingDir, recursive: true);
    }
  }

  [Test]
  public void ReplaceBaseline_ReportPath_Warns_When_Missing()
  {
    var options = new MetricsReporterOptions
    {
      CommandName = "generate",
      ReplaceMetricsBaseline = true,
      BaselinePath = "baseline.json",
      MetricsDirectory = "metrics"
    };

    var warnings = ConfigurationWarningEvaluator.CollectWarnings(options, "generate");

    Assert.That(warnings.Any(w => w.RuleId == "replaceBaseline.reportPath"), Is.True);
  }
}

