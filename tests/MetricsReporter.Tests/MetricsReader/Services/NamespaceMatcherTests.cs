namespace MetricsReporter.Tests.MetricsReader.Services;

using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.MetricsReader.Services;

/// <summary>
/// Unit tests for <see cref="NamespaceMatcher"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class NamespaceMatcherTests
{
  [Test]
  public void Matches_EmptyFilter_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services.SampleType", string.Empty);

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void Matches_WhitespaceFilter_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services.SampleType", "   ");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void Matches_NullFilter_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services.SampleType", null!);

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void Matches_ExactNamespaceMatch_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services", "Rca.Loader.Services");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void Matches_NamespaceWithChildType_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services.SampleType", "Rca.Loader.Services");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void Matches_NamespaceWithNestedType_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services+InnerType", "Rca.Loader.Services");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void Matches_NamespaceWithGenericType_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services.SampleType`1", "Rca.Loader.Services");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void Matches_DifferentNamespace_ReturnsFalse()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.UI.Services.SampleType", "Rca.Loader.Services");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void Matches_PartialMatch_ReturnsFalse()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.ServicesExtended.Type", "Rca.Loader.Services");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void Matches_NullFullyQualifiedName_ReturnsFalse()
  {
    // Act
    var result = NamespaceMatcher.Matches(null, "Rca.Loader.Services");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void Matches_EmptyFullyQualifiedName_ReturnsFalse()
  {
    // Act
    var result = NamespaceMatcher.Matches(string.Empty, "Rca.Loader.Services");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void Matches_WhitespaceFullyQualifiedName_ReturnsFalse()
  {
    // Act
    var result = NamespaceMatcher.Matches("   ", "Rca.Loader.Services");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void Matches_NamespacePrefix_ReturnsFalse()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader", "Rca.Loader.Services");

    // Assert
    result.Should().BeFalse("shorter name should not match longer filter");
  }

  [Test]
  public void Matches_RootNamespace_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services.Type", "Rca");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void Matches_ExactMatchWithDot_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader", "Rca.Loader");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void Matches_NestedTypeSeparator_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services+Inner", "Rca.Loader.Services");

    // Assert
    result.Should().BeTrue("+ separator should be recognized");
  }

  [Test]
  public void Matches_MemberSeparator_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services:Method", "Rca.Loader.Services");

    // Assert
    result.Should().BeTrue(": separator should be recognized");
  }

  [Test]
  public void Matches_CaseSensitiveMatch_ReturnsTrue()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services.Type", "Rca.Loader.Services");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void Matches_CaseMismatch_ReturnsFalse()
  {
    // Act
    var result = NamespaceMatcher.Matches("Rca.Loader.Services.Type", "rca.loader.services");

    // Assert
    result.Should().BeFalse("should be case sensitive");
  }
}


