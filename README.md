# Parallel You

Parallel You is a private, actor-scoped record of observation, reflection, and direction. It preserves earlier representations instead of overwriting them. Its workflows cover capture, reflection, tracking, intentions, planning, and recommendations with explicit human response.

This repository owns the institution implementation, reusable Blazor UI, provisioning definition, and standalone host. The completed module was imported from `aetheric-forge/aetheric-web` at `e2da89fd84f65b9f38e46aa0bd1a6969f7f5e5d3`. The previous standalone main (`81064ac`) is preserved by the annotated tag `archive/main-before-bootstrap-2026-10-14`.

## Projects

| Project | Purpose |
| --- | --- |
| `ParallelYou.Abstractions` | Institution and workflow contracts |
| `ParallelYou.Models` | Personal artifacts, representations, and submissions |
| `ParallelYou.Services` | Reusable institution composition and workflow implementations |
| `ParallelYou.Web` | Razor component library, authenticated identity access, and consumer configuration loading |
| `ParallelYou.Host` | Standalone ASP.NET Core host with OIDC, persistent authentication keys, and readiness checks |
| `tests/ParallelYou.Tests` | Workflow, isolation, provisioning, and host regression coverage |

`ParallelYouModule.Create` accepts an institution context, a Librarian, and an Artificer. `AddParallelYouModule` exports its workflow services without replacing a host's shared Library or Workbench. The standalone host creates a dedicated Mongo knowledge provider and uses the provisioned parent Redis Workbench. Workflows store durable artifacts in the `knowledge` collection with the `parallel-you` scheme; they do not directly use Archive or S3.

The runtime and primitives dependencies are public HTTPS Git submodules pinned to explicit commits. Neither an adjacent web checkout nor a moving runtime branch is required.

## Build and test

Use .NET 10 and initialize dependencies:

```bash
git clone --recurse-submodules https://github.com/aetheric-forge/parallel-you.git
cd parallel-you
# For an existing checkout:
git submodule update --init --recursive

dotnet test ParallelYou.sln --configuration Release
dotnet publish ParallelYou.Host/ParallelYou.Host.csproj --configuration Release --output .output/host
```

Tests use in-memory providers and an OIDC discovery fixture; they do not need live MongoDB, Redis, or Keycloak. Readiness checks in a running deployment do check MongoDB and Redis.

## Bootstrap from Git

Load these inputs with the existing provisioning Git loader:

| Input | Value |
| --- | --- |
| Repository | `https://github.com/aetheric-forge/parallel-you` |
| Revision | A reviewed tag or full commit SHA |
| Definition path | `institution/parallel-you.yaml` |
| Bindings path | `institution/parallel-you.bindings.yaml` |

The bindings are an example. Select the actual database and parent Registrar/Workbench sources in the provisioning host. The loader resolves the revision to a commit before reading either file. Provisioning owns resource creation, the durable deployment record, and delivery of scoped consumer credentials. See [the bootstrap contract](docs/architecture/bootstrap.md).

## Run the standalone host

Mount the private consumer JSON produced by provisioning and configure the expected deployment:

```bash
export ParallelYou__ProvisionedConfigurationPath=/run/secrets/parallel-you-consumer.json
export ParallelYou__DeploymentId=<deployment-id-from-the-catalog>
export ParallelYou__Environment=production
export ParallelYou__ProtectionKeyDirectory=/var/lib/parallel-you/keys
dotnet run --project ParallelYou.Host
```

`ParallelYou:PlanId` optionally pins the expected plan. The authentication key directory must persist across restarts and be writable by the host user. The browser client created by provisioning must register this host's `/signin-oidc` and `/signout-callback-oidc` URLs.

Without an artifact, provide explicit `ParallelYou:MongoDb`, `Workbench:Redis`, and `Registry:Keycloak` configuration. Mongo requires Host, DatabaseName, Username and Password; provisioned users use AuthenticationDatabase `admin`. Redis requires Host and Password, with optional User, Port, Ssl and Database. Keycloak requires Authority (base URL), Realm, ClientId and ClientSecret. Credentials belong in their owning institution section; endpoint defaults may come from the corresponding platform section. The host never inherits platform/root credentials. Environment-variable keys use double underscores.

The public landing page is `/parallel-you`; `/` redirects there. Every working page requires authentication. `/health/live` reports process liveness; `/health/ready` checks the module's Mongo database and parent Redis connection. Consumer configuration changes require a restart.

## Container

Initialize submodules before building:

```bash
docker build --tag parallel-you:bootstrap .
```

The container runs `ParallelYou.Host.dll` on port 8080 as the .NET application user. Mount the consumer artifact read-only and a writable persistent authentication-key directory, and supply the same deployment settings shown above.

## Existing deployments

The host uses the scoped settings described above instead of the old unscoped `MongoDb`, `Redis`, and `Keycloak` credentials. Routes now live under `/parallel-you`. Before changing an existing deployment, verify its database, identity mapping, and stored Person IDs. Records previously published through the web host's shared ForgeCampus Librarian may be in that shared store. This repository update does not copy or migrate those records.
