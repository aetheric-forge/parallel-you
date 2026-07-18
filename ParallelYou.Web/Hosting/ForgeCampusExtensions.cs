using ParallelYou.Services.Capture;
using ParallelYou.Abstractions.Capture;
using ParallelYou.Abstractions.Person;
using ParallelYou.Services.Person;
using ParallelYou.Abstractions.Reflection;
using ParallelYou.Services.Reflection;
using AethericForge.Runtime.Abstractions.Interfaces.Archive.Primitives;
using AethericForge.Runtime.Abstractions.Interfaces.Archive.Providers;
using AethericForge.Runtime.Abstractions.Interfaces.Archive.Serialization;
using AethericForge.Runtime.Abstractions.Interfaces.Archive.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Authorities;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Authentication;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Lifecycle;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Provisioning;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Providers;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Post.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Staging.Providers;
using AethericForge.Runtime.Abstractions.Interfaces.Staging.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Workbench.Services;
using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Institutions.Abstractions.Composition;
using AethericForge.Runtime.Institutions.Abstractions.Models;
using AethericForge.Runtime.Institutions.Abstractions.Primitives;
using AethericForge.Runtime.Institutions.Archive;
using AethericForge.Runtime.Institutions.Campus;
using AethericForge.Runtime.Institutions.Library;
using AethericForge.Runtime.Institutions.PostOffice;
using AethericForge.Runtime.Institutions.Registry;
using AethericForge.Runtime.Institutions.Workbench;
using AethericForge.Runtime.Models.Archive.Serialization;
using AethericForge.Runtime.Models.Authorities;
using AethericForge.Runtime.Models.Identity.Primitives;
using AethericForge.Runtime.Providers.Archive.InMemory;
using AethericForge.Runtime.Providers.Identity.InMemory;
using AethericForge.Runtime.Providers.Identity.Keycloak;
using AethericForge.Runtime.Providers.Knowledge.InMemory;
using AethericForge.Runtime.Providers.Staging.InMemory;
using AethericForge.Runtime.Services.Archive;
using AethericForge.Runtime.Services.Identity;
using AethericForge.Runtime.Services.Identity.Lifecycle;
using AethericForge.Runtime.Services.Knowledge;
using AethericForge.Runtime.Services.Library;
using AethericForge.Runtime.Services.Post;
using AethericForge.Runtime.Services.Registry;
using AethericForge.Runtime.Services.Staging;
using AethericForge.Runtime.Services.Workbench;

namespace ParallelYou.Web.Hosting;

public static class ForgeCampusExtensions 
{
    public static IServiceCollection AddForgeCampus(this IServiceCollection services)
    {
        services.AddInstitutionTemplate(builder =>
        {
            builder.WithDescriptor(
                    "ForgeCampus",
                    new Version(0, 1, 0),
                    "The Aetheric Forge learning and collaboration campus.")
                .With<IIdentityLifecycleService, IdentityLifecycleService>()
                .With<IIdentityService, IdentityService>()
                .With<IPersonService, PersonService>()
                .With<IIdentityRegistry, IdentityRegistry>()
                .With<HttpClient, HttpClient>()
                .With<KeycloakOptions>(sp => new KeycloakOptions
                {
                    ClientId = "parallel-you",
                    Realm = "int.aethericforge.ca",
                    Authority = "https://sso-dev.int.aethericforge.ca/realms/int.aethericforge.ca",
                    ClientSecret = sp.GetRequiredService<IConfiguration>().GetValue<string>("Keycloak:ClientSecret")
                                   ?? throw new InvalidOperationException("Keycloak:ClientSecret is required")
                })
                .With<IIdentityProvider, KeycloakIdentityProvider>()
                .With<IRegistryService, RegistryService>()
                .With<ITeam<IRegistryClerk>>(_ => new Team<IRegistryClerk>(Array.Empty<IRegistryClerk>()))
                .With<IRegistrar, Registrar>()
                .With<IRegistryContext, RegistryContext>()
                .With<IRegistry, Registry>()
                .With<IArchiveProvider>(_ => new InMemoryArchiveProvider("InMemory"))
                .With<IArchiveVault, ArchiveVault>()
                .With<IArchiveService, ArchiveService>()
                .With<ITeam<IArchiveClerk>>(_ => new Team<IArchiveClerk>(Array.Empty<IArchiveClerk>()))
                .With<IArchivist, Archivist>()
                .With<IArchiveContext, ArchiveContext>()
                .With<IArchive, Archive>()
                .With<IKnowledgeProvider>(_ => new InMemoryKnowledgeProvider("InMemory"))
                .With<IKnowledgeService, KnowledgeService>()
                .With<ITeam<ICuratorClerk>>(_ => new Team<ICuratorClerk>(Array.Empty<ICuratorClerk>()))
                .With<ICurator, Curator>()
                .With<ILibraryService, LibraryService>()
                .With<ITeam<ILibraryClerk>>(_ => new Team<ILibraryClerk>(Array.Empty<ILibraryClerk>()))
                .With<ILibrarian, Librarian>()
                .With<ILibraryContext, LibraryContext>()
                .With<ILibrary, Library>()
                .With<IStagingProvider>(_ => new InMemoryStagingProvider("InMemory"))
                .With<IStagingProvider>(_ => new InMemoryStagingProvider("ReflectionMapping"))
                .With<IStagingService, StagingService>()
                .With<IWorkbenchService, WorkbenchService>()
                .With<ITeam<IWorkbenchWorker>>(_ => new Team<IWorkbenchWorker>(Array.Empty<IWorkbenchWorker>()))
                .With<IArtificer, Artificer>()
                .With<IWorkbenchContext, WorkbenchContext>()
                .With<IWorkbench, Workbench>();
        });

        services.AddScoped<ICaptureService, CaptureService>();
        services.AddScoped<IReflectionService, ReflectionService>();

        services.AddSingleton<ICampus>(serviceProvider =>
        {
            var campusTemplate = (InstitutionTemplate)serviceProvider.GetRequiredService<IInstitutionTemplate>();
            var campusContext = new CampusContext(campusTemplate, serviceProvider);

            var campus = new Campus(campusContext);

            var registryTemplate = campusTemplate with { Descriptor = new InstitutionDescriptor("Registry", campusTemplate.Descriptor.Version, "Registry institution") };
            campus.Register<IRegistry>(ActivatorUtilities.CreateInstance<Registry>(serviceProvider, new RegistryContext(registryTemplate, serviceProvider, campus)));
            
            var libraryTemplate = campusTemplate with { Descriptor = new InstitutionDescriptor("Library", campusTemplate.Descriptor.Version, "Library institution") };
            campus.Register<ILibrary>(ActivatorUtilities.CreateInstance<Library>(serviceProvider, new LibraryContext(libraryTemplate, serviceProvider, campus)));

            var workbenchTemplate = campusTemplate with { Descriptor = new InstitutionDescriptor("Workbench", campusTemplate.Descriptor.Version, "Workbench institution") };
            campus.Register<IWorkbench>(ActivatorUtilities.CreateInstance<Workbench>(serviceProvider, new WorkbenchContext(workbenchTemplate, serviceProvider, campus)));
            
            return campus;
        });

        services.AddSingleton<ForgeCampusHost>();
        services.AddHostedService<ForgeCampusHost>(serviceProvider =>
            serviceProvider.GetRequiredService<ForgeCampusHost>());
        
        return services;
    }

    public static IEndpointRouteBuilder MapForgeCampusDiagnostics(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            "/api/status",
            (ICampus campus, ForgeCampusHost host) =>
                Results.Ok(new
                {
                    institution = campus.Context.Template.Descriptor.Name,
                    version = campus.Context.Template.Descriptor.Version.ToString(),
                    isRoot = campus.Context.Parent is null,
                    host.IsRunning,
                    registrar = new
                    {
                        name = campus.Registry.Context.Template.Descriptor.Name,
                        version = campus.Registry.Context.Template.Descriptor.Version.ToString()
                    },
                    postOffice = new
                    {
                        name = campus.PostOffice.Context.Template.Descriptor.Name,
                        version = campus.PostOffice.Context.Template.Descriptor.Version.ToString()
                    },
                    archive = new
                    {
                        name = campus.Archive.Context.Template.Descriptor.Name,
                        version = campus.Archive.Context.Template.Descriptor.Version.ToString()
                    },
                    library = new
                    {
                        name = campus.Library.Context.Template.Descriptor.Name,
                        version = campus.Library.Context.Template.Descriptor.Version.ToString()
                    }
                }));

        return endpoints;
    }
}
