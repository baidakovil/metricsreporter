namespace MetricsReporter.Configuration;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using MetricsReporter.Model;
using MetricsReporter.Serialization;

/// <summary>
/// Converts a thresholds JSON payload into symbol-aware metric threshold definitions.
/// </summary>
public sealed class ThresholdsParser
{
  /// <summary>
  /// Symbol levels supported by the thresholds document.
  /// </summary>
  private static readonly MetricSymbolLevel[] SupportedLevels =
  {
        MetricSymbolLevel.Solution,
        MetricSymbolLevel.Assembly,
        MetricSymbolLevel.Namespace,
        MetricSymbolLevel.Type,
        MetricSymbolLevel.Member
    };

  /// <summary>
  /// Parses the JSON payload and returns metric threshold definitions grouped by symbol level.
  /// </summary>
  /// <param name="input">JSON payload with thresholds. May be <see langword="null"/>.</param>
  /// <returns>Dictionary with threshold definitions.</returns>
  /// <remarks>
  /// Expected JSON format: object with "metrics" array containing metric objects.
  /// Each metric object should have "name", optional "description", and optional "symbolThresholds".
  /// </remarks>
  /// <exception cref="InvalidOperationException">Thrown when JSON parsing fails or format is invalid.</exception>
  public static Dictionary<MetricIdentifier, MetricThresholdDefinition> Parse(string? input)
  {
    var thresholds = CreateDefaults();

    if (string.IsNullOrWhiteSpace(input))
    {
      return thresholds;
    }

    try
    {
      var sanitizedInput = input.Replace('\'', '"');
      using var document = JsonDocument.Parse(sanitizedInput);

      var rootElement = document.RootElement;
      if (rootElement.ValueKind != JsonValueKind.Object ||
          !rootElement.TryGetProperty("metrics", out var metricsElement) ||
          metricsElement.ValueKind != JsonValueKind.Array)
      {
        throw new InvalidOperationException(
            "Invalid thresholds JSON format. Expected object with 'metrics' array property.");
      }

      foreach (var metricElement in metricsElement.EnumerateArray())
      {
        ParseMetricEntry(metricElement, thresholds);
      }
    }
    catch (JsonException ex)
    {
      throw new InvalidOperationException("Failed to parse metrics thresholds JSON.", ex);
    }

    return thresholds;
  }

  /// <summary>
  /// Parses a single metric entry from the JSON structure.
  /// </summary>
  /// <param name="metricElement">The JSON element representing a metric definition.</param>
  /// <param name="thresholds">The thresholds dictionary to update.</param>
  /// <remarks>
  /// Processes a metric entry by:
  /// 1. Extracting the metric identifier from the "name" property
  /// 2. Creating or cloning a threshold definition
  /// 3. Applying description if present
  /// 4. Parsing symbol-level thresholds if present
  /// </remarks>
  private static void ParseMetricEntry(
      JsonElement metricElement,
      Dictionary<MetricIdentifier, MetricThresholdDefinition> thresholds)
  {
    if (!TryExtractMetricIdentifier(metricElement, out var identifier))
    {
      return;
    }

    var definition = GetOrCreateDefinition(thresholds, identifier);
    definition = ApplyDescription(metricElement, definition);

    var higherIsBetter = ExtractHigherIsBetter(metricElement, definition.Levels);
    var positiveDeltaNeutral = ExtractPositiveDeltaNeutral(metricElement, definition.Levels);
    ApplySymbolThresholds(metricElement, definition, higherIsBetter, positiveDeltaNeutral);

    thresholds[identifier] = definition;
  }

  /// <summary>
  /// Extracts the metric identifier from a metric JSON element.
  /// </summary>
  /// <param name="metricElement">The JSON element containing the metric definition.</param>
  /// <param name="identifier">When this method returns, contains the parsed metric identifier if successful.</param>
  /// <returns><see langword="true"/> if the identifier was successfully extracted; otherwise, <see langword="false"/>.</returns>
  private static bool TryExtractMetricIdentifier(JsonElement metricElement, out MetricIdentifier identifier)
  {
    identifier = default;

    if (!metricElement.TryGetProperty("name", out var nameProperty))
    {
      return false;
    }

    var metricName = nameProperty.GetString();
    if (string.IsNullOrWhiteSpace(metricName))
    {
      return false;
    }

    return Enum.TryParse(metricName, ignoreCase: true, out identifier);
  }

  /// <summary>
  /// Gets an existing threshold definition or creates a new one with default values.
  /// </summary>
  /// <param name="thresholds">The thresholds dictionary to check.</param>
  /// <param name="identifier">The metric identifier.</param>
  /// <returns>An existing cloned definition or a new default definition.</returns>
  private static MetricThresholdDefinition GetOrCreateDefinition(
      Dictionary<MetricIdentifier, MetricThresholdDefinition> thresholds,
      MetricIdentifier identifier)
  {
    if (thresholds.TryGetValue(identifier, out var existing))
    {
      return CloneDefinition(existing);
    }

    var higherIsBetter = ExtractHigherIsBetter(thresholds, identifier);
    var positiveDeltaNeutral = ExtractPositiveDeltaNeutral(thresholds, identifier);
    return new MetricThresholdDefinition
    {
      Levels = CreateUniformThresholds(null, null, higherIsBetter, positiveDeltaNeutral)
    };
  }

  /// <summary>
  /// Applies the description from the JSON element to the threshold definition.
  /// </summary>
  /// <param name="metricElement">The JSON element containing the metric definition.</param>
  /// <param name="definition">The threshold definition to update.</param>
  /// <returns>A new definition with the description applied, or the original if no description is present.</returns>
  private static MetricThresholdDefinition ApplyDescription(
      JsonElement metricElement,
      MetricThresholdDefinition definition)
  {
    if (!metricElement.TryGetProperty("description", out var descriptionElement) ||
        descriptionElement.ValueKind != JsonValueKind.String)
    {
      return definition;
    }

    var descriptionValue = descriptionElement.GetString();
    if (string.IsNullOrWhiteSpace(descriptionValue))
    {
      return definition;
    }

    return new MetricThresholdDefinition
    {
      Description = descriptionValue,
      Levels = definition.Levels
    };
  }

  /// <summary>
  /// Applies symbol-level thresholds from the JSON element to the threshold definition.
  /// </summary>
  /// <param name="metricElement">The JSON element containing the metric definition.</param>
  /// <param name="definition">The threshold definition to update.</param>
  /// <param name="higherIsBetter">The "higher is better" preference for the thresholds.</param>
  /// <param name="positiveDeltaNeutral">Whether positive deltas should render neutrally for this metric.</param>
  /// <remarks>
  /// Processes the "symbolThresholds" object, which contains threshold values for different
  /// symbol levels (Solution, Assembly, Namespace, Type, Member).
  /// The method also ensures every level has consistent presentation metadata even if not explicitly listed.
  /// </remarks>
  private static void ApplySymbolThresholds(
      JsonElement metricElement,
      MetricThresholdDefinition definition,
      bool higherIsBetter,
      bool positiveDeltaNeutral)
  {
    SymbolThresholdProcessor.ApplySymbolThresholds(
        metricElement,
        definition,
        (warning, error) => CreateThreshold(warning, error, higherIsBetter, positiveDeltaNeutral));
  }

  /// <summary>
  /// Extracts the "higher is better" preference from a metric JSON element or falls back to existing levels.
  /// </summary>
  /// <param name="metricElement">The JSON element containing the metric definition.</param>
  /// <param name="levels">Existing threshold levels to use as fallback.</param>
  /// <returns><see langword="true"/> if higher values are better; otherwise, <see langword="false"/>.</returns>
  private static bool ExtractHigherIsBetter(
      JsonElement metricElement,
      IDictionary<MetricSymbolLevel, MetricThreshold> levels)
  {
    if (metricElement.TryGetProperty("higherIsBetter", out var higherIsBetterProperty) &&
        higherIsBetterProperty.ValueKind is JsonValueKind.True or JsonValueKind.False)
    {
      return higherIsBetterProperty.GetBoolean();
    }

    return ExtractHigherIsBetter(levels);
  }

  private static bool ExtractHigherIsBetter(
      IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds,
      MetricIdentifier identifier)
  {
    if (thresholds.TryGetValue(identifier, out var existing))
    {
      foreach (var threshold in existing.Levels.Values)
      {
        return threshold.HigherIsBetter;
      }
    }

    foreach (var definition in thresholds.Values)
    {
      foreach (var threshold in definition.Levels.Values)
      {
        return threshold.HigherIsBetter;
      }
    }

    return true;
  }

  private static bool ExtractHigherIsBetter(
      IDictionary<MetricSymbolLevel, MetricThreshold> levels)
  {
    foreach (var threshold in levels.Values)
    {
      return threshold.HigherIsBetter;
    }

    return true;
  }

  private static bool ExtractPositiveDeltaNeutral(
      JsonElement metricElement,
      IDictionary<MetricSymbolLevel, MetricThreshold> levels)
  {
    if (metricElement.TryGetProperty("positiveDeltaNeutral", out var positiveDeltaNeutralProperty) &&
        positiveDeltaNeutralProperty.ValueKind is JsonValueKind.True or JsonValueKind.False)
    {
      return positiveDeltaNeutralProperty.GetBoolean();
    }

    return ExtractPositiveDeltaNeutral(levels);
  }

  private static bool ExtractPositiveDeltaNeutral(
      IDictionary<MetricIdentifier, MetricThresholdDefinition> thresholds,
      MetricIdentifier identifier)
  {
    if (thresholds.TryGetValue(identifier, out var existing))
    {
      foreach (var threshold in existing.Levels.Values)
      {
        return threshold.PositiveDeltaNeutral;
      }
    }

    foreach (var definition in thresholds.Values)
    {
      foreach (var threshold in definition.Levels.Values)
      {
        return threshold.PositiveDeltaNeutral;
      }
    }

    return false;
  }

  private static bool ExtractPositiveDeltaNeutral(
      IDictionary<MetricSymbolLevel, MetricThreshold> levels)
  {
    foreach (var threshold in levels.Values)
    {
      return threshold.PositiveDeltaNeutral;
    }

    return false;
  }

  private static MetricThreshold CreateThreshold(
      decimal? warning,
      decimal? error,
      bool higherIsBetter,
      bool positiveDeltaNeutral)
      => new()
      {
        Warning = warning,
        Error = error,
        HigherIsBetter = higherIsBetter,
        PositiveDeltaNeutral = positiveDeltaNeutral
      };

  private static MetricThreshold CloneThreshold(MetricThreshold source)
      => CreateThreshold(source.Warning, source.Error, source.HigherIsBetter, source.PositiveDeltaNeutral);

  private static MetricThresholdDefinition CloneDefinition(MetricThresholdDefinition source)
  {
    var cloneLevels = new Dictionary<MetricSymbolLevel, MetricThreshold>();
    foreach (var (level, threshold) in source.Levels)
    {
      cloneLevels[level] = CloneThreshold(threshold);
    }

    return new MetricThresholdDefinition
    {
      Description = source.Description,
      Levels = cloneLevels
    };
  }

  private static Dictionary<MetricIdentifier, MetricThresholdDefinition> CreateDefaults()
  {
    return new Dictionary<MetricIdentifier, MetricThresholdDefinition>
    {
      [MetricIdentifier.OpenCoverSequenceCoverage] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(75, 60, true) },
      [MetricIdentifier.OpenCoverBranchCoverage] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(70, 55, true) },
      [MetricIdentifier.OpenCoverCyclomaticComplexity] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(15, 30, false) },
      [MetricIdentifier.OpenCoverNPathComplexity] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(200, 400, false) },
      [MetricIdentifier.RoslynMaintainabilityIndex] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(65, 40, true) },
      [MetricIdentifier.RoslynCyclomaticComplexity] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(12, 25, false) },
      [MetricIdentifier.RoslynClassCoupling] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(50, 80, false) },
      [MetricIdentifier.RoslynDepthOfInheritance] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(5, 8, false) },
      [MetricIdentifier.RoslynSourceLines] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(null, null, false, true) },
      [MetricIdentifier.RoslynExecutableLines] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(null, null, false, true) },
      [MetricIdentifier.SarifCaRuleViolations] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(5, 10, false) },
      [MetricIdentifier.SarifIdeRuleViolations] = new MetricThresholdDefinition { Levels = CreateUniformThresholds(10, 20, false) }
    };
  }

  private static Dictionary<MetricSymbolLevel, MetricThreshold> CreateUniformThresholds(
      decimal? warning,
      decimal? error,
      bool higherIsBetter,
      bool positiveDeltaNeutral = false)
  {
    var result = new Dictionary<MetricSymbolLevel, MetricThreshold>();
    foreach (var level in SupportedLevels)
    {
      result[level] = CreateThreshold(warning, error, higherIsBetter, positiveDeltaNeutral);
    }

    return result;
  }
}


