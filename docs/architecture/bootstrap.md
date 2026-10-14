# Institution bootstrap and host contract

The institution owns `institution/parallel-you.yaml`. Deployment choices are in the sibling example bindings file. Parallel You owns its personal Library and inherits Registrar and Workbench from its selected parent. The example Mongo binding creates a database-scoped user through the existing provisioner.

## Provisioning boundary

The provisioning service loads the two YAML files at one resolved Git commit, verifies parent capabilities, reviews and executes the plan, and records the successful institution revision in its durable deployment catalog. The catalog contains deployment identity, source provenance, resource locations, secret references, and provisioning outcome. Pending or failed attempts are separate from successfully provisioned revisions.

Provisioning exports a private version-1 consumer JSON for module `parallel-you`, with the deployment ID, institution ID/version/name/description, environment, plan ID, and complete settings map. It delivers this to a private file or Kubernetes Secret. This module reads only that delivered artifact; it does not read provisioning's catalog, root credential store, or encryption keys.

The web module owns a wire-compatible consumer DTO. Tests serialize the provisioner's DTO and validate the result with the module loader so format drift fails validation without introducing a production dependency on the provisioning engine.

The settings map contains exactly:

- `ParallelYou:Enabled`;
- `ParallelYou:MongoDb:Host`, `Port`, `DatabaseName`, `Username`, `Password`, `AuthenticationDatabase`;
- `Workbench:Redis:Host`, `Port`, `Ssl`, `Database`, `User`, `Password`;
- `Registry:Keycloak:Authority`, `Realm`, `ClientId`, `ClientSecret`.

The loader requires the expected `ParallelYou:DeploymentId` and `ParallelYou:Environment`. Optional `ParallelYou:PlanId` constrains the plan. Missing, incomplete, extra, or mismatched artifacts fail startup before applying settings. An accepted artifact replaces the full consumer settings revision. The host requires a separate persistent `ParallelYou:ProtectionKeyDirectory`; this is a host setting, not part of the consumer artifact.

## Host composition

`ParallelYou.Services/ParallelYouModule.cs` composes the institution from an explicit context, Librarian, and Artificer. Its service registrations do not replace the host's shared capabilities. Each deployment must provide the intended isolated Library and scoped parent Workbench connection.

`ParallelYou.Web` provides reusable pages under `/parallel-you`. A consuming Blazor host registers cascading authentication state, module services and identity accessors, adds the component assembly to routing and endpoint discovery, and includes the library's static assets. Working pages require authorization; the landing page allows anonymous access.

`ParallelYou.Host` provides the reference standalone composition. It validates OIDC with the provisioned browser client, derives the identity from the validated `sub` claim, and replaces conflicting identity-mapping claims. Personal identity access resolves Person records through the module's own Library. It requires neither Registry administration roles nor a second campus authentication request.

The five preserved Workbench stages are ReflectionMapping, TrackingCurrent, IntentionCurrent, PlanCurrent, and RecommendationCurrent. The Mongo collection is `knowledge`; the provider scheme is `parallel-you`. A new Library does not automatically discover records from a previous shared Library.

## Revisions and readiness

Git loads provisioning inputs; implementation dependencies are pinned at build time. The definition loader does not load executable .NET assemblies. A successfully provisioned database/client/ACL grant does not establish host readiness: configuration must be delivered and the host restarted. `/health/ready` checks the selected Mongo and Redis connections; OIDC discovery and an actual browser sign-in should also be verified when deploying against a real identity provider.

This extraction does not change the consuming `aetheric-web` repository or deploy infrastructure. Its embedded copy can be replaced by a pinned dependency after this repository revision is reviewed.
