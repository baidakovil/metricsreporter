using System;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NUnit.Framework;
using MetricsReporter.Logging;

namespace MetricsReporter.Tests.Logging;

/// <summary>
/// Unit tests for <see cref="MinimalConsoleFormatter"/>.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class MinimalConsoleFormatterTests
{
  [Test]
  public void Constructor_WithNullOptions_ThrowsArgumentNullException()
  {
    // Act & Assert
    var act = () => new MinimalConsoleFormatter(null!);
    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("options");
  }

}

