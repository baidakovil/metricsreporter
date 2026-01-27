namespace MetricsReporter.Tests.Processing;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using MetricsReporter.Processing;
using System.Linq;
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
  public void TryParse_SuppressMessageWithOpenCoverBranchCoverageCategory_ReturnsTrue()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("OpenCoverBranchCoverage", "OpenCoverBranchCoverage", Justification = "Coverage suppression")]
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
    ruleId.Should().Be("OpenCoverBranchCoverage");
    justification.Should().Be("Coverage suppression");
  }

  [Test]
  public void TryParse_SuppressMessageWithOpenCoverSequenceCoverageCategory_ReturnsTrue()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("OpenCoverSequenceCoverage", "OpenCoverSequenceCoverage")]
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
    ruleId.Should().Be("OpenCoverSequenceCoverage");
    justification.Should().BeNull();
  }

  [Test]
  public void TryParse_SuppressMessageWithUnknownMetricCategory_ReturnsFalse()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [SuppressMessage("UnknownMetric", "OpenCoverBranchCoverage")]
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

  [Test]
  public void TryParseTargetFullyQualifiedName_WithTypeTarget_NormalizesNestedTypes()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [assembly: SuppressMessage(
          "OpenCoverBranchCoverage",
          "OpenCoverBranchCoverage",
          Scope = "type",
          Target = "~T:Sample.App.Worker+Nested",
          Justification = "Assembly-level type suppression")]
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();

    // Act
    var result = SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attribute, out var fullyQualifiedName);

    // Assert
    result.Should().BeTrue();
    fullyQualifiedName.Should().Be("Sample.App.Worker.Nested");
  }

  [Test]
  public void TryParseTargetFullyQualifiedName_WithMethodTarget_NormalizesSignature()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;
      [assembly: SuppressMessage(
          "OpenCoverBranchCoverage",
          "OpenCoverBranchCoverage",
          Scope = "member",
          Target = "~M:Sample.App.Worker.Do(System.Int32)",
          Justification = "Assembly-level method suppression")]
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();

    // Act
    var result = SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attribute, out var fullyQualifiedName);

    // Assert
    result.Should().BeTrue();
    fullyQualifiedName.Should().Be("Sample.App.Worker.Do(...)");
  }

  [Test]
  public void TryParseTargetFullyQualifiedName_WithOverloadedMethods_NormalizesToMethodPlaceholder()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;

      [assembly: SuppressMessage(
          "OpenCoverBranchCoverage",
          "OpenCoverBranchCoverage",
          Target = "~M:Sample.App.Worker.Do(System.Int32)")]
      [assembly: SuppressMessage(
          "OpenCoverBranchCoverage",
          "OpenCoverBranchCoverage",
          Target = "~M:Sample.App.Worker.Do(System.String,System.Boolean)")]
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attributes = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().ToArray();

    // Act
    var first = SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attributes[0], out var fqn1);
    var second = SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attributes[1], out var fqn2);

    // Assert
    first.Should().BeTrue();
    second.Should().BeTrue();
    fqn1.Should().Be("Sample.App.Worker.Do(...)");
    fqn2.Should().Be("Sample.App.Worker.Do(...)");
  }

  [Test]
  public void TryParseTargetFullyQualifiedName_WithGenericTypeAndMethod_StripsGenericArguments()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;

      [assembly: SuppressMessage(
          "OpenCoverBranchCoverage",
          "OpenCoverBranchCoverage",
          Target = "~M:Sample.App.Worker`1.Process`1(System.Collections.Generic.List{System.String})")]
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();

    // Act
    var result = SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attribute, out var fqn);

    // Assert
    result.Should().BeTrue();
    fqn.Should().Be("Sample.App.Worker.Process(...)");
  }

  [Test]
  public void TryParseTargetFullyQualifiedName_WithPropertyEventFieldPrefixes_NormalizesMemberFqn()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;

      [assembly: SuppressMessage("OpenCoverSequenceCoverage", "OpenCoverSequenceCoverage", Target = "~P:Sample.App.Worker.Value")]
      [assembly: SuppressMessage("OpenCoverBranchCoverage", "OpenCoverBranchCoverage", Target = "~E:Sample.App.Worker.Changed")]
      [assembly: SuppressMessage("OpenCoverBranchCoverage", "OpenCoverBranchCoverage", Target = "~F:Sample.App.Worker._flag")]
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attributes = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().ToArray();

    // Act / Assert
    SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attributes[0], out var propertyFqn).Should().BeTrue();
    propertyFqn.Should().Be("Sample.App.Worker.Value");

    SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attributes[1], out var eventFqn).Should().BeTrue();
    eventFqn.Should().Be("Sample.App.Worker.Changed");

    SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attributes[2], out var fieldFqn).Should().BeTrue();
    fieldFqn.Should().Be("Sample.App.Worker._flag");
  }

  [Test]
  public void TryParseTargetFullyQualifiedName_WithConstructors_NormalizesCtorAndCctor()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;

      [assembly: SuppressMessage("OpenCoverBranchCoverage", "OpenCoverBranchCoverage", Target = "~M:Sample.App.Worker.#ctor(System.Int32)")]
      [assembly: SuppressMessage("OpenCoverBranchCoverage", "OpenCoverBranchCoverage", Target = "~M:Sample.App.Worker.#cctor")]
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attributes = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().ToArray();

    // Act / Assert
    SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attributes[0], out var ctorFqn).Should().BeTrue();
    ctorFqn.Should().Be("Sample.App.Worker.#ctor(...)");

    SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attributes[1], out var cctorFqn).Should().BeTrue();
    cctorFqn.Should().Be("Sample.App.Worker.#cctor(...)");
  }

  [Test]
  public void TryParseTargetFullyQualifiedName_WithOperatorAndIndexer_NormalizesNames()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;

      [assembly: SuppressMessage("OpenCoverBranchCoverage", "OpenCoverBranchCoverage", Target = "~M:Sample.App.Worker.op_Equality(Sample.App.Worker,Sample.App.Worker)")]
      [assembly: SuppressMessage("OpenCoverBranchCoverage", "OpenCoverBranchCoverage", Target = "~M:Sample.App.Worker.Item(System.Int32)")]
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attributes = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().ToArray();

    // Act / Assert
    SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attributes[0], out var opFqn).Should().BeTrue();
    opFqn.Should().Be("Sample.App.Worker.op_Equality(...)");

    SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attributes[1], out var indexerFqn).Should().BeTrue();
    indexerFqn.Should().Be("Sample.App.Worker.Item(...)");
  }

  [Test]
  public void TryParseTargetFullyQualifiedName_WithDeepNestedTypes_NormalizesPlusToDot()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;

      [assembly: SuppressMessage("OpenCoverBranchCoverage", "OpenCoverBranchCoverage", Target = "~T:Sample.App.Outer+Middle+Inner")]
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();

    // Act
    var result = SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attribute, out var fqn);

    // Assert
    result.Should().BeTrue();
    fqn.Should().Be("Sample.App.Outer.Middle.Inner");
  }

  [Test]
  public void TryParseTargetFullyQualifiedName_WithModuleAttribute_Works()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;

      [module: SuppressMessage("OpenCoverBranchCoverage", "OpenCoverBranchCoverage", Target = "~T:Sample.App.Worker")]
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();

    // Act
    var result = SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attribute, out var fqn);

    // Assert
    result.Should().BeTrue();
    fqn.Should().Be("Sample.App.Worker");
  }

  [Test]
  public void TryParseTargetFullyQualifiedName_WithEmptyTarget_ReturnsFalse()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;

      [assembly: SuppressMessage("OpenCoverBranchCoverage", "OpenCoverBranchCoverage", Target = "")]
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();

    // Act
    var result = SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attribute, out var fqn);

    // Assert
    result.Should().BeFalse();
    fqn.Should().BeNull();
  }

  [Test]
  public void TryParseTargetFullyQualifiedName_WithInvalidPrefix_ReturnsFalse()
  {
    // Arrange
    var code = """
      using System.Diagnostics.CodeAnalysis;

      [assembly: SuppressMessage("OpenCoverBranchCoverage", "OpenCoverBranchCoverage", Target = "~X:Sample.App.Worker")]
      """;
    var syntaxTree = CSharpSyntaxTree.ParseText(code);
    var root = syntaxTree.GetRoot();
    var attribute = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.AttributeSyntax>().First();

    // Act
    var result = SuppressMessageAttributeParser.TryParseTargetFullyQualifiedName(attribute, out var fqn);

    // Assert
    result.Should().BeFalse();
    fqn.Should().BeNull();
  }
}






