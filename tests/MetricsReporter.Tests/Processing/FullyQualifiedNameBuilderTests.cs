namespace MetricsReporter.Tests.Processing;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Processing;
/// <summary>
/// Unit tests for <see cref="FullyQualifiedNameBuilder"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class FullyQualifiedNameBuilderTests
{
  [Test]
  public void BuildTypeFqn_SingleNamespaceAndType_ReturnsCorrectFqn()
  {
    // Arrange
    var builder = new FullyQualifiedNameBuilder();
    builder.PushNamespace("Sample.Namespace");
    builder.PushType("SampleType");
    // Act
    var result = builder.BuildTypeFqn();
    // Assert
    result.Should().Be("Sample.Namespace.SampleType");
  }
  [Test]
  public void BuildTypeFqn_NestedNamespaces_ReturnsCorrectFqn()
  {
    // Arrange
    var builder = new FullyQualifiedNameBuilder();
    builder.PushNamespace("Sample");
    builder.PushNamespace("Namespace");
    builder.PushType("SampleType");
    // Act
    var result = builder.BuildTypeFqn();
    // Assert
    result.Should().Be("Sample.Namespace.SampleType");
  }
  [Test]
  public void BuildTypeFqn_NestedTypes_ReturnsCorrectFqn()
  {
    // Arrange
    var builder = new FullyQualifiedNameBuilder();
    builder.PushNamespace("Sample.Namespace");
    builder.PushType("OuterType");
    builder.PushType("InnerType");
    // Act
    var result = builder.BuildTypeFqn();
    // Assert
    result.Should().Be("Sample.Namespace.OuterType.InnerType");
  }
  [Test]
  public void BuildTypeFqn_NoType_ReturnsNull()
  {
    // Arrange
    var builder = new FullyQualifiedNameBuilder();
    builder.PushNamespace("Sample.Namespace");
    // Act
    var result = builder.BuildTypeFqn();
    // Assert
    result.Should().BeNull();
  }
  [Test]
  public void BuildTypeFqn_NoNamespace_ReturnsTypeNameOnly()
  {
    // Arrange
    var builder = new FullyQualifiedNameBuilder();
    builder.PushType("SampleType");
    // Act
    var result = builder.BuildTypeFqn();
    // Assert
    result.Should().Be("SampleType");
  }
  [Test]
  public void BuildMemberFqn_SimpleMethod_ReturnsNormalizedFqn()
  {
    // Arrange
    var builder = new FullyQualifiedNameBuilder();
    builder.PushNamespace("Sample.Namespace");
    builder.PushType("SampleType");
    // Act
    var result = builder.BuildMemberFqn("TestMethod");
    // Assert
    result.Should().Be("Sample.Namespace.SampleType.TestMethod(...)");
  }
  [Test]
  public void BuildMemberFqn_NoType_ReturnsNull()
  {
    // Arrange
    var builder = new FullyQualifiedNameBuilder();
    builder.PushNamespace("Sample.Namespace");
    // Act
    var result = builder.BuildMemberFqn("TestMethod");
    // Assert
    result.Should().BeNull();
  }
  [Test]
  public void BuildPropertyFqn_SimpleProperty_ReturnsCorrectFqn()
  {
    // Arrange
    var builder = new FullyQualifiedNameBuilder();
    builder.PushNamespace("Sample.Namespace");
    builder.PushType("SampleType");
    // Act
    var result = builder.BuildPropertyFqn("TestProperty");
    // Assert
    result.Should().Be("Sample.Namespace.SampleType.TestProperty");
  }
  [Test]
  public void BuildPropertyFqn_NoType_ReturnsNull()
  {
    // Arrange
    var builder = new FullyQualifiedNameBuilder();
    builder.PushNamespace("Sample.Namespace");
    // Act
    var result = builder.BuildPropertyFqn("TestProperty");
    // Assert
    result.Should().BeNull();
  }
  [Test]
  public void PushAndPopNamespace_StackBehavior_WorksCorrectly()
  {
    // Arrange
    var builder = new FullyQualifiedNameBuilder();
    builder.PushNamespace("Outer");
    builder.PushNamespace("Inner");
    builder.PushType("Type");
    // Act
    var beforePop = builder.BuildTypeFqn();
    builder.PopNamespace();
    var afterPop = builder.BuildTypeFqn();
    // Assert
    beforePop.Should().Be("Outer.Inner.Type");
    afterPop.Should().Be("Outer.Type");
  }
  [Test]
  public void PushAndPopType_StackBehavior_WorksCorrectly()
  {
    // Arrange
    var builder = new FullyQualifiedNameBuilder();
    builder.PushNamespace("Sample.Namespace");
    builder.PushType("OuterType");
    builder.PushType("InnerType");
    // Act
    var beforePop = builder.BuildTypeFqn();
    builder.PopType();
    var afterPop = builder.BuildTypeFqn();
    // Assert
    beforePop.Should().Be("Sample.Namespace.OuterType.InnerType");
    afterPop.Should().Be("Sample.Namespace.OuterType");
  }
}






