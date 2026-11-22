# Deployment: Azure Container Apps with azd and hand-written Bicep

**Status:** Accepted. It records what the pilot built by hand for the charter's "Where it runs" (#62, PR #64; #66, PR #73; #65, PR #87), as the owner's decision X2 on software-fabric#602 (2026-11-21) asks (#90). A pilot decision, not generated. The network hardening is docs/adr/0055 and is not repeated here. No software-fabric ADR covers deployment yet; this one is the pilot's record for the `deploy.target` dimension M5 plans.

## Context

The charter's "Where it runs": Azure Container Apps, "deployed from the Aspire AppHost with hand-written IaC kept in this repository (`azd` and Bicep under `infra/`)", with Azure Database for PostgreSQL, Key Vault, Blob Storage and the Azure OpenAI deployment. The generator produced no deployment code in M4; the charter names `deploy.target` as an M5 candidate. The M4 gate needs the portal deployed and used by the demo tenant.

The owner decided on #62 (2026-11-15) that an agent writes the IaC and validates it without Azure access, and the owner runs `azd up` with their own subscription. Why hand-written Bicep rather than the Bicep azd or Aspire can generate from the AppHost is not recorded beyond the charter's wording.

## What was built

- **`azure.yaml`**: three azd services, each deployed to the one resource tagged with its name. `migrations` (`src/MigrationRunner`) is a Container Apps job that a postdeploy hook starts and waits for (`infra/scripts/run-migrations.sh`); `host` (`src/Host`) is deployed after it (`uses: [migrations]`), the AppHost's `WaitForCompletion(migrations)`; `web` after the host. The .NET services are built by the SDK's container publish and the web app in the registry (`remoteBuild`), so the owner needs no Docker.
- **`infra/main.bicep`** (subscription scope, one resource group `rg-<environment>`) and **`infra/resources.bicep`**. Every resource mirrors one the AppHost declares:

  | AppHost | Deployed as |
  | --- | --- |
  | `host` | Container App, internal ingress only, reached through the web app's `/api` |
  | `migrations` | Container Apps job, run to completion after each deploy, before the host |
  | `web` | Container App, external: the SPA built with the deployed realm's authority, served by nginx, which forwards `/api/<path>` to the host as `/<path>`, as Vite does locally (`infra/web/`) |
  | `keycloak` | Container App, external, `quay.io/keycloak/keycloak:26.6` in production mode (`start`), its own database `keycloak`, behind the environment's TLS (`KC_HTTP_ENABLED`, `KC_PROXY_HEADERS=xforwarded`), with an nginx beside it for `/admin` (docs/adr/0055) |
  | `postgres` / `database` | Azure Database for PostgreSQL Flexible Server, Burstable B1ms, PostgreSQL 17 (`postgresVersion`), `azure.extensions = VECTOR` |
  | `storage` / `blobs` | Storage account without shared keys (docs/adr/0057) |
  | `messaging` | Container App `rabbitmq:4.3`, TCP inside the environment, one replica, no volume |
  | `mailpit` | not deployed; SMTP is a parameter (docs/adr/0056) |
  | `azure-openai-*` | Azure OpenAI resource with the deployments `gpt-5-mini` and `text-embedding-3-small`, `GlobalStandard`, region separable (`openAiLocation`) |

  Besides: a Key Vault (RBAC), a container registry, a Log Analytics workspace, the Container Apps environment, and **one user-assigned managed identity** every app runs as, which may pull images, read the vault's secrets and read and write blobs, nothing else. Every app runs one replica.
- **Settings by the AppHost's names.** The host gets every setting the AppHost gives it, under the same name (`hostSettings` in `infra/resources.bicep`); `TENANTS` becomes `Tenancy__Tenants__<n>`. `Notifications__AllowUnencryptedSmtp` is the one exception, local Mailpit only.
- **Secrets.** Key Vault holds every secret parameter the AppHost declares, under the parameter's name, plus the PostgreSQL and RabbitMQ passwords and the two connection strings that carry them; the apps read them as Key Vault references through the identity. azd generates the passwords and client secrets on the first provision (`@metadata azd generate` in `infra/main.bicep`) and keeps them in the azd environment; the model's key is copied from the Azure OpenAI resource inside the deployment. The only secret the owner types is the SMTP password (`SMTP_PASSWORD`, docs/adr/0056).
- **The deployed realm.** `infra/keycloak/create-realm.sh` creates the realm once, through Keycloak's admin API, from the local `src/AppHost/realms/realm.json` passed through `infra/keycloak/deployed-realm.jq`: no users but the service accounts, no Mailpit SMTP, no password grant on `fabric-pilot-web`, redirect URIs at the web app, a group `/tenants/<tenant>` per tenant, and the client secrets azd generated. It leaves an existing realm alone. `infra/keycloak-user-admin.md` describes the same clients by hand.
- **The runbook**, `infra/README.md`: what to install, the values azd cannot know (`SMTP_CONNECTION`, `NOTIFICATIONS_SENDER_ADDRESS`, `TENANTS`, `KEYCLOAK_ADMIN_ADDRESSES`, optionally `SMTP_USER_NAME` and `SMTP_PASSWORD`), `azd up`, the realm, the demo tenant's first users, and later changes.
- **The hardening** (#65, PR #87): a virtual network, private endpoints for PostgreSQL, Key Vault and Storage with public access disabled, and Keycloak's `/admin` only to the environment and listed addresses (`infra/private-endpoint.bicep`, `infra/keycloak/proxy.conf`). The decision and its reasons are docs/adr/0055.
- **The offline check**, `infra/check.sh`, run by `.github/workflows/infra.yml` on every pull request and push to `main`, with the Bicep CLI pinned to v0.38.33:
  1. every Bicep file builds without errors or warnings;
  2. every `azure.yaml` service has its project and exactly one resource tagged with its name;
  3. every secret parameter the AppHost declares is a Key Vault secret of the same name;
  4. every setting the AppHost gives the host is set on the deployed host, but the listed exception;
  5. the deployed realm can be made from the local one;
  6. the environment is in a subnet, the data stores are private, and the proxy denies `/admin` to every address it does not list (docs/adr/0055).

## Why

- **An agent writes, the owner deploys:** the owner's decision on #62. Agents have no Azure access, so every check is one that runs without it.
- **The AppHost stays the description:** the runbook's first paragraph. Checks 3 and 4 read `src/AppHost/AppHost.cs` with `grep` and compare it with the compiled template, so a setting or secret the AppHost adds and `infra/` lacks fails CI.
- **`create-realm.sh`:** ADR-0052 and ADR-0053 say a deployed realm is not imported. The owner accepted the script on PR #64, reading "set up by hand" as no automatic `--import-realm` of the local file, which carries the local users' passwords.
- **Open networking first, closed second:** the owner accepted public PostgreSQL ("Allow Azure services") and a public admin console for the demo tenant on PR #64, with hardening before the second real customer (#65, docs/adr/0055).
- **The rest are PR #64's assumptions**, listed there as the simplest option where the charter, the ADRs and the issue left a choice: Keycloak as the stock image in production mode on the same server; `KeycloakAdmin__BaseAddress` as Keycloak's public address, so issuer and admin API agree; PostgreSQL 17 because 18's availability on Flexible Server was uncertain; RabbitMQ deployed although #62 did not list it, without a volume, since Wolverine's outbox and inbox are in PostgreSQL; one managed identity rather than one per app; one replica each. Azure OpenAI is provisioned here although docs/adr/fabric/backend-llm-azure-openai.md says the project does not provision it, because that describes the generated code, which still takes endpoint, deployment and key as settings.

## What it taught

- **Validated offline is not deployed.** PR #64 and PR #87 were checked only by `infra/check.sh`, a Bicep build and, for the proxy, a local nginx; both list what only Azure can show (PR #87: that the ingress appends the caller to `X-Forwarded-For`, that Key Vault references resolve through the private endpoint, that an existing server accepts a private endpoint in place). The runbook's "Moving a deployment into the network" speaks of "the first demo" made before docs/adr/0055; when `azd up` ran, and whether the hardened version has been provisioned, is not recorded in this repository.
- **A check that compares the IaC with the AppHost earns its keep.** When #66 added the SMTP sign-in to the AppHost, checks 3 and 4 required the new secret and the two settings in `infra/` without any change to `infra/check.sh` (PR #73); #65 added check 6 in a test commit before the change (8ace036). Reading the AppHost with `grep` is fragile; check 3 fails when its patterns stop matching rather than passing empty.
- **Deploying surfaced what local runs hid.** Writing `infra/` found that the e-mail sender could not sign in to a real SMTP server (#66, docs/adr/0056); local Mailpit needs no sign-in.
- **Development-only defaults bite when deployed.** `MapDefaultEndpoints` maps `/health` only in Development, so the deployed host has Container Apps' default TCP probe, not an HTTP health check (PR #64). Keycloak gets HTTP probes on its management port.
- **Some choices cannot be changed in place.** A Container Apps environment cannot join a subnet after it is created, so moving into the network deletes and recreates the apps, the job and the environment, and changes the public addresses (docs/adr/0055; `infra/README.md`).
- **Container Apps restricts by app, not by path.** Closing Keycloak's admin console without locking customers out of sign-in needed a proxy beside it (docs/adr/0055).

## Consequences

- The deployment is the owner's to run (`azd up`, `azd provision`, `azd deploy`), from `infra/README.md`.
- This is the pilot's record for `deploy.target`, which software-fabric plans for M5 on Aspire's publisher (software-fabric ADR-0020's Consequences, "D2, M5-07"). software-fabric has no deployment ADR yet; when it does, it should cite this one and docs/adr/0055.
- Still public, each its own decision if a customer asks: Azure OpenAI (key authentication) and the container registry (docs/adr/0055).
