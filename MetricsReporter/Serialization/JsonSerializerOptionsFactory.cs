namespace MetricsReporter.Serialization;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Creates preconfigured JSON options for (de)serialising reports.
/// </summary>
public static class JsonSerializerOptionsFactory
{
  /// <summary>
  /// Returns JSON serialisation options with camel case naming and enum support.
  /// </summary>
  public static JsonSerializerOptions Create()
  {
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
      WriteIndented = true,
      PropertyNameCaseInsensitive = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    options.Converters.Add(new JsonStringEnumConverter());
    return options;
  }
}


