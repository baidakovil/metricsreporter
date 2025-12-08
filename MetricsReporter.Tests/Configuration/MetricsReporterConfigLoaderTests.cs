namespace MetricsReporter.Tests.Configuration;

using System;
using System.IO;
using FluentAssertions;
using MetricsReporter.Configuration;
using NUnit.Framework;

[TestFixture]
[Category("Unit")]
internal sealed class MetricsReporterConfigLoaderTests
{
  private string _root = null!;
  private MetricsReporterConfigLoader _loader = null!;

  [SetUp]
  public void SetUp()
  {
    _root = Path.Combine(Path.GetTempPath(), $"cfg-loader-{Guid.NewGuid():N}");
    Directory.CreateDirectory(_root);
    _loader = new MetricsReporterConfigLoader();
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_root))
    {
      Directory.Delete(_root, recursive: true);
    }
  }

  [Test]
  public void Load_WhenNoFileFound_ReturnsNotFound()
  {
    // Arrange

    // Act
    var result = _loader.Load(requestedPath: null, workingDirectory: _root);

    // Assert
    result.Path.Should().BeNull();
    result.IsSuccess.Should().BeTrue();
    result.Configuration.Should().NotBeNull();
  }

  [Test]
  public void Load_WithExplicitPath_NotExisting_ReturnsFailure()
  {
    // Arrange
    var explicitPath = Path.Combine(_root, "missing.json");

    // Act
    var result = _loader.Load(explicitPath, _root);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Path.Should().Be(Path.GetFullPath(explicitPath));
    result.Errors.Should().NotBeEmpty();
  }

  [Test]
  public void Load_WithMalformedJson_ReturnsFailure()
  {
    // Arrange
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, "{ invalid json");

    // Act
    var result = _loader.Load(null, _root);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Path.Should().Be(configPath);
    result.Errors.Should().ContainSingle();
  }

  [Test]
  public void Load_WithValidJson_ReturnsSuccessAndConfiguration()
  {
    // Arrange
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": { "runScripts": false, "aggregateAfterScripts": true, "verbosity": "minimal", "timeoutSeconds": 10, "workingDirectory": "work", "logTruncationLimit": 99 },
      "paths": { "metricsDir": "build/Metrics" },
      "scripts": { "generate": [ "scripts/run.ps1" ], "read": { "any": [], "byMetric": [] }, "test": { "any": [ "scripts/test.ps1" ] } }
    }
    """);

    // Act
    var result = _loader.Load(null, _root);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Path.Should().Be(configPath);
    result.Configuration.General.RunScripts.Should().BeFalse();
    result.Configuration.General.AggregateAfterScripts.Should().BeTrue();
    result.Configuration.General.Verbosity.Should().Be("minimal");
    result.Configuration.Paths.MetricsDir.Should().Be("build/Metrics");
    result.Configuration.Scripts.Generate.Should().ContainSingle().Which.Should().Be("scripts/run.ps1");
    result.Configuration.Scripts.Test.Any.Should().ContainSingle().Which.Should().Be("scripts/test.ps1");
  }

  [Test]
  public void Load_WhenJsonIsNull_ReturnsFailure()
  {
    // Arrange
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, "null");

    // Act
    var result = _loader.Load(null, _root);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().NotBeEmpty();
  }

  [Test]
  public void Load_WithUnknownRootProperty_ReturnsFailure()
  {
    // Arrange
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """{ "unknown": {} }""");

    // Act
    var result = _loader.Load(null, _root);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("Unknown root property"));
  }

  [Test]
  public void Load_WithMissingRequiredSections_ReturnsFailure()
  {
    // Arrange
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """{ "general": {}, "paths": {} }""");

    // Act
    var result = _loader.Load(null, _root);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("Missing required section 'scripts'"));
  }

  [Test]
  public void Load_WithInvalidGeneralProperty_ReturnsFailure()
  {
    // Arrange
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": { "unexpected": true },
      "paths": {},
      "scripts": {}
    }
    """);

    // Act
    var result = _loader.Load(null, _root);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("Invalid property in 'general' section."));
  }

  [Test]
  public void Load_WithNonObjectPathsSection_ReturnsFailure()
  {
    // Arrange
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": {},
      "paths": "not-an-object",
      "scripts": {}
    }
    """);

    // Act
    var result = _loader.Load(null, _root);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("Invalid property in 'paths' section."));
  }

  [Test]
  public void Load_WithInvalidByMetricEntry_ReturnsFailure()
  {
    // Arrange
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": {},
      "paths": {},
      "scripts": {
        "read": {
          "byMetric": [ { "metrics": [], "path": "" } ]
        }
      }
    }
    """);

    // Act
    var result = _loader.Load(null, _root);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("byMetric items must contain non-empty 'metrics'"));
  }

  [Test]
  public void Load_WithDuplicateMetricDifferentPaths_ReturnsFailure()
  {
    // Arrange
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": {},
      "paths": {},
      "scripts": {
        "read": {
          "byMetric": [
            { "metrics": [ "RoslynClassCoupling" ], "path": "scripts/first.ps1" },
            { "metrics": [ "RoslynClassCoupling" ], "path": "scripts/second.ps1" }
          ]
        }
      }
    }
    """);

    // Act
    var result = _loader.Load(null, _root);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("Duplicate metric 'RoslynClassCoupling'"));
  }

  [Test]
  public void Load_WithInvalidTestByMetricEntry_ReturnsFailure()
  {
    // Arrange
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": {},
      "paths": {},
      "scripts": {
        "test": {
          "byMetric": [ { "metrics": [], "path": "" } ]
        }
      }
    }
    """);

    // Act
    var result = _loader.Load(null, _root);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("scripts.test.byMetric items must contain non-empty 'metrics'"));
  }

  [Test]
  public void Load_WithDuplicateTestMetricDifferentPaths_ReturnsFailure()
  {
    // Arrange
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": {},
      "paths": {},
      "scripts": {
        "test": {
          "byMetric": [
            { "metrics": [ "RoslynClassCoupling" ], "path": "scripts/first.ps1" },
            { "metrics": [ "RoslynClassCoupling" ], "path": "scripts/second.ps1" }
          ]
        }
      }
    }
    """);

    // Act
    var result = _loader.Load(null, _root);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("scripts.test.byMetric"));
  }

  [Test]
  public void Load_WithUnknownMetricAliasKey_ReturnsFailure()
  {
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": {},
      "paths": {},
      "scripts": { "generate": [], "read": {}, "test": {} },
      "metricAliases": { "UnknownMetric": [ "x" ] }
    }
    """);

    var result = _loader.Load(null, _root);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("Unknown metric identifier"));
  }

  [Test]
  public void Load_WithEmptyAliasArray_ReturnsFailure()
  {
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": {},
      "paths": {},
      "scripts": { "generate": [], "read": {}, "test": {} },
      "metricAliases": { "RoslynCyclomaticComplexity": [] }
    }
    """);

    var result = _loader.Load(null, _root);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("non-empty array"));
  }

  [Test]
  public void Load_WithNonStringAlias_ReturnsFailure()
  {
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": {},
      "paths": {},
      "scripts": { "generate": [], "read": {}, "test": {} },
      "metricAliases": { "RoslynCyclomaticComplexity": [ 1 ] }
    }
    """);

    var result = _loader.Load(null, _root);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("must contain only strings"));
  }

  [Test]
  public void Load_WithDuplicateAliasAcrossMetrics_ReturnsFailure()
  {
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": {},
      "paths": {},
      "scripts": { "generate": [], "read": {}, "test": {} },
      "metricAliases": {
        "RoslynCyclomaticComplexity": [ "cc" ],
        "RoslynClassCoupling": [ "cc" ]
      }
    }
    """);

    var result = _loader.Load(null, _root);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("assigned to multiple metrics"));
  }

  [Test]
  public void Load_WithEmptyAliasString_ReturnsFailure()
  {
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": {},
      "paths": {},
      "scripts": { "generate": [], "read": {}, "test": {} },
      "metricAliases": { "RoslynCyclomaticComplexity": [ "  " ] }
    }
    """);

    var result = _loader.Load(null, _root);

    result.IsSuccess.Should().BeFalse();
    result.Errors.Should().ContainSingle(e => e.Contains("non-empty strings"));
  }

  [Test]
  public void Load_WithValidMetricAliases_Succeeds()
  {
    var configPath = Path.Combine(_root, ".metricsreporter.json");
    File.WriteAllText(configPath, """
    {
      "general": {},
      "paths": { "report": "report.json", "readReport": "report.json" },
      "scripts": { "generate": [], "read": {}, "test": {} },
      "metricAliases": { "RoslynCyclomaticComplexity": [ "cc", "cyclomatic" ] }
    }
    """);

    var result = _loader.Load(null, _root);

    result.IsSuccess.Should().BeTrue();
    result.Configuration.MetricAliases.Should().ContainKey("RoslynCyclomaticComplexity");
  }

  [Test]
  public void ResolveConfigPath_WalksUpwardToFindFile()
  {
    // Arrange
    var parent = Directory.CreateDirectory(Path.Combine(_root, "parent"));
    var child = Directory.CreateDirectory(Path.Combine(parent.FullName, "child"));
    var configPath = Path.Combine(parent.FullName, ".metricsreporter.json");
    File.WriteAllText(configPath, "{}");

    // Act
    var resolved = MetricsReporterConfigLoader.ResolveConfigPath(null, child.FullName);

    // Assert
    resolved.Should().Be(configPath);
  }

  [Test]
  public void ResolveConfigPath_WithRequestedPath_ReturnsFullPath()
  {
    // Arrange
    var requested = Path.Combine(_root, "custom.json");

    // Act
    var resolved = MetricsReporterConfigLoader.ResolveConfigPath(requested, _root);

    // Assert
    resolved.Should().Be(Path.GetFullPath(requested));
  }
}

