namespace MetricsReporter.Tests.Processing;

using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Processing;

/// <summary>
/// Unit tests for <see cref="TypeFilter"/> with wildcard patterns.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class TypeFilterTests
{
  [Test]
  public void FromString_WithWildcardPatterns_ExcludesCompilerGeneratedTypes()
  {
    // Arrange
    const string patterns = "*<>c*;*__DisplayClass*";

    // Act
    var filter = TypeFilter.FromString(patterns);

    // Assert - types containing the specified substrings should be excluded
    filter.ShouldExcludeType("Namespace.Type+<>c").Should().BeTrue();
    filter.ShouldExcludeType("Namespace.Type+<>c__DisplayClass1_0").Should().BeTrue();
    filter.ShouldExcludeType("Namespace.Type+Outer__DisplayClass0_1").Should().BeTrue();

    // Normal types should not be excluded
    filter.ShouldExcludeType("Namespace.Type").Should().BeFalse();
    filter.ShouldExcludeType("Namespace.Type+Helper").Should().BeFalse();
  }

  [Test]
  public void FromString_HandlesNullAndEmpty_AsNoExclusions()
  {
    // Act
    var filter1 = TypeFilter.FromString(null);
    var filter2 = TypeFilter.FromString(string.Empty);
    var filter3 = TypeFilter.FromString("   ");

    // Assert
    filter1.ShouldExcludeType("Namespace.Type+<>c").Should().BeFalse();
    filter2.ShouldExcludeType("Namespace.Type+__DisplayClass1_0").Should().BeFalse();
    filter3.ShouldExcludeType("Namespace.Type").Should().BeFalse();
  }
}



