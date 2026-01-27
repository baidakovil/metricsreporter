namespace MetricsReporter.Tests.Aggregation;

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using MetricsReporter.Tests.TestHelpers;

/// <summary>
/// Unit tests for <see cref="MetricsAggregationService"/> focusing on rule descriptions merging.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class MetricsAggregationServiceRuleDescriptionsTests
{
  private MetricsAggregationService service = null!;
  private Dictionary<MetricIdentifier, MetricThresholdDefinition> thresholds = null!;

  [SetUp]
  public void SetUp()
  {
    service = new MetricsAggregationService();
    thresholds = new Dictionary<MetricIdentifier, MetricThresholdDefinition>
    {
      [MetricIdentifier.SarifCaRuleViolations] = ThresholdTestFactory.CreateDefinition(1, 2, false)
    };
  }

  [Test]
  public void BuildReport_SingleSarifDocumentWithRuleDescriptions_IncludesRuleDescriptionsInMetadata()
  {
    // Arrange
    var sarifDocument = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = "Methods should not have excessive cyclomatic complexity.",
          HelpUri = "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502",
          Category = "Maintainability"
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
    var description = report.Metadata.RuleDescriptions["CA1502"];
    description.ShortDescription.Should().Be("Avoid excessive complexity");
    description.FullDescription.Should().Be("Methods should not have excessive cyclomatic complexity.");
    description.HelpUri.Should().Be("https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502");
    description.Category.Should().Be("Maintainability");
  }

  [Test]
  public void BuildReport_MultipleSarifDocumentsWithDifferentRules_MergesAllRuleDescriptions()
  {
    // Arrange
    var sarifDocument1 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = "Methods should not have excessive cyclomatic complexity.",
          HelpUri = "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502",
          Category = "Maintainability"
        }
      }
    };

    var sarifDocument2 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["IDE0051"] = new RuleDescription
        {
          ShortDescription = "Remove unused private members",
          FullDescription = null,
          HelpUri = null,
          Category = null
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument1, sarifDocument2 },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    report.Metadata.RuleDescriptions.Should().HaveCount(2);
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
    report.Metadata.RuleDescriptions.Should().ContainKey("IDE0051");
  }

  [Test]
  public void BuildReport_MultipleSarifDocumentsWithSameRule_FirstDescriptionIsUsed()
  {
    // Arrange
    var sarifDocument1 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "First description",
          FullDescription = "First full description",
          HelpUri = "https://first.example.com",
          Category = "Maintainability"
        }
      }
    };

    var sarifDocument2 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Second description",
          FullDescription = "Second full description",
          HelpUri = "https://second.example.com",
          Category = "Design"
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument1, sarifDocument2 },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var originalError = Console.Error;
    using var consoleOutput = new StringWriter();
    Console.SetError(consoleOutput);
    try
    {
      var report = service.BuildReport(input);

      // Assert
      report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
      report.Metadata.RuleDescriptions["CA1502"].ShortDescription.Should().Be("First description", "First encountered description should be used");
      var output = consoleOutput.ToString();
      output.Should().Contain("WARNING", "Warning should be issued for conflicting descriptions");
      output.Should().Contain("CA1502", "Warning should mention the rule ID");
    }
    finally
    {
      Console.SetError(originalError);
    }
  }

  [Test]
  public void BuildReport_MultipleSarifDocumentsWithIdenticalRuleDescriptions_NoWarning()
  {
    // Arrange
    var description = new RuleDescription
    {
      ShortDescription = "Avoid excessive complexity",
      FullDescription = "Methods should not have excessive cyclomatic complexity.",
      HelpUri = "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502",
      Category = "Maintainability"
    };

    var sarifDocument1 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = description
      }
    };

    var sarifDocument2 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = description
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument1, sarifDocument2 },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var originalError = Console.Error;
    using var consoleOutput = new StringWriter();
    Console.SetError(consoleOutput);
    try
    {
      var report = service.BuildReport(input);

      // Assert
      report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
      var output = consoleOutput.ToString();
      output.Should().NotContain("WARNING", "No warning should be issued for identical descriptions");
    }
    finally
    {
      Console.SetError(originalError);
    }
  }

  [Test]
  public void BuildReport_EmptySarifDocuments_ReturnsEmptyRuleDescriptions()
  {
    // Arrange
    var sarifDocument = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>()
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    report.Metadata.RuleDescriptions.Should().BeEmpty();
  }

  [Test]
  public void BuildReport_NoSarifDocuments_ReturnsEmptyRuleDescriptions()
  {
    // Arrange
    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument>(),
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    report.Metadata.RuleDescriptions.Should().BeEmpty();
  }

  [Test]
  public void BuildReport_RuleDescriptionWithPartialFields_MergesCorrectly()
  {
    // Arrange
    var sarifDocument1 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = null,
          HelpUri = "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502",
          Category = null
        }
      }
    };

    var sarifDocument2 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Avoid excessive complexity",
          FullDescription = null,
          HelpUri = "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502",
          Category = null
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument1, sarifDocument2 },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var report = service.BuildReport(input);

    // Assert
    report.Metadata.RuleDescriptions.Should().ContainKey("CA1502");
    report.Metadata.RuleDescriptions["CA1502"].ShortDescription.Should().Be("Avoid excessive complexity");
    report.Metadata.RuleDescriptions["CA1502"].FullDescription.Should().BeNull();
    report.Metadata.RuleDescriptions["CA1502"].HelpUri.Should().Be("https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502");
    report.Metadata.RuleDescriptions["CA1502"].Category.Should().BeNull();
  }

  [Test]
  public void BuildReport_ConflictingRuleDescriptions_DifferentShortDescription_WarnsAndUsesFirst()
  {
    // Arrange
    var sarifDocument1 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "First description",
          FullDescription = "Full description",
          HelpUri = "https://example.com",
          Category = "Maintainability"
        }
      }
    };

    var sarifDocument2 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Second description",
          FullDescription = "Full description",
          HelpUri = "https://example.com",
          Category = "Maintainability"
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument1, sarifDocument2 },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var originalError = Console.Error;
    using var consoleOutput = new StringWriter();
    Console.SetError(consoleOutput);
    try
    {
      var report = service.BuildReport(input);

      // Assert
      report.Metadata.RuleDescriptions["CA1502"].ShortDescription.Should().Be("First description");
      var output = consoleOutput.ToString();
      output.Should().Contain("WARNING");
      output.Should().Contain("First description");
      output.Should().Contain("Second description");
    }
    finally
    {
      Console.SetError(originalError);
    }
  }

  [Test]
  public void BuildReport_ConflictingRuleDescriptions_DifferentFullDescription_WarnsAndUsesFirst()
  {
    // Arrange
    var sarifDocument1 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Same short",
          FullDescription = "First full description",
          HelpUri = "https://example.com",
          Category = "Maintainability"
        }
      }
    };

    var sarifDocument2 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Same short",
          FullDescription = "Second full description",
          HelpUri = "https://example.com",
          Category = "Maintainability"
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument1, sarifDocument2 },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var originalError = Console.Error;
    using var consoleOutput = new StringWriter();
    Console.SetError(consoleOutput);
    try
    {
      var report = service.BuildReport(input);

      // Assert
      report.Metadata.RuleDescriptions["CA1502"].FullDescription.Should().Be("First full description");
      var output = consoleOutput.ToString();
      output.Should().Contain("WARNING");
    }
    finally
    {
      Console.SetError(originalError);
    }
  }

  [Test]
  public void BuildReport_ConflictingRuleDescriptions_DifferentHelpUri_WarnsAndUsesFirst()
  {
    // Arrange
    var sarifDocument1 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Same short",
          FullDescription = "Same full",
          HelpUri = "https://first.example.com",
          Category = "Maintainability"
        }
      }
    };

    var sarifDocument2 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Same short",
          FullDescription = "Same full",
          HelpUri = "https://second.example.com",
          Category = "Maintainability"
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument1, sarifDocument2 },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var originalError = Console.Error;
    using var consoleOutput = new StringWriter();
    Console.SetError(consoleOutput);
    try
    {
      var report = service.BuildReport(input);

      // Assert
      report.Metadata.RuleDescriptions["CA1502"].HelpUri.Should().Be("https://first.example.com");
      var output = consoleOutput.ToString();
      output.Should().Contain("WARNING");
    }
    finally
    {
      Console.SetError(originalError);
    }
  }

  [Test]
  public void BuildReport_ConflictingRuleDescriptions_DifferentCategory_WarnsAndUsesFirst()
  {
    // Arrange
    var sarifDocument1 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Same short",
          FullDescription = "Same full",
          HelpUri = "https://example.com",
          Category = "Maintainability"
        }
      }
    };

    var sarifDocument2 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Same short",
          FullDescription = "Same full",
          HelpUri = "https://example.com",
          Category = "Design"
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument1, sarifDocument2 },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var originalError = Console.Error;
    using var consoleOutput = new StringWriter();
    Console.SetError(consoleOutput);
    try
    {
      var report = service.BuildReport(input);

      // Assert
      report.Metadata.RuleDescriptions["CA1502"].Category.Should().Be("Maintainability");
      var output = consoleOutput.ToString();
      output.Should().Contain("WARNING");
    }
    finally
    {
      Console.SetError(originalError);
    }
  }

  [Test]
  public void BuildReport_MultipleRulesWithConflicts_WarnsForEachConflict()
  {
    // Arrange
    var sarifDocument1 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "First CA1502",
          FullDescription = null,
          HelpUri = null,
          Category = null
        },
        ["IDE0051"] = new RuleDescription
        {
          ShortDescription = "First IDE0051",
          FullDescription = null,
          HelpUri = null,
          Category = null
        }
      }
    };

    var sarifDocument2 = new ParsedMetricsDocument
    {
      SolutionName = "TestSolution",
      Elements = new List<ParsedCodeElement>(),
      RuleDescriptions = new Dictionary<string, RuleDescription>
      {
        ["CA1502"] = new RuleDescription
        {
          ShortDescription = "Second CA1502",
          FullDescription = null,
          HelpUri = null,
          Category = null
        },
        ["IDE0051"] = new RuleDescription
        {
          ShortDescription = "Second IDE0051",
          FullDescription = null,
          HelpUri = null,
          Category = null
        }
      }
    };

    var input = new MetricsAggregationInput
    {
      SolutionName = "TestSolution",
      OpenCoverDocuments = new List<ParsedMetricsDocument>(),
      RoslynDocuments = new List<ParsedMetricsDocument>(),
      SarifDocuments = new List<ParsedMetricsDocument> { sarifDocument1, sarifDocument2 },
      Baseline = null,
      Thresholds = thresholds,
      Paths = new ReportPaths()
    };

    // Act
    var originalError = Console.Error;
    using var consoleOutput = new StringWriter();
    Console.SetError(consoleOutput);
    try
    {
      var report = service.BuildReport(input);

      // Assert
      report.Metadata.RuleDescriptions.Should().HaveCount(2);
      var output = consoleOutput.ToString();
      var warningCount = output.Split(new[] { "WARNING" }, StringSplitOptions.None).Length - 1;
      warningCount.Should().BeGreaterOrEqualTo(2, "Should warn for each conflicting rule");
    }
    finally
    {
      Console.SetError(originalError);
    }
  }
}


