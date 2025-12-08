using System;
using System.Collections.Generic;
using FluentAssertions;
using MetricsReporter.Cli.Commands;
using MetricsReporter.Cli.Configuration;
using MetricsReporter.Cli.Settings;
using MetricsReporter.Configuration;
using MetricsReporter.Model;
using CliConfigurationLoadResult = MetricsReporter.Cli.Commands.ConfigurationLoadResult;
using NUnit.Framework;

namespace MetricsReporter.Tests.Cli.Commands;

[TestFixture]
[Category("Unit")]
public sealed class GenerateScriptResolverTests
{
  private static readonly ResolvedGeneralOptions DefaultGeneralOptions =
    new("normal", TimeSpan.FromSeconds(30), Environment.CurrentDirectory, 1000, true, true);

  [Test]
  public void Resolve_WithNullSettings_Throws()
  {
    var configuration = CreateConfiguration(envGenerate: Array.Empty<string>(), fileGenerate: Array.Empty<string>());

    Action act = () => GenerateScriptResolver.Resolve(null!, configuration);

    act.Should().Throw<ArgumentNullException>();
  }

  [Test]
  public void Resolve_WithCliScripts_OverridesEnvAndFile()
  {
    var settings = new GenerateSettings { Scripts = new List<string> { "cli.ps1" } };
    var configuration = CreateConfiguration(
      envGenerate: new[] { "env.ps1" },
      fileGenerate: new[] { "file.ps1" });

    var result = GenerateScriptResolver.Resolve(settings, configuration);

    result.Succeeded.Should().BeTrue();
    result.Scripts!.Generate.Should().BeEquivalentTo("cli.ps1");
    result.Scripts.ReadAny.Should().BeEmpty();
    result.Scripts.ReadByMetric.Should().BeEmpty();
    result.Scripts.TestAny.Should().BeEmpty();
    result.Scripts.TestByMetric.Should().BeEmpty();
  }

  [Test]
  public void Resolve_WhenCliEmpty_UsesEnvironmentGenerateScripts()
  {
    var settings = new GenerateSettings();
    var configuration = CreateConfiguration(
      envGenerate: new[] { "env.ps1" },
      fileGenerate: new[] { "file.ps1" });

    var result = GenerateScriptResolver.Resolve(settings, configuration);

    result.Succeeded.Should().BeTrue();
    result.Scripts!.Generate.Should().BeEquivalentTo("env.ps1");
  }

  [Test]
  public void Resolve_WhenCliAndEnvironmentEmpty_FallsBackToFileScripts()
  {
    var settings = new GenerateSettings();
    var configuration = CreateConfiguration(
      envGenerate: null,
      fileGenerate: new[] { "file.ps1" });

    var result = GenerateScriptResolver.Resolve(settings, configuration);

    result.Succeeded.Should().BeTrue();
    result.Scripts!.Generate.Should().BeEquivalentTo("file.ps1");
  }

  [Test]
  public void Resolve_WhenNoSourcesPresent_ReturnsEmptyGenerateScripts()
  {
    var settings = new GenerateSettings();
    var configuration = CreateConfiguration(envGenerate: Array.Empty<string>(), fileGenerate: Array.Empty<string>());

    var result = GenerateScriptResolver.Resolve(settings, configuration);

    result.Succeeded.Should().BeTrue();
    result.Scripts!.Generate.Should().BeEmpty();
  }

  private static CliConfigurationLoadResult CreateConfiguration(
    IReadOnlyList<string>? envGenerate,
    IReadOnlyList<string>? fileGenerate)
  {
    return CliConfigurationLoadResult.Success(
      DefaultGeneralOptions,
      new MetricsReporterConfiguration
      {
        Scripts = new ScriptsConfiguration
        {
          Generate = envGenerate,
          Read = new ReadScriptsConfiguration { Any = Array.Empty<string>() },
          Test = new ReadScriptsConfiguration { Any = Array.Empty<string>() }
        }
      },
      new MetricsReporterConfiguration
      {
        Scripts = new ScriptsConfiguration
        {
          Generate = fileGenerate,
          Read = new ReadScriptsConfiguration { Any = Array.Empty<string>() },
          Test = new ReadScriptsConfiguration { Any = Array.Empty<string>() }
        }
      },
      new Dictionary<MetricIdentifier, IReadOnlyList<string>>());
  }
}


