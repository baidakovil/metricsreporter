namespace MetricsReporter.Tests.Aggregation;

using System.Collections.Generic;
using FluentAssertions;
using MetricsReporter.Aggregation;
using MetricsReporter.Model;
using NUnit.Framework;

[TestFixture]
[Category("Unit")]
public sealed class SarifBreakdownHelperTests
{
  // Ensures cloning a null dictionary yields null to avoid allocating unnecessary collections.
  [Test]
  public void Clone_NullSource_ReturnsNull()
  {
    // Act
    var result = SarifBreakdownHelper.Clone(null);

    // Assert
    result.Should().BeNull();
  }

  // Verifies empty dictionaries are treated as missing data rather than producing empty clones.
  [Test]
  public void Clone_EmptySource_ReturnsNull()
  {
    // Act
    var result = SarifBreakdownHelper.Clone(new Dictionary<string, SarifRuleBreakdownEntry>());

    // Assert
    result.Should().BeNull();
  }

  // Confirms null breakdown entries are materialized with defaults so callers receive usable containers.
  [Test]
  public void Clone_WithNullEntry_InitializesDefaults()
  {
    // Arrange
    var source = new Dictionary<string, SarifRuleBreakdownEntry?>
    {
      ["CA1000"] = null
    };

    // Act
    var result = SarifBreakdownHelper.Clone(source!);

    // Assert
    result.Should().NotBeNull();
    var nonNullResult = result!;
    nonNullResult.Should().ContainKey("CA1000");
    nonNullResult["CA1000"].Count.Should().Be(0);
    nonNullResult["CA1000"].Violations.Should().BeEmpty();
  }

  // Ensures cloning produces deep copies so subsequent mutations do not leak between instances.
  [Test]
  public void Clone_WithViolations_PerformsDeepCopy()
  {
    // Arrange
    var sourceEntry = CreateEntry(2, new SarifRuleViolationDetail { Message = "first", Uri = "file.cs", StartLine = 1, EndLine = 2 });
    var source = new Dictionary<string, SarifRuleBreakdownEntry>
    {
      ["CA1001"] = sourceEntry
    };

    // Act
    var result = SarifBreakdownHelper.Clone(source);

    // Assert
    result.Should().NotBeSameAs(source);
    var clonedEntry = result!.Should().ContainKey("CA1001").WhoseValue;
    clonedEntry.Should().NotBeSameAs(sourceEntry);
    clonedEntry.Count.Should().Be(2);
    clonedEntry.Violations.Should().HaveCount(1);
    clonedEntry.Violations[0].Should().NotBeSameAs(sourceEntry.Violations[0]);
    clonedEntry.Violations[0].Message.Should().Be("first");
  }

  // Validates merge clones existing data when new input is missing to avoid mutating the original dictionary.
  [Test]
  public void Merge_IncomingNull_ReturnsCloneOfExisting()
  {
    // Arrange
    var existing = new Dictionary<string, SarifRuleBreakdownEntry>
    {
      ["CA2000"] = CreateEntry(1)
    };

    // Act
    var result = SarifBreakdownHelper.Merge(existing, null);

    // Assert
    result.Should().NotBeSameAs(existing);
    var nonNullResult = result!;
    nonNullResult.Should().ContainKey("CA2000");
    nonNullResult["CA2000"].Should().NotBeSameAs(existing["CA2000"]);
    nonNullResult["CA2000"].Count.Should().Be(1);
  }

  // Confirms merge handles missing existing data by cloning the incoming dictionary.
  [Test]
  public void Merge_ExistingNull_ReturnsCloneOfIncoming()
  {
    // Arrange
    var incoming = new Dictionary<string, SarifRuleBreakdownEntry>
    {
      ["CA2001"] = CreateEntry(3, new SarifRuleViolationDetail { Message = "incoming" })
    };

    // Act
    var result = SarifBreakdownHelper.Merge(null, incoming);

    // Assert
    result.Should().NotBeSameAs(incoming);
    var nonNullResult = result!;
    nonNullResult.Should().ContainKey("CA2001");
    nonNullResult["CA2001"].Violations.Should().HaveCount(1);
    nonNullResult["CA2001"].Violations[0].Should().NotBeSameAs(incoming["CA2001"].Violations[0]);
  }

  // Ensures merge sums counts and concatenates violation details without sharing references.
  [Test]
  public void Merge_WithOverlappingRules_AggregatesCountsAndViolations()
  {
    // Arrange
    var existingEntry = CreateEntry(
        1,
        new SarifRuleViolationDetail { Message = "existing", Uri = "existing.cs", StartLine = 10, EndLine = 12 });
    var incomingEntry = CreateEntry(
        2,
        new SarifRuleViolationDetail { Message = "incoming1" },
        new SarifRuleViolationDetail { Message = "incoming2" });

    var existing = new Dictionary<string, SarifRuleBreakdownEntry> { ["CA2100"] = existingEntry };
    var incoming = new Dictionary<string, SarifRuleBreakdownEntry> { ["CA2100"] = incomingEntry };

    // Act
    var result = SarifBreakdownHelper.Merge(existing, incoming);

    // Assert
    var nonNullResult = result!;
    var mergedEntry = nonNullResult.Should().ContainKey("CA2100").WhoseValue;
    mergedEntry.Count.Should().Be(3);
    mergedEntry.Violations.Should().HaveCount(3);
    mergedEntry.Violations.Should().OnlyContain(v => v != existingEntry.Violations[0] && !incomingEntry.Violations.Contains(v));
  }

  private static SarifRuleBreakdownEntry CreateEntry(int count, params SarifRuleViolationDetail[] violations)
  {
    return new SarifRuleBreakdownEntry
    {
      Count = count,
      Violations = new List<SarifRuleViolationDetail>(violations)
    };
  }
}

