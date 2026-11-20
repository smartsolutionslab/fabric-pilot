# Deployment: the data stores in a private network, Keycloak's admin console by address

**Status:** Accepted (#65, which asks for the choice to be recorded; the owner accepted the open demo networking on 2026-11-15 only until a real customer joins). A pilot decision, not generated.

## Context

The first Azure deployment (#62) kept networking simple: Azure Database for PostgreSQL admitted every Azure address ("Allow Azure services"), Key Vault and Storage answered from the internet, protected only by passwords, RBAC and TLS, and Keycloak's admin console (`/admin`) was public on Keycloak's own address. Before the second real customer, the data stores must be reachable only from the Container Apps environment, and the admin console not from the internet (#65).

Keycloak's address cannot simply be closed: customers' browsers sign in at `/realms/...` and load `/resources/...` there. Container Apps restricts addresses per app, not per path, so its own IP restrictions would lock customers out too. Keycloak's `hostname-admin` only changes the address the console links to; it does not stop the console answering on the public one (Keycloak's reverse-proxy guide leaves blocking `/admin` to the proxy). And the host itself calls Keycloak's admin API (`/admin/realms/...`, ADR-0052, ADR-0053), so `/admin` must stay open to the environment.

## Decision

- **A virtual network.** The Container Apps environment runs in its `apps` subnet (delegated to `Microsoft.App/environments`, workload-profiles environment, consumption profile) with a public ingress for the web app and Keycloak; host, broker and migrations stay internal as before.
- **Private endpoints** in a second subnet for PostgreSQL (`postgresqlServer`), Key Vault (`vault`) and Storage (`blob`), each with its `privatelink.*` DNS zone linked to the network (`infra/private-endpoint.bicep`). All three have public network access **disabled**; the PostgreSQL firewall rule is gone. Inside the network the usual names resolve to the endpoints, so connection strings, the blob endpoint and Key Vault references are unchanged, and the identity reaches Key Vault and the blobs exactly as before. PostgreSQL keeps the public-access server it was created with: a private endpoint can be added to it in place, which VNet integration (a delegated subnet) cannot.
- **Keycloak's admin console by address.** Keycloak's ingress reaches an nginx beside Keycloak in its container app (`infra/keycloak/proxy.conf`), which passes everything through but `/admin`. `/admin` answers only private addresses, that is callers inside the environment (the host), and the CIDR ranges the deployment lists in `KEYCLOAK_ADMIN_ADDRESSES` (empty by default: nobody outside). The caller's address is the last one outside the private ranges in `X-Forwarded-For`, which the Container Apps ingress appends to; an address a caller puts there themselves comes before it and is not trusted.
- `infra/check.sh` fails the build when the environment leaves its subnet, a data store is public again, a private endpoint is missing, or the proxy stops denying `/admin` to every address it does not list.

## Consequences

- The owner administers Keycloak only from a listed address; `infra/keycloak/create-realm.sh` too. Nobody reads Key Vault from outside: the script reads the generated secrets from azd's own copy (`azd env config get infra.parameters.<name>`), and the deployer's Key Vault role is gone.
- An existing deployment cannot move its environment into a subnet: the apps, the job and the environment are deleted and created again (`infra/README.md`, "Moving a deployment into the network"). PostgreSQL, Key Vault, Storage and the data in them stay. The new environment has a new default domain, so the web app's and Keycloak's addresses change, and the realm's `fabric-pilot-web` client needs the new redirect URI.
- The guard on `/admin` depends on the Container Apps ingress appending the caller to `X-Forwarded-For`, which Keycloak's `KC_PROXY_HEADERS=xforwarded` already relies on. The runbook's check after each provision is that `/admin/` answers `403` from an unlisted address.
- Not part of this decision, and still public: Azure OpenAI (key authentication, docs/adr/fabric/backend-llm-azure-openai.md) and the container registry (Basic has no private endpoints). Each is its own decision if a customer asks for it.
- The alternatives were weighed and not taken: Container Apps IP restrictions on Keycloak (locks customers out of sign-in), `hostname-admin` alone (does not close the public address), and serving Keycloak's public paths through the web app's nginx with Keycloak internal (changes the issuer, the SPA's authority and the realm's redirect URIs, and still needs an address rule for the owner).
