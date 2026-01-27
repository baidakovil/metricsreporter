namespace MetricsReporter.Tests.Rendering;

using System.Linq;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="TableStructureBuilder"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class TableStructureBuilderTests
{
  [Test]
  public void AppendTableContainerAndActions_AppendsContainerAndActions()
  {
    // Arrange
    var builder = new StringBuilder();

    // Act
    TableStructureBuilder.AppendTableContainerAndActions(builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("<div class=\"table-container\">");
    result.Should().Contain("<div class=\"table-actions\">");
    result.Should().Contain("status-badges");
    result.Should().Contain("state-filters");
    result.Should().Contain("awareness-control");
    result.Should().Contain("filter-control");
    result.Should().Contain("detail-control");
    result.Should().Contain("expand-all");
    result.Should().Contain("collapse-all");
  }

  [Test]
  public void AppendTableContainerAndActions_IncludesFilterCheckboxes()
  {
    // Arrange
    var builder = new StringBuilder();

    // Act
    TableStructureBuilder.AppendTableContainerAndActions(builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("filter-new");
    result.Should().Contain("filter-changes");
    result.Should().Contain("filter-suppressed");
  }

  [Test]
  public void AppendTableHeader_WithMetrics_AppendsHeader()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.RoslynCyclomaticComplexity
    };
    var builder = new StringBuilder();

    // Act
    TableStructureBuilder.AppendTableHeader(metricOrder, builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("<table id=\"metrics-table\"");
    result.Should().Contain("<thead>");
    result.Should().Contain("data-col-group=\"OpenCover\"");
    result.Should().Contain("data-col-group=\"Roslyn\"");
    result.Should().Contain("data-col-group=\"Sarif\"");
    result.Should().Contain("data-col=\"RoslynClassCoupling\"");
    result.Should().Contain("data-col=\"RoslynCyclomaticComplexity\"");
    result.Should().Contain("<tbody>");
  }

  [Test]
  public void AppendTableHeader_WithAllMetricTypes_IncludesAllGroups()
  {
    // Arrange
    var metricOrder = new[]
    {
      MetricIdentifier.OpenCoverSequenceCoverage,
      MetricIdentifier.RoslynClassCoupling,
      MetricIdentifier.SarifCaRuleViolations
    };
    var builder = new StringBuilder();

    // Act
    TableStructureBuilder.AppendTableHeader(metricOrder, builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("OpenCover");
    result.Should().Contain("Roslyn");
    result.Should().Contain("Sarif");
  }

  [Test]
  public void AppendTableHeader_WithMetricNames_EncodesMetricNames()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var builder = new StringBuilder();

    // Act
    TableStructureBuilder.AppendTableHeader(metricOrder, builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("Class Coupling"); // Display name for RoslynClassCoupling
  }

  [Test]
  public void AppendTableHeader_WithSymbolColumn_IncludesSymbolColumn()
  {
    // Arrange
    var metricOrder = new[] { MetricIdentifier.RoslynClassCoupling };
    var builder = new StringBuilder();

    // Act
    TableStructureBuilder.AppendTableHeader(metricOrder, builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("data-col=\"symbol\"");
    result.Should().Contain("rowspan=\"2\"");
  }

  [Test]
  public void AppendTableClose_AppendsClosingTags()
  {
    // Arrange
    var builder = new StringBuilder();

    // Act
    TableStructureBuilder.AppendTableClose(builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("</tbody>");
    result.Should().Contain("</table>");
    result.Should().Contain("</div>");
  }

  [Test]
  public void AppendTableContainerAndActions_IncludesAriaLabels()
  {
    // Arrange
    var builder = new StringBuilder();

    // Act
    TableStructureBuilder.AppendTableContainerAndActions(builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("aria-label");
    result.Should().Contain("aria-valuemin");
    result.Should().Contain("aria-valuemax");
    result.Should().Contain("aria-valuenow");
  }

  [Test]
  public void AppendTableHeader_WithEmptyMetricOrder_StillBuildsStructure()
  {
    // Arrange
    var metricOrder = Array.Empty<MetricIdentifier>();
    var builder = new StringBuilder();

    // Act
    TableStructureBuilder.AppendTableHeader(metricOrder, builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("<table");
    result.Should().Contain("<thead>");
    result.Should().Contain("<tbody>");
  }

  [Test]
  public void AppendTableContainerAndActions_IncludesAllControls()
  {
    // Arrange
    var builder = new StringBuilder();

    // Act
    TableStructureBuilder.AppendTableContainerAndActions(builder);

    // Assert
    var result = builder.ToString();
    result.Should().Contain("id=\"awareness-level\"");
    result.Should().Contain("id=\"detail-level\"");
    result.Should().Contain("id=\"filter-input\"");
    result.Should().Contain("id=\"filter-clear\"");
    result.Should().Contain("id=\"expand-all\"");
    result.Should().Contain("id=\"collapse-all\"");
  }
}


