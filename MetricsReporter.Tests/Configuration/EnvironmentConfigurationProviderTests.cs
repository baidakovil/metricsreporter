using System;
using System.Collections.Generic;
using FluentAssertions;
using MetricsReporter.Configuration;
using NUnit.Framework;

namespace MetricsReporter.Tests.Configuration;

[TestFixture]
[Category("Unit")]
public sealed class EnvironmentConfigurationProviderTests
{
  private readonly Dictionary<string, string?> _originalValues = new(StringComparer.Ordinal);

  [SetUp]
  public void SetUp()
  {
    _originalValues.Clear();
  }

  [TearDown]
  public void TearDown()
  {
    foreach (var pair in _originalValues)
    {
      Environment.SetEnvironmentVariable(pair.Key, pair.Value);
    }
  }

  [Test]
  public void ReadAliases_WhenVariableMissing_ReturnsNull()
  {
    SetMetricAliases(null);

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.MetricAliases.Should().BeNull();
  }

  [Test]
  public void ReadAliases_WithInvalidJson_ReturnsNull()
  {
    SetMetricAliases("{ invalid json");

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.MetricAliases.Should().BeNull();
  }

  [Test]
  public void ReadAliases_WithNonObjectRoot_ReturnsNull()
  {
    SetMetricAliases("[1,2,3]");

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.MetricAliases.Should().BeNull();
  }

  [Test]
  public void ReadAliases_WithMixedContent_ReturnsDistinctTrimmedAliases()
  {
    const string payload = """
    {
      "RoslynClassCoupling": [" coupling ", "Coupling", " ", 5, "COUPLING", "depth"],
      "RoslynDepthOfInheritance": "not-an-array",
      "RoslynExecutableLines": []
    }
    """;
    SetMetricAliases(payload);

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.MetricAliases.Should().NotBeNull();
    var aliases = configuration.MetricAliases!;
    aliases.Should().ContainKey("RoslynClassCoupling");
    aliases["RoslynClassCoupling"].Should().BeEquivalentTo("coupling", "depth");
    aliases.Should().NotContainKey("RoslynDepthOfInheritance");
    aliases.Should().NotContainKey("RoslynExecutableLines");
  }

  [Test]
  public void ReadAliases_WithArrayWithoutStrings_ReturnsNull()
  {
    SetMetricAliases("""{"RoslynClassCoupling": [1, true]}""");

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.MetricAliases.Should().BeNull();
  }

  [Test]
  public void Read_WhenBooleanVariablesProvided_ParsesValues()
  {
    SetEnvironmentVariable("METRICSREPORTER_RUN_SCRIPTS", "true");
    SetEnvironmentVariable("METRICSREPORTER_AGGREGATE_AFTER_SCRIPTS", "false");

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.General.RunScripts.Should().BeTrue();
    configuration.General.AggregateAfterScripts.Should().BeFalse();
  }

  [Test]
  public void Read_WhenBooleanVariableIsInvalid_ReturnsNull()
  {
    SetEnvironmentVariable("METRICSREPORTER_RUN_SCRIPTS", "not-a-bool");

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.General.RunScripts.Should().BeNull();
  }

  [Test]
  public void Read_WhenIntegerVariablesProvided_ParsesValues()
  {
    SetEnvironmentVariable("METRICSREPORTER_TIMEOUT_SECONDS", "120");
    SetEnvironmentVariable("METRICSREPORTER_LOG_TRUNCATION_LIMIT", "256");

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.General.TimeoutSeconds.Should().Be(120);
    configuration.General.LogTruncationLimit.Should().Be(256);
  }

  [Test]
  public void Read_WhenIntegerVariableIsInvalid_ReturnsNull()
  {
    SetEnvironmentVariable("METRICSREPORTER_TIMEOUT_SECONDS", "not-an-int");

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.General.TimeoutSeconds.Should().BeNull();
  }

  [Test]
  public void ReadList_WithMixedSeparators_TrimsAndFiltersEntries()
  {
    SetEnvironmentVariable("METRICSREPORTER_PATHS_OPENCOVER", " first.xml;; , second.xml, third.xml ; ; fourth.xml ");

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.Paths.OpenCover.Should().BeEquivalentTo("first.xml", "second.xml", "third.xml", "fourth.xml");
  }

  [Test]
  public void ReadList_WhenVariableIsWhitespace_ReturnsNull()
  {
    SetEnvironmentVariable("METRICSREPORTER_PATHS_ROSLYN", "   ");

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.Paths.Roslyn.Should().BeNull();
  }

  [Test]
  public void ReadMetricScripts_WhenVariableMissing_ReturnsEmptyList()
  {
    SetEnvironmentVariable("METRICSREPORTER_SCRIPTS_TEST_BYMETRIC", null);

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.Scripts.Test.ByMetric.Should().BeEmpty();
  }

  [Test]
  public void ReadMetricScripts_WithInvalidEntries_ReturnsOnlyValidScripts()
  {
    const string payload = "RoslynMetrics: ./scripts/read.ps1; invalidentry; :missingmetrics; metric-three: script3.ps1; metric-four, : script4.ps1; metric-five:  ";
    SetEnvironmentVariable("METRICSREPORTER_SCRIPTS_READ_BYMETRIC", payload);

    var configuration = EnvironmentConfigurationProvider.Read();

    configuration.Scripts.Read.ByMetric.Should().HaveCount(3);
    configuration.Scripts.Read.ByMetric[0].Metrics.Should().BeEquivalentTo("RoslynMetrics");
    configuration.Scripts.Read.ByMetric[0].Path.Should().Be("./scripts/read.ps1");
    configuration.Scripts.Read.ByMetric[1].Metrics.Should().BeEquivalentTo("metric-three");
    configuration.Scripts.Read.ByMetric[1].Path.Should().Be("script3.ps1");
    configuration.Scripts.Read.ByMetric[2].Metrics.Should().BeEquivalentTo("metric-four");
    configuration.Scripts.Read.ByMetric[2].Path.Should().Be("script4.ps1");
  }

  private void SetMetricAliases(string? value)
  {
    SetEnvironmentVariable("METRICSREPORTER_METRIC_ALIASES", value);
  }

  private void SetEnvironmentVariable(string name, string? value)
  {
    if (!_originalValues.ContainsKey(name))
    {
      _originalValues[name] = Environment.GetEnvironmentVariable(name);
    }

    Environment.SetEnvironmentVariable(name, value);
  }
}


