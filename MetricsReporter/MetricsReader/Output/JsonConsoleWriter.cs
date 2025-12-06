namespace MetricsReporter.MetricsReader.Output;

using System.Text.Json;
using MetricsReporter.Serialization;

/// <summary>
/// Serialises command results to JSON for easy scripting.
/// </summary>
internal static class JsonConsoleWriter
{
  public static void Write(object? payload)
  {
    var options = JsonSerializerOptionsFactory.Create();
    var json = JsonSerializer.Serialize(payload, options);
    Console.Out.WriteLine(json);
  }
}


