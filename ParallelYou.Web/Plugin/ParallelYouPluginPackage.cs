using AethericForge.Runtime.Abstractions.Interfaces.Institutions;
using AethericForge.Runtime.Abstractions.Interfaces.Institutions.Plugins;
using AethericForge.Runtime.Institutions.Abstractions.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParallelYou.Abstractions;
using ParallelYou.Services;
using ParallelYou.Web.Hosting;
using System.Runtime.Loader;

[assembly: InstitutionPluginPackage(typeof(ParallelYou.Web.Plugin.ParallelYouPluginPackage))]

namespace ParallelYou.Web.Plugin;

public sealed class ParallelYouPluginPackage : IInstitutionPluginPackage
{
    public IReadOnlyCollection<IInstitutionFactory> GetFactories() => [new ParallelYouWebFactory()];
}

/// <summary>Web host adapter discovered through the existing institution plugin loader.</summary>
public sealed class ParallelYouWebFactory : IInstitutionFactory
{
    // Retain the context during discovery, before the host takes ownership of the module.
    private readonly AssemblyLoadContext _loadContext =
        AssemblyLoadContext.GetLoadContext(typeof(ParallelYouWebFactory).Assembly)!;
    public Type ContractType => typeof(IParallelYou);
    public IInstitutionTemplate Template { get; } = ParallelYouModule.CreateTemplate();
    public IInstitutionManifest Manifest => Template.Descriptor;

    // The shell calls this adapter before building its service provider. All arguments are shared
    // framework/runtime contracts; the shell never references Parallel You's implementation types.
    public void ConfigureWebServices(IServiceCollection services, IConfiguration configuration,
        Func<IServiceProvider, IInstitution?> parent) => services.AddStandaloneParallelYou(configuration, parent);

    public IInstitution Create(IInstitution parent, IServiceProvider services) => ParallelYouHosting.Create(services,
        ParallelYouHosting.CreateContext(services.GetRequiredService<IConfiguration>(), services, parent));
}
