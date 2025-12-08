using System;
using FluentAssertions;
using MetricsReporter.Configuration;
using NUnit.Framework;

namespace MetricsReporter.Tests.Configuration;

[TestFixture]
[Category("Unit")]
public sealed class EnvironmentConfigurationProviderTests
{
  private const string MetricAliasesVariable = "METRICSREPORTER_METRIC_ALIASES";
  private string? _originalMetricAliases;

  [SetUp]
  public void SetUp()
  {
    _originalMetricAliases = Environment.GetEnvironmentVariable(MetricAliasesVariable);
  }

  [TearDown]
  public void TearDown()
  {
    Environment.SetEnvironmentVariable(MetricAliasesVariable, _originalMetricAliases);
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

  private static void SetMetricAliases(string? value)
  {
    Environment.SetEnvironmentVariable(MetricAliasesVariable, value);
  }
}


