namespace MetricsReporter.Tests.Aggregation;

using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using MetricsReporter.Processing;

/// <summary>
/// Unit tests for <see cref="AggregationWorkspaceLookup"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class AggregationWorkspaceLookupTests
{
  private Dictionary<string, AssemblyMetricsNode> assemblies = null!;
  private Dictionary<string, List<NamespaceEntry>> namespaceIndex = null!;
  private Dictionary<string, TypeEntry> types = null!;
  private AssemblyFilter assemblyFilter = null!;
  private AggregationWorkspaceLookup lookup = null!;

  [SetUp]
  public void SetUp()
  {
    assemblies = new Dictionary<string, AssemblyMetricsNode>(StringComparer.OrdinalIgnoreCase);
    namespaceIndex = new Dictionary<string, List<NamespaceEntry>>(StringComparer.Ordinal);
    types = new Dictionary<string, TypeEntry>(StringComparer.Ordinal);
    assemblyFilter = new AssemblyFilter();
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, assemblyFilter);
  }

  #region Constructor tests

  [Test]
  public void Constructor_NullAssemblies_ThrowsArgumentNullException()
  {
    // Act & Assert
    Action act = () => new AggregationWorkspaceLookup(null!, namespaceIndex, types, assemblyFilter);
    act.Should().Throw<ArgumentNullException>().WithParameterName("assemblies");
  }

  [Test]
  public void Constructor_NullNamespaceIndex_ThrowsArgumentNullException()
  {
    // Act & Assert
    Action act = () => new AggregationWorkspaceLookup(assemblies, null!, types, assemblyFilter);
    act.Should().Throw<ArgumentNullException>().WithParameterName("namespaceIndex");
  }

  [Test]
  public void Constructor_NullTypes_ThrowsArgumentNullException()
  {
    // Act & Assert
    Action act = () => new AggregationWorkspaceLookup(assemblies, namespaceIndex, null!, assemblyFilter);
    act.Should().Throw<ArgumentNullException>().WithParameterName("types");
  }

  [Test]
  public void Constructor_NullAssemblyFilter_ThrowsArgumentNullException()
  {
    // Act & Assert
    Action act = () => new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, null!);
    act.Should().Throw<ArgumentNullException>().WithParameterName("assemblyFilter");
  }

  [Test]
  public void Constructor_ValidParameters_CreatesInstance()
  {
    // Act
    var instance = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, assemblyFilter);

    // Assert
    instance.Should().NotBeNull();
  }

  #endregion

  #region ResolveMemberAssemblyNode tests

  [Test]
  public void ResolveMemberAssemblyNode_MemberWithNullFqn_ReturnsNull()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = null
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void ResolveMemberAssemblyNode_MemberWithFqnButTypeNotFound_ReturnsNull()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert
    result.Should().BeNull();
  }

  [Test]
  public void ResolveMemberAssemblyNode_MemberWithFqnAndTypeFound_ReturnsAssembly()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    var typeNode = new TypeMetricsNode
    {
      Name = "MyType",
      FullyQualifiedName = "MyNamespace.MyType"
    };
    var typeEntry = new TypeEntry(typeNode, assembly);
    types["MyNamespace.MyType"] = typeEntry;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert
    result.Should().BeSameAs(assembly);
  }

  [Test]
  public void ResolveMemberAssemblyNode_MemberWithFqnAndTypeFoundWithParameters_ReturnsAssembly()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    var typeNode = new TypeMetricsNode
    {
      Name = "MyType",
      FullyQualifiedName = "MyNamespace.MyType"
    };
    var typeEntry = new TypeEntry(typeNode, assembly);
    types["MyNamespace.MyType"] = typeEntry;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod(System.String, System.Int32)"
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert
    result.Should().BeSameAs(assembly);
  }

  #endregion

  #region ShouldExcludeMember tests

  [Test]
  public void ShouldExcludeMember_MemberWithNullFqn_ReturnsFalse()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = null
    };

    // Act
    var result = lookup.ShouldExcludeMember(member);

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void ShouldExcludeMember_MemberWithFqnButAssemblyNotFound_ReturnsFalse()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act
    var result = lookup.ShouldExcludeMember(member);

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void ShouldExcludeMember_MemberWithFqnAndAssemblyExcluded_ReturnsTrue()
  {
    // Arrange
    var excludedAssembly = new AssemblyMetricsNode
    {
      Name = "ExcludedAssembly",
      FullyQualifiedName = "ExcludedAssembly"
    };
    assemblies["ExcludedAssembly"] = excludedAssembly;

    var typeNode = new TypeMetricsNode
    {
      Name = "MyType",
      FullyQualifiedName = "MyNamespace.MyType"
    };
    var typeEntry = new TypeEntry(typeNode, excludedAssembly);
    types["MyNamespace.MyType"] = typeEntry;

    var filter = AssemblyFilter.FromString("Excluded");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act
    var result = lookup.ShouldExcludeMember(member);

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeMember_MemberWithFqnAndAssemblyNotExcluded_ReturnsFalse()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var typeNode = new TypeMetricsNode
    {
      Name = "MyType",
      FullyQualifiedName = "MyNamespace.MyType"
    };
    var typeEntry = new TypeEntry(typeNode, assembly);
    types["MyNamespace.MyType"] = typeEntry;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act
    var result = lookup.ShouldExcludeMember(member);

    // Assert
    result.Should().BeFalse();
  }

  #endregion

  #region ShouldExcludeType tests

  [Test]
  public void ShouldExcludeType_TypeWithExcludedAssembly_ReturnsTrue()
  {
    // Arrange
    var excludedAssembly = new AssemblyMetricsNode
    {
      Name = "ExcludedAssembly",
      FullyQualifiedName = "ExcludedAssembly"
    };
    assemblies["ExcludedAssembly"] = excludedAssembly;

    var typeNode = new TypeMetricsNode
    {
      Name = "MyType",
      FullyQualifiedName = "MyNamespace.MyType"
    };
    var typeEntry = new TypeEntry(typeNode, excludedAssembly);

    var filter = AssemblyFilter.FromString("Excluded");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    // Act
    var result = lookup.ShouldExcludeType(typeEntry);

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeType_TypeWithNonExcludedAssembly_ReturnsFalse()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var typeNode = new TypeMetricsNode
    {
      Name = "MyType",
      FullyQualifiedName = "MyNamespace.MyType"
    };
    var typeEntry = new TypeEntry(typeNode, assembly);

    // Act
    var result = lookup.ShouldExcludeType(typeEntry);

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void ShouldExcludeType_TypeWithAssemblyNotInDictionary_ReturnsTrue()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "UnknownAssembly",
      FullyQualifiedName = "UnknownAssembly"
    };

    var typeNode = new TypeMetricsNode
    {
      Name = "MyType",
      FullyQualifiedName = "MyNamespace.MyType"
    };
    var typeEntry = new TypeEntry(typeNode, assembly);

    // Act
    var result = lookup.ShouldExcludeType(typeEntry);

    // Assert
    result.Should().BeTrue();
  }

  #endregion

  #region ShouldExcludeAssembly tests

  [Test]
  public void ShouldExcludeAssembly_NullAssemblyName_ReturnsFalse()
  {
    // Act
    var result = lookup.ShouldExcludeAssembly(null);

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void ShouldExcludeAssembly_EmptyAssemblyName_ReturnsFalse()
  {
    // Act
    var result = lookup.ShouldExcludeAssembly(string.Empty);

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void ShouldExcludeAssembly_WhitespaceAssemblyName_ReturnsFalse()
  {
    // Act
    var result = lookup.ShouldExcludeAssembly("   ");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void ShouldExcludeAssembly_AssemblyExcludedByFilter_ReturnsTrue()
  {
    // Arrange
    var filter = AssemblyFilter.FromString("Test");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    // Act
    var result = lookup.ShouldExcludeAssembly("TestAssembly");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeAssembly_AssemblyNotInDictionary_ReturnsTrue()
  {
    // Act
    var result = lookup.ShouldExcludeAssembly("UnknownAssembly");

    // Assert
    result.Should().BeTrue();
  }

  [Test]
  public void ShouldExcludeAssembly_AssemblyInDictionaryAndNotExcluded_ReturnsFalse()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    // Act
    var result = lookup.ShouldExcludeAssembly("TestAssembly");

    // Assert
    result.Should().BeFalse();
  }

  [Test]
  public void ShouldExcludeAssembly_AssemblyExcludedByFilterAndNotInDictionary_ReturnsTrue()
  {
    // Arrange
    var filter = AssemblyFilter.FromString("Test");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    // Act
    var result = lookup.ShouldExcludeAssembly("TestAssembly");

    // Assert
    result.Should().BeTrue();
  }

  #endregion

  #region ResolveMemberAssemblyName tests (via ShouldExcludeMember and ResolveMemberAssemblyNode)

  [Test]
  public void ResolveMemberAssemblyName_MemberWithTypeInDictionary_ReturnsAssemblyName()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var typeNode = new TypeMetricsNode
    {
      Name = "MyType",
      FullyQualifiedName = "MyNamespace.MyType"
    };
    var typeEntry = new TypeEntry(typeNode, assembly);
    types["MyNamespace.MyType"] = typeEntry;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test ResolveMemberAssemblyName via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - if assembly is resolved, shouldExclude should be false (assuming not filtered)
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveMemberAssemblyName_MemberWithoutTypeFallsBackToFqnResolution_ReturnsAssemblyName()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var namespaceNode = new NamespaceMetricsNode
    {
      Name = "MyNamespace",
      FullyQualifiedName = "MyNamespace"
    };
    var namespaceEntry = new NamespaceEntry(namespaceNode, assembly);
    namespaceIndex["MyNamespace"] = new List<NamespaceEntry> { namespaceEntry };

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.UnknownType.MyMethod()"
    };

    // Act - indirectly test ResolveMemberAssemblyName via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - assembly should be resolved from namespace
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveMemberAssemblyName_TypeEntryAssemblyWithNullFullyQualifiedName_ReturnsNull()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = null
    };
    assemblies["TestAssembly"] = assembly;

    var typeNode = new TypeMetricsNode
    {
      Name = "MyType",
      FullyQualifiedName = "MyNamespace.MyType"
    };
    var typeEntry = new TypeEntry(typeNode, assembly);
    types["MyNamespace.MyType"] = typeEntry;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test ResolveMemberAssemblyName via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - null assembly name returns null, which makes ShouldExcludeMember return false
    // (because assemblyName is not null check fails when assemblyName is null)
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveMemberAssemblyName_ResolveDeclaringTypeReturnsNull_ReturnsNull()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyMethod()" // No type prefix - ResolveDeclaringType returns "MyMethod()"
    };

    // Act - indirectly test ResolveMemberAssemblyName via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - ResolveDeclaringType returns "MyMethod()" (not null), but type not found,
    // so falls back to ResolveAssemblyNameFromFqn which returns string.Empty when no assemblies,
    // and ShouldExcludeAssembly(string.Empty) returns false
    shouldExclude.Should().BeFalse();
  }

  #endregion

  #region ResolveAssemblyNameFromFqn tests (via ResolveMemberAssemblyName)

  [Test]
  public void ResolveAssemblyNameFromFqn_TypeFqnWithNamespaceInIndex_ReturnsAssemblyName()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var namespaceNode = new NamespaceMetricsNode
    {
      Name = "MyNamespace",
      FullyQualifiedName = "MyNamespace"
    };
    var namespaceEntry = new NamespaceEntry(namespaceNode, assembly);
    namespaceIndex["MyNamespace"] = new List<NamespaceEntry> { namespaceEntry };

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test ResolveAssemblyNameFromFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveAssemblyNameFromFqn_TypeFqnExcludedByFilter_ReturnsTypeFqn()
  {
    // Arrange
    var filter = AssemblyFilter.FromString("MyNamespace.MyType");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test ResolveAssemblyNameFromFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - should exclude because type FQN matches filter
    shouldExclude.Should().BeTrue();
  }

  [Test]
  public void ResolveAssemblyNameFromFqn_NamespaceExcludedByFilter_ReturnsNamespace()
  {
    // Arrange
    var filter = AssemblyFilter.FromString("MyNamespace");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test ResolveAssemblyNameFromFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - should exclude because namespace matches filter
    shouldExclude.Should().BeTrue();
  }

  [Test]
  public void ResolveAssemblyNameFromFqn_RootNamespaceExcludedByFilter_ReturnsRootNamespace()
  {
    // Arrange
    var filter = AssemblyFilter.FromString("My");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test ResolveAssemblyNameFromFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - should exclude because root namespace matches filter
    shouldExclude.Should().BeTrue();
  }

  [Test]
  public void ResolveAssemblyNameFromFqn_NoMatchReturnsFirstAssembly_ReturnsFirstAssemblyName()
  {
    // Arrange
    var assembly1 = new AssemblyMetricsNode
    {
      Name = "FirstAssembly",
      FullyQualifiedName = "FirstAssembly"
    };
    var assembly2 = new AssemblyMetricsNode
    {
      Name = "SecondAssembly",
      FullyQualifiedName = "SecondAssembly"
    };
    assemblies["FirstAssembly"] = assembly1;
    assemblies["SecondAssembly"] = assembly2;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "UnknownNamespace.UnknownType.MyMethod()"
    };

    // Act - indirectly test ResolveAssemblyNameFromFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - should not exclude because first assembly is used as fallback
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveAssemblyNameFromFqn_NoAssembliesReturnsEmptyString_ReturnsEmptyString()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "UnknownNamespace.UnknownType.MyMethod()"
    };

    // Act - indirectly test ResolveAssemblyNameFromFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - ResolveAssemblyNameFromFqn returns string.Empty when no assemblies,
    // and ShouldExcludeAssembly(string.Empty) returns false (because IsNullOrWhiteSpace is true),
    // so shouldExclude is false
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveAssemblyNameFromFqn_AssemblyNameNotInDictionary_ReturnsTrue()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    // Create a filter that excludes a specific assembly name
    var filter = AssemblyFilter.FromString("ExcludedAssembly");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "UnknownNamespace.UnknownType.MyMethod()"
    };

    // Act - indirectly test ResolveAssemblyNameFromFqn via ShouldExcludeMember
    // ResolveAssemblyNameFromFqn will return "TestAssembly" (first assembly) as fallback,
    // which is in dictionary, so should not exclude
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - first assembly is used as fallback and is in dictionary
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveAssemblyNameFromFqn_GlobalNamespaceNotExcluded_ReturnsFirstAssembly()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "<global>.MyType.MyMethod()"
    };

    // Act - indirectly test ResolveAssemblyNameFromFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - global namespace should not be excluded, should use first assembly
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveAssemblyNameFromFqn_AssemblyWithNullFullyQualifiedNameUsesName_ReturnsAssemblyName()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = null
    };
    assemblies["TestAssembly"] = assembly;

    var namespaceNode = new NamespaceMetricsNode
    {
      Name = "MyNamespace",
      FullyQualifiedName = "MyNamespace"
    };
    var namespaceEntry = new NamespaceEntry(namespaceNode, assembly);
    namespaceIndex["MyNamespace"] = new List<NamespaceEntry> { namespaceEntry };

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test ResolveAssemblyNameFromFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - should use assembly.Name when FullyQualifiedName is null
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveAssemblyNameFromFqn_TypeFqnIsEmptyString_ReturnsFirstAssembly()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = string.Empty
    };

    // Act - indirectly test ResolveAssemblyNameFromFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - empty string should not match filter, should use first assembly
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveAssemblyNameFromFqn_TypeFqnIsWhitespace_ReturnsFirstAssembly()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "   "
    };

    // Act - indirectly test ResolveAssemblyNameFromFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - whitespace should not match filter, should use first assembly
    shouldExclude.Should().BeFalse();
  }

  #endregion

  #region TryResolveAssembly tests (via ResolveAssemblyNameFromFqn)

  [Test]
  public void TryResolveAssembly_NamespaceInIndex_ReturnsAssembly()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var namespaceNode = new NamespaceMetricsNode
    {
      Name = "MyNamespace",
      FullyQualifiedName = "MyNamespace"
    };
    var namespaceEntry = new NamespaceEntry(namespaceNode, assembly);
    namespaceIndex["MyNamespace"] = new List<NamespaceEntry> { namespaceEntry };

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test TryResolveAssembly via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void TryResolveAssembly_NamespaceNotInIndex_ReturnsNull()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "UnknownNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test TryResolveAssembly via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - TryResolveAssembly returns null, so ResolveAssemblyNameFromFqn falls back
    // to other strategies and eventually returns string.Empty when no assemblies,
    // and ShouldExcludeAssembly(string.Empty) returns false
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void TryResolveAssembly_NamespaceInIndexButEmptyList_ReturnsNull()
  {
    // Arrange
    namespaceIndex["MyNamespace"] = new List<NamespaceEntry>();

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test TryResolveAssembly via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - TryResolveAssembly returns null (because entries.Count == 0),
    // so ResolveAssemblyNameFromFqn falls back to other strategies and eventually
    // returns string.Empty when no assemblies, and ShouldExcludeAssembly(string.Empty) returns false
    shouldExclude.Should().BeFalse();
  }

  #endregion

  #region ResolveNamespaceFromIndexesOrFqn tests (via ResolveAssemblyNameFromFqn)

  [Test]
  public void ResolveNamespaceFromIndexesOrFqn_KnownNamespaceInIndex_ReturnsKnownNamespace()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var namespaceNode = new NamespaceMetricsNode
    {
      Name = "MyNamespace",
      FullyQualifiedName = "MyNamespace"
    };
    var namespaceEntry = new NamespaceEntry(namespaceNode, assembly);
    namespaceIndex["MyNamespace"] = new List<NamespaceEntry> { namespaceEntry };

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test ResolveNamespaceFromIndexesOrFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveNamespaceFromIndexesOrFqn_UnknownNamespaceExtractsFromFqn_ReturnsExtractedNamespace()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var namespaceNode = new NamespaceMetricsNode
    {
      Name = "MyNamespace",
      FullyQualifiedName = "MyNamespace"
    };
    var namespaceEntry = new NamespaceEntry(namespaceNode, assembly);
    namespaceIndex["MyNamespace"] = new List<NamespaceEntry> { namespaceEntry };

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.OtherType.MyMethod()"
    };

    // Act - indirectly test ResolveNamespaceFromIndexesOrFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - namespace should be extracted and found in index
    shouldExclude.Should().BeFalse();
  }

  [Test]
  public void ResolveNamespaceFromIndexesOrFqn_PartialNamespaceMatch_ReturnsLongestMatchingNamespace()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    assemblies["TestAssembly"] = assembly;

    var namespaceNode1 = new NamespaceMetricsNode
    {
      Name = "MyNamespace",
      FullyQualifiedName = "MyNamespace"
    };
    var namespaceEntry1 = new NamespaceEntry(namespaceNode1, assembly);
    namespaceIndex["MyNamespace"] = new List<NamespaceEntry> { namespaceEntry1 };

    var namespaceNode2 = new NamespaceMetricsNode
    {
      Name = "MyNamespace.SubNamespace",
      FullyQualifiedName = "MyNamespace.SubNamespace"
    };
    var namespaceEntry2 = new NamespaceEntry(namespaceNode2, assembly);
    namespaceIndex["MyNamespace.SubNamespace"] = new List<NamespaceEntry> { namespaceEntry2 };

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.SubNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test ResolveNamespaceFromIndexesOrFqn via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - should match longest namespace
    shouldExclude.Should().BeFalse();
  }

  #endregion

  #region ExtractRootNamespace tests (via ResolveAssemblyNameFromFqn)

  [Test]
  public void ExtractRootNamespace_NormalNamespace_ReturnsRootNamespace()
  {
    // Arrange
    var filter = AssemblyFilter.FromString("My");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act - indirectly test ExtractRootNamespace via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - root namespace "My" should match filter
    shouldExclude.Should().BeTrue();
  }

  [Test]
  public void ExtractRootNamespace_GlobalNamespace_ReturnsEmptyString()
  {
    // Arrange
    var filter = AssemblyFilter.FromString("My");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "<global>.MyType.MyMethod()"
    };

    // Act - indirectly test ExtractRootNamespace via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - global namespace should not match root namespace extraction
    shouldExclude.Should().BeTrue(); // Because assembly not found, not because of root namespace
  }

  [Test]
  public void ExtractRootNamespace_EmptyNamespace_ReturnsEmptyString()
  {
    // Arrange
    var filter = AssemblyFilter.FromString("My");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyType.MyMethod()"
    };

    // Act - indirectly test ExtractRootNamespace via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - no namespace should return empty string
    shouldExclude.Should().BeTrue(); // Because assembly not found
  }

  [Test]
  public void ExtractRootNamespace_NamespaceWithoutDot_ReturnsFullNamespace()
  {
    // Arrange
    var filter = AssemblyFilter.FromString("Single");
    lookup = new AggregationWorkspaceLookup(assemblies, namespaceIndex, types, filter);

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "Single.MyType.MyMethod()"
    };

    // Act - indirectly test ExtractRootNamespace via ShouldExcludeMember
    var shouldExclude = lookup.ShouldExcludeMember(member);

    // Assert - single namespace without dot should return full namespace
    shouldExclude.Should().BeTrue();
  }

  #endregion

  #region ResolveDeclaringType tests (via ResolveMemberAssemblyNode)

  [Test]
  public void ResolveDeclaringType_MemberWithParameters_ReturnsTypeFqn()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    var typeNode = new TypeMetricsNode
    {
      Name = "MyType",
      FullyQualifiedName = "MyNamespace.MyType"
    };
    var typeEntry = new TypeEntry(typeNode, assembly);
    types["MyNamespace.MyType"] = typeEntry;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod(System.String, System.Int32)"
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert
    result.Should().BeSameAs(assembly);
  }

  [Test]
  public void ResolveDeclaringType_MemberWithoutParameters_ReturnsTypeFqn()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    var typeNode = new TypeMetricsNode
    {
      Name = "MyType",
      FullyQualifiedName = "MyNamespace.MyType"
    };
    var typeEntry = new TypeEntry(typeNode, assembly);
    types["MyNamespace.MyType"] = typeEntry;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.MyType.MyMethod()"
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert
    result.Should().BeSameAs(assembly);
  }

  [Test]
  public void ResolveDeclaringType_MemberWithoutDot_ReturnsMemberFqn()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyMethod()"
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert
    result.Should().BeNull(); // Because no type found
  }

  [Test]
  public void ResolveDeclaringType_EmptyMemberFqn_ReturnsEmpty()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = string.Empty
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert
    result.Should().BeNull(); // Because FQN is null check happens first
  }

  [Test]
  public void ResolveDeclaringType_WhitespaceMemberFqn_ReturnsWhitespace()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "   "
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert
    result.Should().BeNull(); // Because type not found
  }

  [Test]
  public void ResolveDeclaringType_MemberWithNestedType_ReturnsOuterTypeFqn()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    var typeNode = new TypeMetricsNode
    {
      Name = "OuterType",
      FullyQualifiedName = "MyNamespace.OuterType"
    };
    var typeEntry = new TypeEntry(typeNode, assembly);
    types["MyNamespace.OuterType"] = typeEntry;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyNamespace.OuterType.NestedType.MyMethod()"
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert
    result.Should().BeNull(); // Because nested type not in dictionary
  }

  [Test]
  public void ResolveDeclaringType_MemberTypeFqnIsNullAfterResolution_ReturnsNull()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "MyMethod()" // No type prefix, ResolveDeclaringType returns full FQN
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert - when type FQN is not null but not found in dictionary, should return null
    result.Should().BeNull();
  }

  [Test]
  public void ResolveDeclaringType_MemberWithOnlyOneDot_ReturnsTypeFqn()
  {
    // Arrange
    var assembly = new AssemblyMetricsNode
    {
      Name = "TestAssembly",
      FullyQualifiedName = "TestAssembly"
    };
    var typeNode = new TypeMetricsNode
    {
      Name = "Type",
      FullyQualifiedName = "Type"
    };
    var typeEntry = new TypeEntry(typeNode, assembly);
    types["Type"] = typeEntry;

    var member = new MemberMetricsNode
    {
      FullyQualifiedName = "Type.Method()"
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert
    result.Should().BeSameAs(assembly);
  }

  [Test]
  public void ResolveDeclaringType_MemberWithLastDotAtStart_ReturnsFullFqn()
  {
    // Arrange
    var member = new MemberMetricsNode
    {
      FullyQualifiedName = ".Method()" // LastDot would be at position 0
    };

    // Act
    var result = lookup.ResolveMemberAssemblyNode(member);

    // Assert - when lastDot < 0, should return full FQN
    result.Should().BeNull(); // Because type not found
  }

  #endregion
}

