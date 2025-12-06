namespace MetricsReporter.Tests.Processing;

using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Processing;

/// <summary>
/// Unit tests for <see cref="RuleIdValidator"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class RuleIdValidatorTests
{
  [Test]
  public void IsValidRuleId_ValidCARule_ReturnsTrue()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("CA1502");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void IsValidRuleId_ValidIDERule_ReturnsTrue()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("IDE0051");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void IsValidRuleId_ValidCARuleWithLeadingZeros_ReturnsTrue()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("CA0001");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void IsValidRuleId_ValidIDERuleWithLeadingZeros_ReturnsTrue()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("IDE0001");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void IsValidRuleId_ValidCARuleMaximum_ReturnsTrue()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("CA9999");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void IsValidRuleId_ValidIDERuleMaximum_ReturnsTrue()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("IDE9999");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void IsValidRuleId_Null_ReturnsFalse()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId(null);

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void IsValidRuleId_EmptyString_ReturnsFalse()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId(string.Empty);

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void IsValidRuleId_Whitespace_ReturnsFalse()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("   ");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void IsValidRuleId_TooShort_ReturnsFalse()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("CA123");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void IsValidRuleId_TooLong_ReturnsFalse()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("CA12345");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void IsValidRuleId_WrongPrefix_ReturnsFalse()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("XX1502");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void IsValidRuleId_NonNumericSuffix_ReturnsFalse()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("CA15AB");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void IsValidRuleId_LowercaseCA_ReturnsFalse()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("ca1502");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void IsValidRuleId_LowercaseIDE_ReturnsFalse()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("ide0051");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void IsValidRuleId_MixedCaseCA_ReturnsFalse()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("Ca1502");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void IsValidRuleId_MixedCaseIDE_ReturnsFalse()
  {
    // Act
    var result = RuleIdValidator.IsValidRuleId("Ide0051");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void IsValidRuleId_RealWorldExamples_AllValid()
  {
    // Arrange - Real rule IDs from the codebase
    var realRuleIds = new[]
    {
      "CA1502", // Cyclomatic complexity
      "CA1506", // Class coupling
      "CA1501", // Depth of inheritance
      "CA1505", // Maintainability index
      "IDE0028", // Collection initialization
      "IDE0051", // Remove unused private members
      "IDE0060", // Remove unused parameter
      "IDE0059", // Unnecessary assignment
      "CA1869", // Use 'string.Contains(char)' instead of 'string.IndexOf(char)'
      "CA1861", // Constant arrays should not be declared as 'static readonly'
      "CA1834", // Consider using 'StringBuilder.Append(char)' when applicable
      "CA1510", // Use 'ArgumentNullException.ThrowIfNull'
      "CA1062", // Validate arguments of public methods
      "CA1822", // Mark members as static
      "CA1859", // Use concrete types when possible for improved performance
    };

    // Act & Assert
    foreach (var ruleId in realRuleIds)
    {
      var result = RuleIdValidator.IsValidRuleId(ruleId);
      result.Should().BeTrue($"Rule ID '{ruleId}' should be valid");
    }
  }
}


