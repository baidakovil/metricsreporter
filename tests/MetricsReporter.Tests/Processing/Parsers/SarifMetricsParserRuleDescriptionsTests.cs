namespace MetricsReporter.Tests.Processing.Parsers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Processing.Parsers;

/// <summary>
/// Unit tests for <see cref="SarifMetricsParser"/> focusing on rule descriptions extraction.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class SarifMetricsParserRuleDescriptionsTests
{
  private SarifMetricsParser parser = null!;

  [SetUp]
  public void SetUp()
  {
    parser = new SarifMetricsParser();
  }

  [Test]
  public async Task ParseAsync_SingleCARuleWithFullDescription_ExtractsRuleDescription()
  {
    // Arrange
    var sarif = CreateSarifJsonWithRule(
        ruleId: "CA1502",
        shortDescription: "Avoid excessive complexity",
        fullDescription: "Methods should not have excessive cyclomatic complexity.",
        helpUri: "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502",
        category: "Maintainability");

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().ContainKey("CA1502");
      var description = result.RuleDescriptions["CA1502"];
      description.ShortDescription.Should().Be("Avoid excessive complexity");
      description.FullDescription.Should().Be("Methods should not have excessive cyclomatic complexity.");
      description.HelpUri.Should().Be("https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502");
      description.Category.Should().Be("Maintainability");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_SingleIDERuleWithMinimalDescription_ExtractsRuleDescription()
  {
    // Arrange
    var sarif = CreateSarifJsonWithRule(
        ruleId: "IDE0051",
        shortDescription: "Remove unused private members",
        fullDescription: null,
        helpUri: null,
        category: null);

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().ContainKey("IDE0051");
      var description = result.RuleDescriptions["IDE0051"];
      description.ShortDescription.Should().Be("Remove unused private members");
      description.FullDescription.Should().BeNull();
      description.HelpUri.Should().BeNull();
      description.Category.Should().BeNull();
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_MultipleRules_ExtractsAllRuleDescriptions()
  {
    // Arrange
    var sarif = CreateSarifJsonWithMultipleRules(new[]
    {
      ("CA1502", "Avoid excessive complexity", "Methods should not have excessive cyclomatic complexity.", "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1502", "Maintainability"),
      ("IDE0051", "Remove unused private members", null, null, null),
      ("CA1000", "Do not declare static members on generic types", "When a static member of a generic type is called...", "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1000", "Design")
    });

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().HaveCount(3);
      result.RuleDescriptions.Should().ContainKey("CA1502");
      result.RuleDescriptions.Should().ContainKey("IDE0051");
      result.RuleDescriptions.Should().ContainKey("CA1000");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RuleWithoutShortDescription_UsesEmptyString()
  {
    // Arrange
    var sarif = CreateSarifJsonWithRule(
        ruleId: "CA1502",
        shortDescription: null,
        fullDescription: "Some description",
        helpUri: null,
        category: null);

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().ContainKey("CA1502");
      result.RuleDescriptions["CA1502"].ShortDescription.Should().BeEmpty();
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_NonCARule_IgnoresRule()
  {
    // Arrange
    var sarif = CreateSarifJsonWithRule(
        ruleId: "CS1001",
        shortDescription: "Identifier expected",
        fullDescription: "Some description",
        helpUri: null,
        category: null);

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().BeEmpty("Non-CA/IDE rules should be ignored");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RuleWithoutId_IgnoresRule()
  {
    // Arrange
    var sarif = @"{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""tool"": {
        ""driver"": {
          ""name"": ""Test Tool"",
          ""rules"": [
            {
              ""shortDescription"": {
                ""text"": ""Test description""
              }
            }
          ]
        }
      },
      ""results"": []
    }
  ]
}";

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().BeEmpty("Rules without ID should be ignored");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_EmptyRuns_ReturnsEmptyRuleDescriptions()
  {
    // Arrange
    var sarif = @"{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": []
}";

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().BeEmpty();
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RunWithoutTool_ReturnsEmptyRuleDescriptions()
  {
    // Arrange
    var sarif = @"{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""results"": []
    }
  ]
}";

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().BeEmpty();
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RunWithoutDriver_ReturnsEmptyRuleDescriptions()
  {
    // Arrange
    var sarif = @"{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""tool"": {
        ""name"": ""Test Tool""
      },
      ""results"": []
    }
  ]
}";

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().BeEmpty();
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RunWithoutRules_ReturnsEmptyRuleDescriptions()
  {
    // Arrange
    var sarif = @"{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""tool"": {
        ""driver"": {
          ""name"": ""Test Tool""
        }
      },
      ""results"": []
    }
  ]
}";

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().BeEmpty();
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_MultipleRuns_ExtractsRulesFromAllRuns()
  {
    // Arrange
    var sarif = @"{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""tool"": {
        ""driver"": {
          ""name"": ""Test Tool"",
          ""rules"": [
            {
              ""id"": ""CA1502"",
              ""shortDescription"": {
                ""text"": ""Avoid excessive complexity""
              }
            }
          ]
        }
      },
      ""results"": []
    },
    {
      ""tool"": {
        ""driver"": {
          ""name"": ""Test Tool 2"",
          ""rules"": [
            {
              ""id"": ""IDE0051"",
              ""shortDescription"": {
                ""text"": ""Remove unused private members""
              }
            }
          ]
        }
      },
      ""results"": []
    }
  ]
}";

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().HaveCount(2);
      result.RuleDescriptions.Should().ContainKey("CA1502");
      result.RuleDescriptions.Should().ContainKey("IDE0051");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RuleWithPropertiesCategory_ExtractsCategory()
  {
    // Arrange
    var sarif = CreateSarifJsonWithRule(
        ruleId: "CA1502",
        shortDescription: "Avoid excessive complexity",
        fullDescription: null,
        helpUri: null,
        category: "Maintainability");

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions["CA1502"].Category.Should().Be("Maintainability");
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Test]
  public async Task ParseAsync_RuleWithoutProperties_HandlesGracefully()
  {
    // Arrange
    var sarif = @"{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {
      ""tool"": {
        ""driver"": {
          ""name"": ""Test Tool"",
          ""rules"": [
            {
              ""id"": ""CA1502"",
              ""shortDescription"": {
                ""text"": ""Avoid excessive complexity""
              }
            }
          ]
        }
      },
      ""results"": []
    }
  ]
}";

    var tempFile = CreateTempFile(sarif);

    try
    {
      // Act
      var result = await parser.ParseAsync(tempFile, CancellationToken.None);

      // Assert
      result.RuleDescriptions.Should().ContainKey("CA1502");
      result.RuleDescriptions["CA1502"].Category.Should().BeNull();
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  private static string CreateSarifJsonWithRule(
      string ruleId,
      string? shortDescription,
      string? fullDescription,
      string? helpUri,
      string? category)
  {
    var parts = new List<string>();

    if (shortDescription != null)
    {
      parts.Add($@"""shortDescription"": {{
              ""text"": ""{shortDescription}""
            }}");
    }

    if (fullDescription != null)
    {
      parts.Add($@"""fullDescription"": {{
              ""text"": ""{fullDescription}""
            }}");
    }

    if (helpUri != null)
    {
      parts.Add($@"""helpUri"": ""{helpUri}""");
    }

    if (category != null)
    {
      parts.Add($@"""properties"": {{
              ""category"": ""{category}""
            }}");
    }

    var ruleContent = string.Join(",\n              ", parts);

    return $@"{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
      ""tool"": {{
        ""driver"": {{
          ""name"": ""Test Tool"",
          ""rules"": [
            {{
              ""id"": ""{ruleId}"",
              {ruleContent}
            }}
          ]
        }}
      }},
      ""results"": []
    }}
  ]
}}";
  }

  private static string CreateSarifJsonWithMultipleRules(
      (string RuleId, string ShortDescription, string? FullDescription, string? HelpUri, string? Category)[] rules)
  {
    var rulesJson = string.Join(",\n            ", rules.Select((r, i) =>
    {
      var parts = new List<string>
      {
        $@"""id"": ""{r.RuleId}"""
      };

      parts.Add($@"""shortDescription"": {{
                ""text"": ""{r.ShortDescription}""
              }}");

      if (r.FullDescription != null)
      {
        parts.Add($@"""fullDescription"": {{
                ""text"": ""{r.FullDescription}""
              }}");
      }

      if (r.HelpUri != null)
      {
        parts.Add($@"""helpUri"": ""{r.HelpUri}""");
      }

      if (r.Category != null)
      {
        parts.Add($@"""properties"": {{
                ""category"": ""{r.Category}""
              }}");
      }

      var ruleContent = string.Join(",\n              ", parts);

      return $@"{{
              {ruleContent}
            }}";
    }));

    return $@"{{
  ""$schema"": ""http://json.schemastore.org/sarif-2.1.0"",
  ""version"": ""2.1.0"",
  ""runs"": [
    {{
      ""tool"": {{
        ""driver"": {{
          ""name"": ""Test Tool"",
          ""rules"": [
            {rulesJson}
          ]
        }}
      }},
      ""results"": []
    }}
  ]
}}";
  }

  private static string CreateTempFile(string content)
  {
    var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".sarif");
    File.WriteAllText(tempFile, content);
    return tempFile;
  }
}


