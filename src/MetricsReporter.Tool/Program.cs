using System;
using MetricsReporter;
using MetricsReporter.MetricsReader;

if (args.Length > 0 && string.Equals(args[0], "metrics-reader", StringComparison.OrdinalIgnoreCase))
{
  var forwardedArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();
  return await MetricsReaderConsoleHost.ExecuteAsync(forwardedArgs).ConfigureAwait(false);
}

return await MetricsReporterConsoleHost.ExecuteAsync(args).ConfigureAwait(false);


