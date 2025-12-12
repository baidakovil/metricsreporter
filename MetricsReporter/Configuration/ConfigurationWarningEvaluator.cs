namespace MetricsReporter.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using MetricsReporter.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Evaluates conditional configuration requirements defined in JSON and emits warnings when expected values are missing.
/// </summary>
internal static class ConfigurationWarningEvaluator
{
  private const string MatrixResourceName = "MetricsReporter.Configuration.Warnings.required-settings.matrix.json";
  private const string MatrixResourceFileName = "required-settings.matrix.json";
  private static readonly Lazy<WarningRuleSet> RuleSet = new(LoadRuleSet, isThreadSafe: true);

  /// <summary>
  /// Logs configuration warnings for a command using the configured logger.
  /// </summary>
  /// <param name="options">Resolved options.</param>
  /// <param name="commandName">Command name (generate, read, readsarif, test).</param>
  /// <param name="logger">Logger instance.</param>
  public static void LogWarnings(MetricsReporterOptions options, string commandName, ILogger logger)
  {
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(logger);

    if (ShouldSuppressWarnings())
    {
      return;
    }

    var warnings = CollectWarnings(options, commandName);
    foreach (var warning in warnings)
    {
      logger.LogWarning("Config warning [{RuleId}]: {Message}", warning.RuleId, warning.Message);
    }
  }

  /// <summary>
  /// Collects configuration warnings for the specified command without logging.
  /// </summary>
  /// <param name="options">Resolved options.</param>
  /// <param name="commandName">Command name (generate, read, readsarif, test).</param>
  /// <returns>List of warnings.</returns>
  public static IReadOnlyList<ConfigurationWarning> CollectWarnings(MetricsReporterOptions options, string commandName)
  {
    ArgumentNullException.ThrowIfNull(options);

    var normalizedCommand = string.IsNullOrWhiteSpace(commandName) ? "generate" : commandName.Trim();
    var ruleSet = RuleSet.Value;
    if (ruleSet.Rules.Count == 0)
    {
      return Array.Empty<ConfigurationWarning>();
    }

    var results = new List<ConfigurationWarning>();
    foreach (var rule in ruleSet.Rules)
    {
      if (!RuleApplies(rule, normalizedCommand))
      {
        continue;
      }

      if (!ConditionSatisfied(rule.Condition, options))
      {
        continue;
      }

      foreach (var requirement in rule.Requirements)
      {
        if (RequirementSatisfied(requirement, options))
        {
          continue;
        }

        results.Add(new ConfigurationWarning(rule.Id, requirement.Message));
      }
    }

    return results;
  }

  private static bool RuleApplies(WarningRule rule, string commandName)
  {
    if (rule.AppliesTo.Count == 0)
    {
      return true;
    }

    return rule.AppliesTo.Any(c => string.Equals(c, commandName, StringComparison.OrdinalIgnoreCase));
  }

  private static bool ConditionSatisfied(WarningCondition? condition, MetricsReporterOptions options)
  {
    if (condition is null)
    {
      return true;
    }

    if (string.IsNullOrWhiteSpace(condition.Setting))
    {
      return true;
    }

    var value = GetSettingValue(options, condition.Setting);
    return condition.Operator?.ToLowerInvariant() switch
    {
      "equals" => EqualsCondition(value, condition.Value),
      "isnullorempty" => IsNullOrEmpty(value),
      "notnullorempty" => !IsNullOrEmpty(value),
      _ => true
    };
  }

  private static bool RequirementSatisfied(WarningRequirement requirement, MetricsReporterOptions options)
  {
    return requirement.Kind?.ToLowerInvariant() switch
    {
      "nonempty" => !string.IsNullOrWhiteSpace(requirement.Setting)
                    && !IsNullOrEmpty(GetSettingValue(options, requirement.Setting)),
      "nonemptycollection" => !string.IsNullOrWhiteSpace(requirement.Setting)
                              && IsNonEmptyCollection(GetSettingValue(options, requirement.Setting)),
      "atleastone" => AtLeastOnePresent(requirement.Settings, options),
      "directoryexists" => DirectoryExists(requirement.Setting, options),
      "parentdirectoryexists" => ParentDirectoryExists(requirement.Setting, options),
      _ => true
    };
  }

  private static bool EqualsCondition(object? settingValue, JsonElement? conditionValue)
  {
    if (conditionValue is null)
    {
      return true;
    }

    if (conditionValue.Value.ValueKind == JsonValueKind.True || conditionValue.Value.ValueKind == JsonValueKind.False)
    {
      if (settingValue is bool boolValue)
      {
        return boolValue == conditionValue.Value.GetBoolean();
      }

      if (settingValue is string boolString && bool.TryParse(boolString, out var parsed))
      {
        return parsed == conditionValue.Value.GetBoolean();
      }

      return false;
    }

    if (conditionValue.Value.ValueKind == JsonValueKind.String)
    {
      var expected = conditionValue.Value.GetString() ?? string.Empty;
      return string.Equals(Convert.ToString(settingValue, System.Globalization.CultureInfo.InvariantCulture), expected, StringComparison.OrdinalIgnoreCase);
    }

    return false;
  }

  private static bool AtLeastOnePresent(IReadOnlyCollection<string>? settings, MetricsReporterOptions options)
  {
    if (settings is null || settings.Count == 0)
    {
      return false;
    }

    return settings.Any(setting =>
    {
      var value = GetSettingValue(options, setting);
      return !IsNullOrEmpty(value) || IsNonEmptyCollection(value);
    });
  }

  private static bool IsNonEmptyCollection(object? value)
  {
    if (value is IEnumerable<string> strings)
    {
      return strings.Any(s => !string.IsNullOrWhiteSpace(s));
    }

    return false;
  }

  private static bool IsNullOrEmpty(object? value)
  {
    if (value is null)
    {
      return true;
    }

    return value switch
    {
      string text => string.IsNullOrWhiteSpace(text),
      IEnumerable<string> collection => !collection.Any(item => !string.IsNullOrWhiteSpace(item)),
      _ => false
    };
  }

  private static bool DirectoryExists(string? settingName, MetricsReporterOptions options)
  {
    if (string.IsNullOrWhiteSpace(settingName))
    {
      return true;
    }

    var value = GetSettingValue(options, settingName) as string;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    return Directory.Exists(value);
  }

  private static bool ParentDirectoryExists(string? settingName, MetricsReporterOptions options)
  {
    if (string.IsNullOrWhiteSpace(settingName))
    {
      return true;
    }

    var value = GetSettingValue(options, settingName) as string;
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    var parent = Path.GetDirectoryName(value);
    if (string.IsNullOrWhiteSpace(parent))
    {
      return false;
    }

    return Directory.Exists(parent);
  }

  private static object? GetSettingValue(MetricsReporterOptions options, string settingName)
  {
    if (string.IsNullOrWhiteSpace(settingName))
    {
      return null;
    }

    var property = typeof(MetricsReporterOptions).GetProperty(
      settingName,
      BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

    return property?.GetValue(options);
  }

  private static WarningRuleSet LoadRuleSet()
  {
    try
    {
      var assembly = Assembly.GetExecutingAssembly();
      var resourceName = assembly
        .GetManifestResourceNames()
        .FirstOrDefault(name => name.EndsWith(MatrixResourceFileName, StringComparison.OrdinalIgnoreCase))
        ?? MatrixResourceName;

      using var stream = assembly.GetManifestResourceStream(resourceName);
      if (stream is null)
      {
        return new WarningRuleSet();
      }

      using var reader = new StreamReader(stream);
      var json = reader.ReadToEnd();
      var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
      return JsonSerializer.Deserialize<WarningRuleSet>(json, options) ?? new WarningRuleSet();
    }
    catch
    {
      return new WarningRuleSet();
    }
  }

  private static bool ShouldSuppressWarnings()
  {
    var env = Environment.GetEnvironmentVariable("METRICSREPORTER_SUPPRESS_CONFIG_WARNINGS");
    if (!string.IsNullOrWhiteSpace(env) && bool.TryParse(env, out var suppress) && suppress)
    {
      return true;
    }

    var processName = Process.GetCurrentProcess().ProcessName;
    if (processName.Contains("testhost", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    var friendlyName = AppDomain.CurrentDomain.FriendlyName;
    if (!string.IsNullOrWhiteSpace(friendlyName) && friendlyName.Contains("testhost", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    var args = Environment.GetCommandLineArgs();
    if (args.Any(a => a.Contains("testhost.dll", StringComparison.OrdinalIgnoreCase)))
    {
      return true;
    }

    return args.Any(a =>
      a.Contains("vstest", StringComparison.OrdinalIgnoreCase)
      || a.Contains("nunit", StringComparison.OrdinalIgnoreCase)
      || a.Contains("mstest", StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Represents a configuration warning.
  /// </summary>
  /// <param name="RuleId">Rule identifier from the matrix.</param>
  /// <param name="Message">Warning message to log.</param>
  internal sealed record ConfigurationWarning(string RuleId, string Message);

  private sealed record WarningRuleSet
  {
    public List<WarningRule> Rules { get; init; } = new();
  }

  private sealed record WarningRule
  {
    public string Id { get; init; } = string.Empty;
    public List<string> AppliesTo { get; init; } = new();
    public WarningCondition? Condition { get; init; }
    public List<WarningRequirement> Requirements { get; init; } = new();
  }

  private sealed record WarningCondition
  {
    public string Setting { get; init; } = string.Empty;
    public string? Operator { get; init; }
    public JsonElement? Value { get; init; }
  }

  private sealed record WarningRequirement
  {
    public string? Kind { get; init; }
    public string? Setting { get; init; }
    public List<string>? Settings { get; init; }
    public string Message { get; init; } = string.Empty;
  }
}

