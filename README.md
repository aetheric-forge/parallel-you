# Parallel You

Parallel You is a private, actor-scoped record of observation, reflection, and direction. It preserves earlier representations instead of overwriting them, allowing a person to see what changed without pretending the current view was always inevitable.

The 1.0 workflow includes:

- capture and durable retrieval;
- reflection with evidence and adopted insight;
- observation tracking with preserved history;
- intentions and plans with explicit adoption;
- recommendations with human response and no implied authorization to execute.

## Architecture

Parallel You is an ASP.NET Core Blazor Server application built on Aetheric Forge Runtime institutions and services.

- Keycloak provides OpenID Connect authentication.
- MongoDB is the durable archive and knowledge store.
- Redis is a recoverable cache for current-artifact pointers.
- Every domain artifact is scoped to the authenticated Forge identity and authority context.

The `runtime` directory is a Git submodule. Clone with submodules enabled:

```bash
git clone --recurse-submodules https://github.com/aetheric-forge/parallel-you.git
```

For an existing clone:

```bash
git submodule update --init --recursive
```

## Configuration

ASP.NET Core configuration supports JSON, environment variables, and user secrets. Environment-variable keys use double underscores, such as `MongoDb__Host`.

| Key | Required | Purpose |
| --- | --- | --- |
| `Keycloak:Authority` | yes | Realm authority URL used by OpenID Connect |
| `Keycloak:Realm` | yes | Keycloak realm used by Forge Authentication |
| `Keycloak:ClientId` | yes | OIDC client identifier |
| `Keycloak:ClientSecret` | yes | OIDC client secret |
| `MongoDb:Host` | yes | MongoDB host |
| `MongoDb:Port` | yes | MongoDB port |
| `MongoDb:Username` | yes | Application database user |
| `MongoDb:Password` | yes | Application database password |
| `MongoDb:DatabaseName` | yes | Archive and knowledge database |
| `MongoDb:AuthenticationDatabase` | yes | MongoDB authentication source |
| `MongoDb:DirectConnection` | no | Direct topology selection; defaults to `true` |
| `Redis:Host` | yes | Redis host |
| `Redis:Port` | no | Redis port; defaults to `6379` |
| `Redis:Password` | no | Redis password |
| `Redis:Ssl` | no | Enables TLS; defaults to `false` |
| `Redis:Database` | no | Logical database; defaults to `0` |

Keep passwords and client secrets outside committed configuration. For local development, use the web project's configured user-secrets store.

## Build and test

The application targets .NET 10.

```bash
dotnet restore ParallelYou.sln
dotnet test ParallelYou.sln --configuration Release
dotnet run --project ParallelYou.Web/ParallelYou.Web.csproj
```

The authenticated application is available at the URL reported by `dotnet run`. Operational endpoints are anonymous and intended for platform probes:

- `/health/live` reports process liveness.
- `/health/ready` verifies MongoDB and Redis connectivity.

## Container image

Build from the repository root so the runtime submodule is present in the build context:

```bash
docker build \
  --build-arg VERSION=1.0.0 \
  --tag ghcr.io/aetheric-forge/parallel-you:1.0.0 \
  --tag ghcr.io/aetheric-forge/parallel-you:latest \
  .
```

Push the immutable release tag and, if desired, the moving tag:

```bash
docker push ghcr.io/aetheric-forge/parallel-you:1.0.0
docker push ghcr.io/aetheric-forge/parallel-you:latest
```

The image:

- uses the official .NET 10 SDK and ASP.NET Core runtime images;
- publishes only the web application and its runtime dependencies;
- runs as the non-root user supplied by the Microsoft runtime image;
- listens for HTTP traffic on port `8080`.

## Deployment notes

When running behind an ingress or reverse proxy, enable forwarded headers so OIDC constructs the public HTTPS callback URL:

```text
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
```

Persist ASP.NET Core Data Protection keys at `/home/app/.aspnet/DataProtection-Keys`. All replicas must share the same key ring or authentication cookies will not survive pod replacement and cannot move between replicas.

Blazor Server circuits are instance-bound. Start with one replica or configure ingress session affinity before scaling horizontally.

Treat MongoDB as durable state and back it up accordingly. Redis may be cleared or replaced; Parallel You reconstructs current pointers from durable MongoDB history.
