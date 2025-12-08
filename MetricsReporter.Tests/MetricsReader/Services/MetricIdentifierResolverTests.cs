using System;
using System.Collections.Generic;
using FluentAssertions;
using MetricsReporter.MetricsReader.Services;
using MetricsReporter.Model;
using NUnit.Framework;

namespace MetricsReporter.Tests.MetricsReader.Services;

[TestFixture]
[Category("Unit")]
internal sealed class MetricIdentifierResolverTests
{
  [Test]
  public void TryResolve_WithAliasFromConfiguration_ReturnsCanonical()
  {
    var aliases = new Dictionary<MetricIdentifier, IReadOnlyList<string>>
    {
      [MetricIdentifier.RoslynCyclomaticComplexity] = new[] { "cc", "cyclomatic" }
    };
    var resolver = new MetricIdentifierResolver(aliases);

    var success = resolver.TryResolve("cc", out var metric);

    success.Should().BeTrue();
    metric.Should().Be(MetricIdentifier.RoslynCyclomaticComplexity);
  }

  [Test]
  public void TryResolve_WithUnknownAlias_ReturnsFalse()
  {
    var resolver = MetricIdentifierResolver.Empty;

    var success = resolver.TryResolve("nonexistent", out var metric);

    success.Should().BeFalse();
    metric.Should().Be(default);
  }

  [Test]
  public void Constructor_WithDuplicateAliasAcrossMetrics_Throws()
  {
    var aliases = new Dictionary<MetricIdentifier, IReadOnlyList<string>>
    {
      [MetricIdentifier.AltCoverBranchCoverage] = new[] { "branch" },
      [MetricIdentifier.RoslynCyclomaticComplexity] = new[] { "branch" }
    };

    Action act = () => _ = new MetricIdentifierResolver(aliases);

    act.Should().Throw<ArgumentException>()
      .Which.Message.Should().Contain("Alias 'branch'");
  }

  [Test]
  public void TryResolve_WhenNoAliases_FallsBackToEnumParsing()
  {
    var resolver = MetricIdentifierResolver.Empty;

    var success = resolver.TryResolve("RoslynMaintainabilityIndex", out var metric);

    success.Should().BeTrue();
    metric.Should().Be(MetricIdentifier.RoslynMaintainabilityIndex);
  }

  [Test]
  public void Constructor_TrimsAndDeduplicatesAliases()
  {
    var resolver = new MetricIdentifierResolver(
      new Dictionary<MetricIdentifier, IReadOnlyList<string>>
      {
        [MetricIdentifier.RoslynCyclomaticComplexity] = new[] { " cc ", "CC" }
      });

    resolver.AliasesByMetric[MetricIdentifier.RoslynCyclomaticComplexity].Should().BeEquivalentTo("cc");
  }

  [Test]
  public void TryResolve_IgnoresAliasesEqualToIdentifier()
  {
    var resolver = new MetricIdentifierResolver(
      new Dictionary<MetricIdentifier, IReadOnlyList<string>>
      {
        [MetricIdentifier.RoslynCyclomaticComplexity] = new[] { "RoslynCyclomaticComplexity" }
      });

    resolver.AliasesByMetric.Should().BeEmpty();
    resolver.TryResolve("RoslynCyclomaticComplexity", out var metric).Should().BeTrue();
    metric.Should().Be(MetricIdentifier.RoslynCyclomaticComplexity);
  }

  [Test]
  public void Constructor_WithNullAliasEntry_IgnoresIt()
  {
    var resolver = new MetricIdentifierResolver(
      new Dictionary<MetricIdentifier, IReadOnlyList<string>>
      {
        [MetricIdentifier.RoslynCyclomaticComplexity] = null!
      });

    resolver.AliasesByMetric.Should().BeEmpty();
  }

  [Test]
  public void TryResolve_WithWhitespaceAlias_ResolvesAfterTrim()
  {
    var resolver = new MetricIdentifierResolver(
      new Dictionary<MetricIdentifier, IReadOnlyList<string>>
      {
        [MetricIdentifier.RoslynCyclomaticComplexity] = new[] { "  cc  " }
      });

    resolver.TryResolve("cc", out var metric).Should().BeTrue();
    metric.Should().Be(MetricIdentifier.RoslynCyclomaticComplexity);
  }

  [Test]
  public void Constructor_RemovesDuplicateAliasCaseInsensitive()
  {
    var resolver = new MetricIdentifierResolver(
      new Dictionary<MetricIdentifier, IReadOnlyList<string>>
      {
        [MetricIdentifier.RoslynCyclomaticComplexity] = new[] { "CC", "cc", "Cc" }
      });

    resolver.AliasesByMetric.Should().ContainKey(MetricIdentifier.RoslynCyclomaticComplexity);
    resolver.AliasesByMetric[MetricIdentifier.RoslynCyclomaticComplexity].Should().BeEquivalentTo("CC");
  }
}


