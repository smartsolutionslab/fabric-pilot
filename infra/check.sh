#!/usr/bin/env bash
# Checks the deployment without Azure: bash infra/check.sh (CI runs it; BICEP names the Bicep CLI, `bicep` by default).
#   1. Every Bicep file under infra/ builds with no error and no warning.
#   2. Every service in azure.yaml has its project and a resource tagged with its name to deploy to.
#   3. Every secret parameter the AppHost declares is a Key Vault secret of the same name.
#   4. Every setting the AppHost gives the host is set on the deployed host, but those listed below with their reason.
#   5. The deployed realm can be made from the local one (keycloak/deployed-realm.jq and its own checks).
#   6. Nothing but the web app and Keycloak's public paths is reachable from the internet: the environment runs in a
#      subnet, PostgreSQL, Key Vault and Storage only through private endpoints, and keycloak/proxy.conf keeps /admin.
set -euo pipefail

cd "$(dirname "$0")/.."
bicep="${BICEP:-bicep}"
apphost=src/AppHost/AppHost.cs
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

failures=0
fail() {
  echo "FAIL: $*" >&2
  failures=$((failures + 1))
}

# The AppHost's settings for the host that a deployment does not set, each with its reason.
declare -A not_deployed=(
  [Notifications__AllowUnencryptedSmtp]="only the local Mailpit may go unencrypted (SmtpSettings)"
)

# 1
while IFS= read -r file; do
  if ! diagnostics="$("$bicep" build "$file" --outfile "$work/$(basename "$file" .bicep).json" 2>&1)"; then
    fail "$file does not build: $diagnostics"
  elif [ -n "$diagnostics" ]; then
    fail "$file builds with diagnostics: $diagnostics"
  fi
done < <(find infra -name '*.bicep' | sort)
echo "1. Bicep: checked $(find infra -name '*.bicep' | sort | tr '\n' ' ')"

template="$work/resources.json"
[ -f "$template" ] || { echo "FAIL: infra/resources.bicep did not build; nothing more to check." >&2; exit 1; }

# 2
while IFS= read -r service; do
  project="$(yq -r ".services.\"$service\".project" azure.yaml)"
  [ -d "$project" ] || fail "azure.yaml: service $service names the project $project, which does not exist."
  dockerfile="$(yq -r ".services.\"$service\".docker.path // \"\"" azure.yaml)"
  if [ -n "$dockerfile" ] && [ ! -f "$project/$dockerfile" ]; then
    fail "azure.yaml: service $service names the Dockerfile $project/$dockerfile, which does not exist."
  fi
  tagged="$(jq --arg service "$service" '[.resources[] | select((.tags // "" | tostring) | contains("createObject('"'"'azd-service-name'"'"', '"'"'\($service)'"'"')"))] | length' "$template")"
  [ "$tagged" = 1 ] || fail "azure.yaml: service $service has $tagged resources tagged azd-service-name: $service; it needs exactly one."
done < <(yq -r '.services | keys | .[]' azure.yaml)
echo "2. azure.yaml: services $(yq -r '.services | keys | .[]' azure.yaml | tr '\n' ' ')checked."

# 3
secrets="$(jq -r '.resources[] | select(.type == "Microsoft.KeyVault/vaults/secrets") | .name' "$template" | sed -E "s/.*'([^']+)'\)\]$/\1/")"
declared="$( (grep -oE 'AddParameter\("[a-z0-9-]+"[^;]*secret: true' "$apphost" | grep -oE '^AddParameter\("[a-z0-9-]+"' | cut -d'"' -f2
  grep -oE 'CreateDefaultPasswordParameter\(builder, "[a-z0-9-]+"' "$apphost" | cut -d'"' -f2) | sort -u)"
[ -n "$declared" ] || fail "$apphost: no secret parameter found; the patterns in this check no longer match it."
for parameter in $declared; do
  grep -qx -- "$parameter" <<<"$secrets" || fail "The AppHost's secret parameter $parameter is not a Key Vault secret in infra/resources.bicep."
done
echo "3. Key Vault: checked the secret parameters $(tr '\n' ' ' <<<"$declared")"

# 4
host_env="$(jq -r '.resources[] | select(.type == "Microsoft.App/containerApps" and .name == "host") | .properties.template.containers[0].env' "$template")"
deployed="$(grep -oE "createObject\('name', '[^']+'" <<<"$host_env" | sed -E "s/.*'([^']+)'$/\1/" | sort -u)"
expected="$( (grep -oE 'WithEnvironment\("[A-Za-z]+__[A-Za-z_]+"' "$apphost" | cut -d'"' -f2
  sed -n '/AddProject<Projects.Host>/,/;$/p' "$apphost" | grep -oE 'WithReference\((database|messaging|blobs)\)' | sed -E 's/WithReference\((.*)\)/ConnectionStrings__\1/') | sort -u)"
for setting in $expected; do
  if [ -n "${not_deployed[$setting]:-}" ]; then
    echo "   $setting: not deployed, ${not_deployed[$setting]}."
    continue
  fi
  grep -qx -- "$setting" <<<"$deployed" || fail "The AppHost gives the host $setting; infra/resources.bicep does not."
done
if grep -q 'WithTenants(' "$apphost"; then
  jq -e '.variables.copy[]? | select(.name == "tenantSettings") | .input.name | contains("Tenancy__Tenants__")' "$template" >/dev/null \
    || fail "The AppHost gives the host Tenancy__Tenants__<n>; infra/resources.bicep does not."
fi
echo "4. Host: checked $(wc -w <<<"$expected") settings and Tenancy__Tenants__<n>."

# 5
if ! USER_ADMIN_SECRET=check STAFF_READER_SECRET=check jq --arg web https://web.example.org --arg tenants demo \
  -f infra/keycloak/deployed-realm.jq src/AppHost/realms/realm.json >/dev/null; then
  fail "infra/keycloak/deployed-realm.jq cannot make the deployed realm from src/AppHost/realms/realm.json."
fi
echo "5. Keycloak: checked the deployed realm."

# 6
resource() {
  jq -c --arg type "$1" '[.resources[] | select(.type == $type)]' "$template"
}
[ "$(resource Microsoft.App/managedEnvironments | jq '[.[] | select(.properties.vnetConfiguration.infrastructureSubnetId != null)] | length')" = 1 ] \
  || fail "The Container Apps environment does not run in a subnet of a virtual network (vnetConfiguration)."
for type in Microsoft.DBforPostgreSQL/flexibleServers Microsoft.KeyVault/vaults Microsoft.Storage/storageAccounts; do
  open="$(resource "$type" | jq '[.[] | select((.properties.publicNetworkAccess // .properties.network.publicNetworkAccess) != "Disabled")] | length')"
  [ "$open" = 0 ] || fail "$type is reachable from the internet: publicNetworkAccess is not Disabled."
done
[ "$(resource Microsoft.DBforPostgreSQL/flexibleServers/firewallRules | jq length)" = 0 ] \
  || fail "PostgreSQL has a firewall rule; it is reached through its private endpoint only."
endpoints="$(jq -r '.resources[] | select(.type == "Microsoft.Resources/deployments") | .properties.parameters.groupId.value // empty' "$template" | sort)"
for group in blob postgresqlServer vault; do
  grep -qx -- "$group" <<<"$endpoints" || fail "No private endpoint (infra/private-endpoint.bicep) for the group $group."
done
proxy=infra/keycloak/proxy.conf
keycloak_port="$(resource Microsoft.App/containerApps | jq '.[] | select(.name == "keycloak") | .properties.configuration.ingress.targetPort')"
if [ ! -f "$proxy" ]; then
  fail "$proxy does not exist: nothing keeps Keycloak's admin console (/admin) from the internet."
else
  grep -qE "^[[:space:]]*listen $keycloak_port;" "$proxy" \
    || fail "Keycloak's ingress targets port $keycloak_port, which is not the port $proxy listens on."
  sed -n '/location \^~ \/admin {/,/}/p' "$proxy" | grep -qE '^[[:space:]]*deny all;' \
    || fail "$proxy does not deny /admin to every address it does not list (location ^~ /admin { ... deny all; })."
fi
echo "6. Network: checked the environment's subnet, private PostgreSQL, Key Vault and Storage, and Keycloak's /admin."

if [ "$failures" -gt 0 ]; then
  echo "$failures check(s) failed." >&2
  exit 1
fi
echo "All checks passed."
