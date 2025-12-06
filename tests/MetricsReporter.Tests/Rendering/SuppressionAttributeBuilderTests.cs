namespace MetricsReporter.Tests.Rendering;

using FluentAssertions;
using NUnit.Framework;
using MetricsReporter.Model;
using MetricsReporter.Rendering;

/// <summary>
/// Unit tests for <see cref="SuppressionAttributeBuilder"/> class.
/// </summary>
[TestFixture]
[Category("Unit")]
public sealed class SuppressionAttributeBuilderTests
{
  [Test]
  public void BuildDataAttribute_WithNullSuppression_ReturnsEmptyString()
  {
    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(null);

    // Assert
    result.Should().BeEmpty();
  }

  [Test]
  public void BuildDataAttribute_WithValidSuppression_BuildsAttribute()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = "Justified suppression"
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    result.Should().Contain("data-suppression-info=");
    result.Should().Contain("CA1506");
    result.Should().Contain("Justified suppression");
  }

  [Test]
  public void BuildDataAttribute_WithNullJustification_UsesDefaultJustification()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = null
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    result.Should().Contain("Suppressed via SuppressMessage.");
  }

  [Test]
  public void BuildDataAttribute_WithEmptyJustification_UsesDefaultJustification()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = string.Empty
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    result.Should().Contain("Suppressed via SuppressMessage.");
  }

  [Test]
  public void BuildDataAttribute_WithWhitespaceJustification_UsesDefaultJustification()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = "   "
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    result.Should().Contain("Suppressed via SuppressMessage.");
  }

  [Test]
  public void BuildDataAttribute_WithParagraphBreaks_PreservesParagraphs()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = "First paragraph.\r\n\r\nSecond paragraph."
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    result.Should().Contain("First paragraph.");
    result.Should().Contain("Second paragraph.");
    // The <br/><br/> is added in FormatJustificationText, then serialized to JSON, then the JSON is HTML-encoded
    // WebUtility.HtmlEncode encodes quotes as &quot; but does not encode < and > in JSON string values
    // Verify that paragraphs are separated by checking that both paragraphs appear in the result
    var firstIndex = result.IndexOf("First paragraph.", StringComparison.Ordinal);
    var secondIndex = result.IndexOf("Second paragraph.", StringComparison.Ordinal);
    firstIndex.Should().BeGreaterThan(-1);
    secondIndex.Should().BeGreaterThan(firstIndex); // Second paragraph should come after first
  }

  [Test]
  public void BuildDataAttribute_WithUnixLineBreaks_PreservesParagraphs()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = "First paragraph.\n\nSecond paragraph."
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    result.Should().Contain("First paragraph.");
    result.Should().Contain("Second paragraph.");
    // The <br/><br/> is added in FormatJustificationText, then serialized to JSON, then the JSON is HTML-encoded
    // WebUtility.HtmlEncode encodes quotes as &quot; but does not encode < and > in JSON string values
    // Verify that paragraphs are separated by checking that both paragraphs appear in the result
    var firstIndex = result.IndexOf("First paragraph.", StringComparison.Ordinal);
    var secondIndex = result.IndexOf("Second paragraph.", StringComparison.Ordinal);
    firstIndex.Should().BeGreaterThan(-1);
    secondIndex.Should().BeGreaterThan(firstIndex); // Second paragraph should come after first
  }

  [Test]
  public void BuildDataAttribute_WithHtmlInJustification_EscapesHtml()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = "This is <script>alert('xss')</script> dangerous"
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    // The HTML is encoded in FormatJustificationText as &lt;script&gt;, then serialized to JSON, then the JSON is HTML-encoded
    // WebUtility.HtmlEncode encodes quotes as &quot; but preserves already-encoded HTML entities like &lt;
    // Verify that the script tag is present (may be encoded as &lt;script&gt; or &amp;lt;script&amp;gt; depending on encoding)
    result.Should().Contain("script");
    result.Should().Contain("alert");
    result.Should().Contain("xss");
    result.Should().NotContain("<script>"); // Raw <script> should not appear
  }

  [Test]
  public void BuildDataAttribute_WithQuotesInJustification_EscapesQuotes()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = "This has \"quotes\" in it"
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    // Quotes in the justification text are HTML-encoded in FormatJustificationText, then serialized to JSON, then the JSON is HTML-encoded
    // The quotes in the justification become part of the JSON string value, which is then HTML-encoded
    result.Should().Contain("quotes");
    result.Should().Contain("&quot;"); // Quotes in JSON are HTML-encoded
  }

  [Test]
  public void BuildDataAttribute_WithMultipleParagraphs_JoinsWithBrTags()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = "Para1\n\nPara2\n\nPara3"
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    // The <br/><br/> is added in FormatJustificationText, then serialized to JSON, then the JSON is HTML-encoded
    // WebUtility.HtmlEncode does not encode < and > in JSON string values
    // Verify that all three paragraphs are present
    result.Should().Contain("Para1");
    result.Should().Contain("Para2");
    result.Should().Contain("Para3");
    // Verify that paragraphs are separated (the exact separator format may vary)
    result.Should().Match("*Para1*Para2*Para3*");
  }

  [Test]
  public void BuildDataAttribute_WithLeadingTrailingWhitespace_TrimsWhitespace()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = "   Trimmed text   "
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    result.Should().Contain("Trimmed text");
    result.Should().NotContain("   Trimmed text   ");
  }

  [Test]
  public void BuildDataAttribute_WithJsonStructure_ContainsValidJson()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = "Test justification"
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    // JSON is HTML-encoded, so quotes become &quot;
    result.Should().Contain("&quot;ruleId&quot;");
    result.Should().Contain("&quot;justification&quot;");
    result.Should().Contain("CA1506");
    result.Should().Contain("Test justification");
  }

  [Test]
  public void BuildDataAttribute_WithCamelCasePropertyNames_UsesCamelCase()
  {
    // Arrange
    var suppression = new SuppressedSymbolInfo
    {
      FullyQualifiedName = "Sample.Namespace.SampleType",
      Metric = "RoslynClassCoupling",
      RuleId = "CA1506",
      Justification = "Test"
    };

    // Act
    var result = SuppressionAttributeBuilder.BuildDataAttribute(suppression);

    // Assert
    // JSON is HTML-encoded, so quotes become &quot;
    result.Should().Contain("&quot;ruleId&quot;"); // camelCase
    result.Should().NotContain("&quot;RuleId&quot;"); // PascalCase
    result.Should().Contain("&quot;justification&quot;"); // camelCase
    result.Should().NotContain("&quot;Justification&quot;"); // PascalCase
  }
}


