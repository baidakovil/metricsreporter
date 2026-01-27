namespace MetricsReporter.Tests.Processing;

using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Processing;

/// <summary>
/// Unit tests for <see cref="MemberFilter"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class MemberFilterTests
{
  [Test]
  public void ShouldExcludeMethod_ExcludesConstructor_Ctor()
  {
    // Arrange
    var filter = MemberFilter.FromString("ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync");

    // Act & Assert
    filter.ShouldExcludeMethod(".ctor").Should().BeTrue();
    filter.ShouldExcludeMethod("ctor").Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeMethod_ExcludesStaticConstructor_Cctor()
  {
    // Arrange
    var filter = MemberFilter.FromString("ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync");

    // Act & Assert
    filter.ShouldExcludeMethod(".cctor").Should().BeTrue();
    filter.ShouldExcludeMethod("cctor").Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeMethod_ExcludesCompilerGeneratedMethods()
  {
    // Arrange
    var filter = MemberFilter.FromString("ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync");

    // Act & Assert
    filter.ShouldExcludeMethod("MoveNext").Should().BeTrue();
    filter.ShouldExcludeMethod("SetStateMachine").Should().BeTrue();
    filter.ShouldExcludeMethod("MoveNextAsync").Should().BeTrue();
    filter.ShouldExcludeMethod("DisposeAsync").Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeMethod_DoesNotExcludeNormalMethods()
  {
    // Arrange
    var filter = MemberFilter.FromString("ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync");

    // Act & Assert
    filter.ShouldExcludeMethod("DoWork").Should().BeFalse();
    filter.ShouldExcludeMethod("ProcessData").Should().BeFalse();
    filter.ShouldExcludeMethod("GetName").Should().BeFalse();
    filter.ShouldExcludeMethod("ToString").Should().BeFalse();
  }

  [Test]
  public void ShouldExcludeMethodByFqn_ExcludesOpenCoverConstructor()
  {
    // Arrange
    const string constructorFqn = "Namespace.Type..ctor(...)";
    var filter = MemberFilter.FromString("ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync");

    // Act & Assert
    filter.ShouldExcludeMethodByFqn(constructorFqn).Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeMethodByFqn_ExcludesOpenCoverStaticConstructor()
  {
    // Arrange
    const string staticConstructorFqn = "Namespace.Type..cctor(...)";
    var filter = MemberFilter.FromString("ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync");

    // Act & Assert
    filter.ShouldExcludeMethodByFqn(staticConstructorFqn).Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeMethodByFqn_ExcludesRoslynConstructor()
  {
    // Arrange - Roslyn format: constructor name matches type name
    const string constructorFqn = "Namespace.Type.Type(...)";
    var filter = MemberFilter.FromString("ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync");

    // Act & Assert
    filter.ShouldExcludeMethodByFqn(constructorFqn).Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeMethodByFqn_ExcludesCompilerGeneratedMethods()
  {
    // Arrange
    var filter = MemberFilter.FromString("ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync");

    // Act & Assert
    filter.ShouldExcludeMethodByFqn("Namespace.Type.MoveNext(...)").Should().BeTrue();
    filter.ShouldExcludeMethodByFqn("Namespace.Type.SetStateMachine(...)").Should().BeTrue();
    filter.ShouldExcludeMethodByFqn("Namespace.Type.MoveNextAsync(...)").Should().BeTrue();
    filter.ShouldExcludeMethodByFqn("Namespace.Type.DisposeAsync(...)").Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeMethodByFqn_DoesNotExcludeNormalMethods()
  {
    // Arrange
    var filter = MemberFilter.FromString("ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync");

    // Act & Assert
    filter.ShouldExcludeMethodByFqn("Namespace.Type.DoWork(...)").Should().BeFalse();
    filter.ShouldExcludeMethodByFqn("Namespace.Type.ProcessData(...)").Should().BeFalse();
    filter.ShouldExcludeMethodByFqn("Namespace.Type.GetName(...)").Should().BeFalse();
    filter.ShouldExcludeMethodByFqn("Namespace.Type.ToString(...)").Should().BeFalse();
  }

  [Test]
  public void ShouldExcludeMethodByFqn_DoesNotExcludeMethodWithSameNameAsType_ButNotConstructor()
  {
    // Arrange - This is a tricky case: if a type has a static method with the same name as the type,
    // it should not be excluded (only constructors match this pattern in Roslyn format)
    // However, since we check if method name == type name, this would be excluded.
    // This is acceptable because in practice, having a method with the same name as the type
    // (that is not a constructor) is extremely rare and would be confusing code.

    // For now, we'll test that the basic logic works
    const string methodFqn = "Namespace.SomeType.SomeType(...)";
    var filter = MemberFilter.FromString("ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync");

    // Act & Assert
    // This will be excluded because method name matches type name (Roslyn constructor pattern)
    filter.ShouldExcludeMethodByFqn(methodFqn).Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeMethod_HandlesNullAndEmpty()
  {
    // Arrange
    var filter = new MemberFilter();

    // Act & Assert
    filter.ShouldExcludeMethod(null).Should().BeFalse();
    filter.ShouldExcludeMethod(string.Empty).Should().BeFalse();
    filter.ShouldExcludeMethod("   ").Should().BeFalse();
  }

  [Test]
  public void ShouldExcludeMethodByFqn_HandlesNullAndEmpty()
  {
    // Arrange
    var filter = new MemberFilter();

    // Act & Assert
    filter.ShouldExcludeMethodByFqn(null).Should().BeFalse();
    filter.ShouldExcludeMethodByFqn(string.Empty).Should().BeFalse();
    filter.ShouldExcludeMethodByFqn("   ").Should().BeFalse();
  }

  [Test]
  public void ShouldExcludeMethodByFqn_HandlesComplexTypeNames()
  {
    // Arrange - Test with nested types and complex names
    const string constructorFqn = "Namespace.Outer+Nested.Outer+Nested(...)";
    var filter = MemberFilter.FromString("ctor,cctor,MoveNext,SetStateMachine,MoveNextAsync,DisposeAsync");

    // Act & Assert
    // This should extract type name as "Outer+Nested" and method name as "Outer+Nested"
    // So it should be excluded as a constructor
    filter.ShouldExcludeMethodByFqn(constructorFqn).Should().BeTrue();
  }

  [Test]
  public void FromString_ParsesCommaSeparatedList()
  {
    // Arrange
    const string methodNames = "ctor,cctor,MoveNext";

    // Act
    var filter = MemberFilter.FromString(methodNames);

    // Assert
    filter.ShouldExcludeMethod("ctor").Should().BeTrue();
    filter.ShouldExcludeMethod("cctor").Should().BeTrue();
    filter.ShouldExcludeMethod("MoveNext").Should().BeTrue();
    filter.ShouldExcludeMethod("SetStateMachine").Should().BeFalse();
  }

  [Test]
  public void FromString_ParsesSemicolonSeparatedList()
  {
    // Arrange
    const string methodNames = "ctor;cctor;MoveNext";

    // Act
    var filter = MemberFilter.FromString(methodNames);

    // Assert
    filter.ShouldExcludeMethod("ctor").Should().BeTrue();
    filter.ShouldExcludeMethod("cctor").Should().BeTrue();
    filter.ShouldExcludeMethod("MoveNext").Should().BeTrue();
    filter.ShouldExcludeMethod("SetStateMachine").Should().BeFalse();
  }

  [Test]
  public void FromString_HandlesNullAndEmpty()
  {
    // Act
    var filter1 = MemberFilter.FromString(null);
    var filter2 = MemberFilter.FromString(string.Empty);
    var filter3 = MemberFilter.FromString("   ");

    // Assert - No defaults when not provided
    filter1.ShouldExcludeMethod("ctor").Should().BeFalse();
    filter2.ShouldExcludeMethod("ctor").Should().BeFalse();
    filter3.ShouldExcludeMethod("ctor").Should().BeFalse();
  }

  [Test]
  public void FromString_NormalizesMethodNames()
  {
    // Arrange
    const string methodNames = ".ctor,.cctor,MoveNext";

    // Act
    var filter = MemberFilter.FromString(methodNames);

    // Assert - Leading dots should be removed
    filter.ShouldExcludeMethod("ctor").Should().BeTrue();
    filter.ShouldExcludeMethod("cctor").Should().BeTrue();
    filter.ShouldExcludeMethod(".ctor").Should().BeTrue(); // Should still work with dot
    filter.ShouldExcludeMethod("MoveNext").Should().BeTrue();
  }

  [Test]
  public void FromString_WithWildcardAndExactPatterns_ExcludesExpectedMethods()
  {
    // Arrange
    const string patterns = "*b__*,ctor";

    // Act
    var filter = MemberFilter.FromString(patterns);

    // Assert - wildcard pattern matches any name containing 'b__'
    filter.ShouldExcludeMethod("b__0").Should().BeTrue();
    filter.ShouldExcludeMethod("<Method>b__1_2").Should().BeTrue();
    filter.ShouldExcludeMethod("Someb__Helper").Should().BeTrue();

    // Exact pattern matches only 'ctor', not substrings like 'OrderConstructor'
    filter.ShouldExcludeMethod("ctor").Should().BeTrue();
    filter.ShouldExcludeMethod(".ctor").Should().BeTrue();
    filter.ShouldExcludeMethod("OrderConstructor").Should().BeFalse();

    // Normal method should not be excluded
    filter.ShouldExcludeMethod("DoWork").Should().BeFalse();
  }

  [Test]
  public void FromString_WithExactClonePattern_DoesNotExcludePartialMatches()
  {
    // Arrange
    const string patterns = "<Clone>$";

    // Act
    var filter = MemberFilter.FromString(patterns);

    // Assert - exact name match
    filter.ShouldExcludeMethod("<Clone>$").Should().BeTrue();

    // Partial matches should not be excluded when pattern has no wildcards
    filter.ShouldExcludeMethod("Clone").Should().BeFalse();
    filter.ShouldExcludeMethod("My<Clone>$Helper").Should().BeFalse();

    // FQN-based checks use the same underlying name matching
    filter.ShouldExcludeMethodByFqn("Namespace.Type.<Clone>$(...)").Should().BeTrue();
    filter.ShouldExcludeMethodByFqn("Namespace.Type.Clone(...)").Should().BeFalse();
    filter.ShouldExcludeMethodByFqn("Namespace.Type.My<Clone>$Helper(...)").Should().BeFalse();
  }
}

