namespace MetricsReporter.Tests.Services;

using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using MetricsReporter.Services;
using MetricsReporter.Tests.TestHelpers;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tests for <see cref="OpenCoverDocumentValidator"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class OpenCoverDocumentValidatorTests
{
  [Test]
  public void TryValidateUniqueSymbols_WithDistinctSymbols_ReturnsTrue()
  {
    // Arrange
    var documents = new List<ParsedMetricsDocument>
    {
      CreateDocument("coverage-one.xml", new ParsedCodeElement(CodeElementKind.Type, "TypeA", "Namespace.TypeA")),
      CreateDocument("coverage-two.xml", new ParsedCodeElement(CodeElementKind.Member, "MethodB", "Namespace.TypeB.MethodB(...)"))
    };
    var logger = new TestLogger<OpenCoverDocumentValidatorTests>();

    // Act
    var result = OpenCoverDocumentValidator.TryValidateUniqueSymbols(documents, logger);

    // Assert
    result.Should().BeTrue();
    logger.Entries.Should().BeEmpty();
  }

  [Test]
  public void TryValidateUniqueSymbols_WithDuplicateSymbolsAcrossFiles_ReturnsFalse()
  {
    // Arrange
    var duplicateSymbol = "Namespace.TypeA.MethodA(...)";
    var documents = new List<ParsedMetricsDocument>
    {
      CreateDocument("coverage-one.xml", new ParsedCodeElement(CodeElementKind.Member, "MethodA", duplicateSymbol)),
      CreateDocument("coverage-two.xml", new ParsedCodeElement(CodeElementKind.Member, "MethodA", duplicateSymbol))
    };
    var logger = new TestLogger<OpenCoverDocumentValidatorTests>();

    // Act
    var result = OpenCoverDocumentValidator.TryValidateUniqueSymbols(documents, logger);

    // Assert
    result.Should().BeFalse();
    logger.Entries.Should().ContainSingle(entry =>
      entry.Level == LogLevel.Error &&
      entry.Message.Contains(duplicateSymbol));
  }

  [Test]
  public void TryValidateUniqueSymbols_DuplicateWithinSameDocument_Ignored()
  {
    // Arrange
    var duplicateSymbol = "Namespace.TypeA.MethodA(...)";
    var documents = new List<ParsedMetricsDocument>
    {
      CreateDocument("coverage-one.xml",
        new ParsedCodeElement(CodeElementKind.Member, "MethodA", duplicateSymbol),
        new ParsedCodeElement(CodeElementKind.Member, "MethodA", duplicateSymbol))
    };
    var logger = new TestLogger<OpenCoverDocumentValidatorTests>();

    // Act
    var result = OpenCoverDocumentValidator.TryValidateUniqueSymbols(documents, logger);

    // Assert
    result.Should().BeTrue();
    logger.Entries.Should().BeEmpty();
  }

  [Test]
  public void TryValidateUniqueSymbols_NonOpenCoverSymbol_Ignored()
  {
    // Arrange
    var documents = new List<ParsedMetricsDocument>
    {
      CreateDocument("coverage-one.xml", new ParsedCodeElement(CodeElementKind.Namespace, "NamespaceA", "NamespaceA"))
    };
    var logger = new TestLogger<OpenCoverDocumentValidatorTests>();

    // Act
    var result = OpenCoverDocumentValidator.TryValidateUniqueSymbols(documents, logger);

    // Assert
    result.Should().BeTrue();
    logger.Entries.Should().BeEmpty();
  }

  [Test]
  public void TryValidateUniqueSymbols_MissingSourcePath_UsesFallbackDocumentIdInError()
  {
    // Arrange
    var duplicateSymbol = "Namespace.TypeA";
    var documents = new List<ParsedMetricsDocument>
    {
      CreateDocument(string.Empty, new ParsedCodeElement(CodeElementKind.Type, "TypeA", duplicateSymbol)),
      CreateDocument("coverage-two.xml", new ParsedCodeElement(CodeElementKind.Type, "TypeA", duplicateSymbol))
    };
    var logger = new TestLogger<OpenCoverDocumentValidatorTests>();

    // Act
    var result = OpenCoverDocumentValidator.TryValidateUniqueSymbols(documents, logger);

    // Assert
    result.Should().BeFalse();
    logger.Entries.Should().ContainSingle(entry =>
      entry.Level == LogLevel.Error &&
      entry.Message.Contains("OpenCoverDocument#1") &&
      entry.Message.Contains("coverage-two.xml"));
  }

  [Test]
  public void TryValidateUniqueSymbols_EmptyFullyQualifiedName_Ignored()
  {
    // Arrange
    var documents = new List<ParsedMetricsDocument>
    {
      CreateDocument("coverage-one.xml", new ParsedCodeElement(CodeElementKind.Member, "MethodA", "   "))
    };
    var logger = new TestLogger<OpenCoverDocumentValidatorTests>();

    // Act
    var result = OpenCoverDocumentValidator.TryValidateUniqueSymbols(documents, logger);

    // Assert
    result.Should().BeTrue();
    logger.Entries.Should().BeEmpty();
  }

  private static ParsedMetricsDocument CreateDocument(string? sourcePath, params ParsedCodeElement[] elements)
    => new()
    {
      SourcePath = sourcePath ?? string.Empty,
      Elements = new List<ParsedCodeElement>(elements)
    };
}
