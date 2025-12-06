namespace MetricsReporter.Tests.Serialization;

using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Serialization;

/// <summary>
/// Verifies JSON serialization options reused across the toolset.
/// </summary>
[TestFixture]
[Category("Unit")]
internal sealed class JsonSerializerOptionsFactoryTests
{
  [Test]
  public void Create_DoesNotEscapeAsciiQuotes()
  {
    // Arrange
    var options = JsonSerializerOptionsFactory.Create();
    var payload = new { message = "'RunAsync' is coupled with '36' different types." };

    // Act
    var json = JsonSerializer.Serialize(payload, options);

    // Assert
    json.Should().Contain("'RunAsync'");
    json.Should().NotContain("\\u0027");
  }
}



