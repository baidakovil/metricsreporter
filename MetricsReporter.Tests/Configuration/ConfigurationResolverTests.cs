namespace MetricsReporter.Tests.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Configuration;
using NUnit.Framework;

[TestFixture]
[Category("Unit")]
internal sealed class ConfigurationResolverTests
{
  private MetricsReporterConfiguration _emptyConfig = null!;

  [SetUp]
  public void SetUp()
  {
    _emptyConfig = new MetricsReporterConfiguration();
  }

  [Test]
  public void ResolveGeneral_WhenAllSourcesMissing_UsesDefaultsAndCurrentDirectory()
  {
    // Arrange
    var cwd = Environment.CurrentDirectory;

    // Act
    var result = ConfigurationResolver.ResolveGeneral(
      cliVerbosity: null,
      cliTimeoutSeconds: null,
      cliWorkingDirectory: null,
      cliLogTruncation: null,
      envConfig: _emptyConfig,
      fileConfig: _emptyConfig);

    // Assert
    result.Verbosity.Should().Be("normal");
    result.Timeout.Should().Be(TimeSpan.FromSeconds(900));
    result.LogTruncationLimit.Should().Be(4000);
    result.WorkingDirectory.Should().Be(Path.GetFullPath(cwd));
  }

  [Test]
  public void ResolveGeneral_WhenCliProvided_OverridesEnvAndFile()
  {
    // Arrange
    var envConfig = new MetricsReporterConfiguration
    {
      General = new GeneralConfiguration
      {
        Verbosity = "minimal",
        TimeoutSeconds = 10,
        WorkingDirectory = "envDir",
        LogTruncationLimit = 10
      }
    };
    var fileConfig = new MetricsReporterConfiguration
    {
      General = new GeneralConfiguration
      {
        Verbosity = "quiet",
        TimeoutSeconds = 20,
        WorkingDirectory = "fileDir",
        LogTruncationLimit = 20
      }
    };

    // Act
    var result = ConfigurationResolver.ResolveGeneral(
      cliVerbosity: "detailed",
      cliTimeoutSeconds: 30,
      cliWorkingDirectory: ".",
      cliLogTruncation: 30,
      envConfig,
      fileConfig);

    // Assert
    result.Verbosity.Should().Be("detailed");
    result.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    result.LogTruncationLimit.Should().Be(30);
    result.WorkingDirectory.Should().Be(Path.GetFullPath("."));
  }

  [Test]
  public void ResolveGeneral_WhenCliTimeoutNonPositive_FallsBackToDefault()
  {
    // Arrange
    // Act
    var result = ConfigurationResolver.ResolveGeneral(
      cliVerbosity: null,
      cliTimeoutSeconds: 0,
      cliWorkingDirectory: null,
      cliLogTruncation: 0,
      envConfig: _emptyConfig,
      fileConfig: _emptyConfig);

    // Assert
    result.Timeout.Should().Be(TimeSpan.FromSeconds(900));
    result.LogTruncationLimit.Should().Be(4000);
  }

  [Test]
  public void ResolveGeneral_WhenEnvHasNegativeValues_FallsBackToDefaults()
  {
    // Arrange
    var envConfig = new MetricsReporterConfiguration
    {
      General = new GeneralConfiguration
      {
        Verbosity = " detailed ",
        TimeoutSeconds = -5,
        WorkingDirectory = "envDir",
        LogTruncationLimit = -1
      }
    };

    // Act
    var result = ConfigurationResolver.ResolveGeneral(
      cliVerbosity: null,
      cliTimeoutSeconds: null,
      cliWorkingDirectory: null,
      cliLogTruncation: null,
      envConfig,
      _emptyConfig);

    // Assert
    result.Verbosity.Should().Be("detailed");
    result.Timeout.Should().Be(TimeSpan.FromSeconds(900));
    result.LogTruncationLimit.Should().Be(4000);
    result.WorkingDirectory.Should().Be(Path.GetFullPath("envDir"));
  }

  [Test]
  public void ResolveGeneral_WhenFileWorkingDirectoryUsed_ResolvesToFullPath()
  {
    // Arrange
    var fileConfig = new MetricsReporterConfiguration
    {
      General = new GeneralConfiguration
      {
        WorkingDirectory = "relativeDir"
      }
    };

    // Act
    var result = ConfigurationResolver.ResolveGeneral(
      cliVerbosity: null,
      cliTimeoutSeconds: null,
      cliWorkingDirectory: null,
      cliLogTruncation: null,
      envConfig: _emptyConfig,
      fileConfig);

    // Assert
    result.WorkingDirectory.Should().Be(Path.GetFullPath("relativeDir"));
  }

  [Test]
  public void ResolveGeneral_WhenEnvProvided_OverridesFile()
  {
    // Arrange
    var envConfig = new MetricsReporterConfiguration
    {
      General = new GeneralConfiguration
      {
        Verbosity = "quiet",
        TimeoutSeconds = 600,
        WorkingDirectory = "envDir",
        LogTruncationLimit = 123
      }
    };
    var fileConfig = new MetricsReporterConfiguration
    {
      General = new GeneralConfiguration
      {
        Verbosity = "minimal",
        TimeoutSeconds = 120,
        WorkingDirectory = "fileDir",
        LogTruncationLimit = 50
      }
    };

    // Act
    var result = ConfigurationResolver.ResolveGeneral(
      cliVerbosity: null,
      cliTimeoutSeconds: null,
      cliWorkingDirectory: null,
      cliLogTruncation: null,
      envConfig,
      fileConfig);

    // Assert
    result.Verbosity.Should().Be("quiet");
    result.Timeout.Should().Be(TimeSpan.FromSeconds(600));
    result.LogTruncationLimit.Should().Be(123);
    result.WorkingDirectory.Should().Be(Path.GetFullPath("envDir"));
  }

  [Test]
  public void ResolveScripts_PrefersCliThenEnvThenFile()
  {
    // Arrange
    var envScripts = new ScriptsConfiguration
    {
      Generate = new[] { "env-gen.ps1" },
      Read = new ReadScriptsConfiguration
      {
        Any = new[] { "env-read.ps1" },
        ByMetric = new[]
        {
          new MetricScript { Metrics = new[] { "EnvMetric" }, Path = "env-metric.ps1" }
        }
      }
    };
    var fileScripts = new ScriptsConfiguration
    {
      Generate = new[] { "file-gen.ps1" },
      Read = new ReadScriptsConfiguration
      {
        Any = new[] { "file-read.ps1" },
        ByMetric = new[]
        {
          new MetricScript { Metrics = new[] { "FileMetric" }, Path = "file-metric.ps1" }
        }
      }
    };

    // Act
    var result = ConfigurationResolver.ResolveScripts(
      cliGenerate: new[] { "cli-gen.ps1" },
      cliReadAny: new[] { "cli-read.ps1" },
      cliMetricScripts: new[] { ("CliMetric", "cli-metric.ps1") },
      envScripts,
      fileScripts);

    // Assert
    result.Generate.Should().ContainSingle().Which.Should().Be("cli-gen.ps1");
    result.ReadAny.Should().ContainSingle().Which.Should().Be("cli-read.ps1");
    result.ReadByMetric.Should().ContainSingle();
    result.ReadByMetric[0].Metrics.Should().ContainSingle().Which.Should().Be("CliMetric");
    result.ReadByMetric[0].Path.Should().Be("cli-metric.ps1");
  }

  [Test]
  public void ResolveScripts_WhenCliAbsent_UsesEnvBeforeFile()
  {
    // Arrange
    var envScripts = new ScriptsConfiguration
    {
      Generate = new[] { "env-gen.ps1" },
      Read = new ReadScriptsConfiguration
      {
        Any = new[] { "env-read.ps1" },
        ByMetric = Array.Empty<MetricScript>()
      }
    };
    var fileScripts = new ScriptsConfiguration
    {
      Generate = new[] { "file-gen.ps1" },
      Read = new ReadScriptsConfiguration
      {
        Any = new[] { "file-read.ps1" },
        ByMetric = new[]
        {
          new MetricScript { Metrics = new[] { "FileMetric" }, Path = "file-metric.ps1" }
        }
      }
    };

    // Act
    var result = ConfigurationResolver.ResolveScripts(
      cliGenerate: Array.Empty<string>(),
      cliReadAny: Array.Empty<string>(),
      cliMetricScripts: Array.Empty<(string Metric, string Path)>(),
      envScripts,
      fileScripts);

    // Assert
    result.Generate.Should().ContainSingle().Which.Should().Be("env-gen.ps1");
    result.ReadAny.Should().ContainSingle().Which.Should().Be("env-read.ps1");
    result.ReadByMetric.Should().ContainSingle();
    result.ReadByMetric[0].Metrics.Should().ContainSingle().Which.Should().Be("FileMetric");
    result.ReadByMetric[0].Path.Should().Be("file-metric.ps1");
  }

  [Test]
  public void ResolveScripts_WhenEnvMissingMetricScripts_FallsBackToFileMetrics()
  {
    // Arrange
    var envScripts = new ScriptsConfiguration
    {
      Generate = null,
      Read = new ReadScriptsConfiguration
      {
        Any = null,
        ByMetric = Array.Empty<MetricScript>()
      }
    };
    var fileScripts = new ScriptsConfiguration
    {
      Generate = new[] { "file-gen.ps1" },
      Read = new ReadScriptsConfiguration
      {
        Any = new[] { "file-read.ps1" },
        ByMetric = new[]
        {
          new MetricScript { Metrics = new[] { "FileMetric" }, Path = "file-metric.ps1" }
        }
      }
    };

    // Act
    var result = ConfigurationResolver.ResolveScripts(
      cliGenerate: Array.Empty<string>(),
      cliReadAny: Array.Empty<string>(),
      cliMetricScripts: Array.Empty<(string Metric, string Path)>(),
      envScripts,
      fileScripts);

    // Assert
    result.Generate.Should().ContainSingle().Which.Should().Be("file-gen.ps1");
    result.ReadAny.Should().ContainSingle().Which.Should().Be("file-read.ps1");
    result.ReadByMetric.Should().ContainSingle();
    result.ReadByMetric[0].Path.Should().Be("file-metric.ps1");
  }

  [Test]
  public void ResolveScripts_WhenCliMetricHasMultipleMetrics_PreservesAllMetrics()
  {
    // Arrange
    var envScripts = new ScriptsConfiguration
    {
      Generate = Array.Empty<string>(),
      Read = new ReadScriptsConfiguration
      {
        Any = Array.Empty<string>(),
        ByMetric = Array.Empty<MetricScript>()
      }
    };

    // Act
    var result = ConfigurationResolver.ResolveScripts(
      cliGenerate: Array.Empty<string>(),
      cliReadAny: Array.Empty<string>(),
      cliMetricScripts: new[] { ("MetricA", "cli-a.ps1"), ("MetricB,MetricC", "cli-b.ps1") },
      envScripts,
      _emptyConfig.Scripts);

    // Assert
    result.ReadByMetric.Should().HaveCount(2);
    result.ReadByMetric[0].Metrics.Should().ContainSingle().Which.Should().Be("MetricA");
    result.ReadByMetric[1].Metrics.Should().ContainSingle().Which.Should().Be("MetricB,MetricC");
    result.ReadByMetric[0].Path.Should().Be("cli-a.ps1");
    result.ReadByMetric[1].Path.Should().Be("cli-b.ps1");
  }
}

