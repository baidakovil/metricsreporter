namespace MetricsReporter.MetricsReader;

using System.Threading;

/// <summary>
/// Provides a globally accessible cancellation token for the metrics reader commands.
/// </summary>
internal static class MetricsReaderCancellation
{
  private static CancellationToken _token;

  public static void Initialize(CancellationToken token)
    => _token = token;

  public static CancellationToken Token => _token;
}


