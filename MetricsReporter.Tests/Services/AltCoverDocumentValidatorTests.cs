namespace MetricsReporter.Tests.Services;

using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Logging;
using MetricsReporter.Model;
using MetricsReporter.Processing;
using MetricsReporter.Services;

/// <summary>
/// Tests for <see cref="AltCoverDocumentValidator"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class AltCoverDocumentValidatorTests
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
    var logger = new TestLogger();

    // Act
    var result = AltCoverDocumentValidator.TryValidateUniqueSymbols(documents, logger);

    // Assert
    result.Should().BeTrue();
    logger.Errors.Should().BeEmpty();
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
    var logger = new TestLogger();

    // Act
    var result = AltCoverDocumentValidator.TryValidateUniqueSymbols(documents, logger);

    // Assert
    result.Should().BeFalse();
    logger.Errors.Should().ContainSingle()
        .Which.Should().Contain(duplicateSymbol);
  }

  private static ParsedMetricsDocument CreateDocument(string sourcePath, params ParsedCodeElement[] elements)
    => new()
    {
      SourcePath = sourcePath,
      Elements = new List<ParsedCodeElement>(elements)
    };

  private sealed class TestLogger : ILogger
  {
    public List<string> Errors { get; } = [];

    public void LogInformation(string message)
    {
      // Tests do not assert info-level logging.
    }

    public void LogError(string message, Exception? exception = null)
    {
      Errors.Add(message);
    }
  }
}



