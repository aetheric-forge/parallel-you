using AethericForge.Runtime.Abstractions.Interfaces.Authorities;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Authentication;
using AethericForge.Runtime.Abstractions.Interfaces.Knowledge.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Library.Services;
using AethericForge.Runtime.Models.Identity.Primitives;
using AethericForge.Runtime.Models.Knowledge.Authorities;
using AethericForge.Runtime.Providers.Knowledge.InMemory;
using AethericForge.Runtime.Services.Knowledge;
using AethericForge.Runtime.Services.Library;
using Moq;
using ParallelYou.Abstractions.Person;
using ParallelYou.Services.Person;

namespace ParallelYou.Tests.Services.Person;

public sealed class PersonServiceTests
{
    private static (PersonService Service, ILibrarian Librarian) CreateService()
    {
        var provider = new InMemoryKnowledgeProvider("InMemory");
        var knowledgeService = new KnowledgeService(
            [provider],
            Mock.Of<ITeam<ICuratorClerk>>());
        var librarian = new Librarian(
            knowledgeService,
            Mock.Of<ITeam<ILibraryClerk>>());

        return (new PersonService(librarian), librarian);
    }

    [Fact]
    public async Task GetOrCreatePersonAsync_ReturnsSamePersonForSameIdentity()
    {
        var (service, librarian) = CreateService();
        var identity = new IdentitySubject(
            "person-subject",
            IdentityScheme.OpenIdConnect,
            "Test Person");

        var first = await service.GetOrCreatePersonAsync(identity);
        var second = await service.GetOrCreatePersonAsync(identity);

        Assert.Equal(first.Id, second.Id);

        var authority = new KnowledgeAuthority(identity, PersonAuthority.Context);
        var artifacts = await librarian.FindArtifactsAsync(authority);
        Assert.Single(artifacts);
    }

    [Fact]
    public async Task GetOrCreatePersonAsync_CreatesDifferentPeopleForDifferentIdentities()
    {
        var (service, _) = CreateService();
        var firstIdentity = new IdentitySubject(
            "first-subject",
            IdentityScheme.OpenIdConnect);
        var secondIdentity = new IdentitySubject(
            "second-subject",
            IdentityScheme.OpenIdConnect);

        var first = await service.GetOrCreatePersonAsync(firstIdentity);
        var second = await service.GetOrCreatePersonAsync(secondIdentity);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task GetOrCreatePersonAsync_RejectsNullIdentity()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.GetOrCreatePersonAsync(null!));
    }
}
