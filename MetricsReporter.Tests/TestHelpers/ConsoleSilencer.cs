using System;
using System.IO;

namespace MetricsReporter.Tests.TestHelpers;

/// <summary>
/// Temporarily silences console output to avoid noisy test logs.
/// </summary>
internal sealed class ConsoleSilencer : IDisposable
{
  private readonly TextWriter _originalOut;

  public ConsoleSilencer()
  {
    _originalOut = Console.Out;
    Console.SetOut(TextWriter.Null);
  }

  public void Dispose()
  {
    Console.SetOut(_originalOut);
  }
}

