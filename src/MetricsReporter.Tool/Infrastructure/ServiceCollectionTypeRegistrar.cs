using System;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace MetricsReporter.Tool.Infrastructure;

internal sealed class ServiceCollectionTypeRegistrar : ITypeRegistrar
{
  private readonly IServiceCollection _services;

  public ServiceCollectionTypeRegistrar(IServiceCollection services)
  {
    _services = services ?? throw new ArgumentNullException(nameof(services));
  }

  public ITypeResolver Build()
  {
    return new ServiceProviderTypeResolver(_services.BuildServiceProvider());
  }

  public void Register(Type service, Type implementation)
  {
    _services.AddSingleton(service, implementation);
  }

  public void RegisterInstance(Type service, object implementation)
  {
    _services.AddSingleton(service, implementation);
  }

  public void RegisterLazy(Type service, Func<object> factory)
  {
    _services.AddSingleton(service, _ => factory());
  }
}

internal sealed class ServiceProviderTypeResolver : ITypeResolver, IDisposable
{
  private readonly ServiceProvider _provider;

  public ServiceProviderTypeResolver(ServiceProvider provider)
  {
    _provider = provider;
  }

  public object? Resolve(Type? type)
  {
    return type is null ? null : _provider.GetService(type);
  }

  public void Dispose()
  {
    _provider.Dispose();
  }
}

