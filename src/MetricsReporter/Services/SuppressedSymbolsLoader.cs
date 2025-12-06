namespace MetricsReporter.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MetricsReporter.Model;
using MetricsReporter.Serialization;

/// <summary>
/// Loads suppressed symbol metadata from <c>SuppressedSymbols.json</c> when present.
/// </summary>
/// <remarks>
/// This helper keeps JSON handling for suppressed symbols localized and reuses the
/// same <see cref="System.Text.Json.JsonSerializerOptions"/> as the main report to
/// guarantee consistent casing and enum handling.
/// </remarks>
internal static class SuppressedSymbolsLoader
{
  /// <summary>
  /// Loads suppressed symbol entries from the specified JSON file if it exists.
  /// </summary>
  /// <param name="path">Path to <c>SuppressedSymbols.json</c> or <see langword="null"/>.</param>
  /// <param name="cancellationToken">Cancellation token for I/O operations.</param>
  /// <returns>
  /// A list of <see cref="SuppressedSymbolInfo"/> instances. Returns an empty list when
  /// the path is <see langword="null"/>, empty, or the file is missing.
  /// </returns>
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Microsoft.Maintainability",
      "CA1506:Avoid excessive class coupling",
      Justification = "Method performs file I/O and JSON deserialization; further decomposition would require wrapper methods which are prohibited by refactoring rules.")]
  public static async Task<IList<SuppressedSymbolInfo>> LoadAsync(string? path, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
    {
      return [];
    }

    await using var stream = File.OpenRead(path);
    var options = JsonSerializerOptionsFactory.Create();
    var report = await System.Text.Json.JsonSerializer
        .DeserializeAsync<SuppressedSymbolsReport>(stream, options, cancellationToken)
        .ConfigureAwait(false);

    return report?.SuppressedSymbols ?? [];
  }
}



