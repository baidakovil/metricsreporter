namespace MetricsReporter.Tests.Processing;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using MetricsReporter.Processing;
/// <summary>
/// Unit tests for <see cref="SuppressMessageAttributeParser"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class SuppressMessageAttributeParserTests
{
  [Test]
  public void TryParse_ValidSuppressMessageAttribute_ReturnsTrue()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("Microsoft.Maintainability", "CA1506")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();
    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);
    // Assert
    result.Should().BeTrue();
    ruleId.Should().Be("CA1506");
    justification.Should().BeNull();
  }

  [Test]
  public void TryParse_SuppressMessageWithAltCoverBranchCoverageCategory_ReturnsTrue()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("AltCoverBranchCoverage", "AltCoverBranchCoverage", Justification = "Coverage suppression")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();

    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);

    // Assert
    result.Should().BeTrue();
    ruleId.Should().Be("AltCoverBranchCoverage");
    justification.Should().Be("Coverage suppression");
  }

  [Test]
  public void TryParse_SuppressMessageWithAltCoverSequenceCoverageCategory_ReturnsTrue()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("AltCoverSequenceCoverage", "AltCoverSequenceCoverage")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();

    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);

    // Assert
    result.Should().BeTrue();
    ruleId.Should().Be("AltCoverSequenceCoverage");
    justification.Should().BeNull();
  }

  [Test]
  public void TryParse_SuppressMessageWithUnknownMetricCategory_ReturnsFalse()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("UnknownMetric", "AltCoverBranchCoverage")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();

    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);

    // Assert
    result.Should().BeFalse();
    ruleId.Should().BeNull();
    justification.Should().BeNull();
  }
  [Test]
  public void TryParse_SuppressMessageWithJustification_ExtractsJustification()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("Microsoft.Maintainability", "CA1506", Justification = "Test justification")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();
    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);
    // Assert
    result.Should().BeTrue();
    ruleId.Should().Be("CA1506");
    justification.Should().Be("Test justification");
  }
  [Test]
  public void TryParse_SuppressMessageWithConcatenatedJustification_ExtractsAllParts()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("Microsoft.Maintainability", "CA1506", Justification = "Part1" + " " + "Part2")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();
    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);
    // Assert
    result.Should().BeTrue();
    ruleId.Should().Be("CA1506");
    justification.Should().Be("Part1 Part2");
  }
  [Test]
  public void TryParse_SuppressMessageWithRuleIdAndDescription_ExtractsOnlyRuleId()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("Microsoft.Maintainability", "CA1506:Avoid excessive class coupling")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();
    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);
    // Assert
    result.Should().BeTrue();
    ruleId.Should().Be("CA1506");
    justification.Should().BeNull();
  }
  [Test]
  public void TryParse_NonSuppressMessageAttribute_ReturnsFalse()
  {
    // Arrange
    var code = """
      [Obsolete("Test")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();
    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);
    // Assert
    result.Should().BeFalse();
    ruleId.Should().BeNull();
    justification.Should().BeNull();
  }
  [Test]
  public void TryParse_SuppressMessageWithStyleCategory_ReturnsTrue()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("Style", "IDE0001")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();
    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);
    // Assert
    result.Should().BeTrue();
    ruleId.Should().Be("IDE0001");
  }
  [Test]
  public void TryParse_SuppressMessageWithFullyQualifiedName_ReturnsTrue()
  {
    // Arrange
    var code = """
      [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();
    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);
    // Assert
    result.Should().BeTrue();
    ruleId.Should().Be("CA1506");
  }
  [Test]
  public void TryParse_SuppressMessageWithInsufficientArguments_ReturnsFalse()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("Microsoft.Maintainability")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();
    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);
    // Assert
    result.Should().BeFalse();
    ruleId.Should().BeNull();
    justification.Should().BeNull();
  }
  [Test]
  public void TryParse_SuppressMessageWithInvalidCategory_ReturnsFalse()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("Invalid.Category", "CA1506")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();
    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);
    // Assert
    result.Should().BeFalse();
    ruleId.Should().BeNull();
    justification.Should().BeNull();
  }
  [Test]
  public void TryParse_SuppressMessageWithEmptyCheckId_ReturnsFalse()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("Microsoft.Maintainability", "")]
      public class TestClass
      {
      }
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();
    // Act
    var result = SuppressMessageAttributeParser.TryParse(attribute, out var ruleId, out var justification);
    // Assert
    result.Should().BeFalse();
    ruleId.Should().BeNull();
    justification.Should().BeNull();
  }
}






