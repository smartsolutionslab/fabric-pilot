#!/usr/bin/env bash
# Creates the realm in the deployed Keycloak, once, after the first `azd up`: the local realm's roles, clients, mappers,
# user profile and service accounts (deployed-realm.jq), with the client secrets Key Vault holds for the host. It
# creates no users and imports nothing on Keycloak's start (docs/adr/0052). An existing realm is left as it is.
#
# Needs: azd (signed in, the environment selected), az (signed in), jq, curl. Run from anywhere:
#   bash infra/keycloak/create-realm.sh
# No secret is written to disk or passed on a command line: they go from Key Vault to jq's environment and curl's stdin.
set -euo pipefail

cd "$(dirname "$0")/../.."
eval "$(azd env get-values)"
: "${AZURE_KEY_VAULT_NAME:?not in the azd environment: run azd provision first}"
: "${KEYCLOAK_URL:?not in the azd environment: run azd provision first}"
: "${WEB_URL:?not in the azd environment: run azd provision first}"
tenants="${TENANTS:-demo}"

secret() {
  az keyvault secret show --vault-name "$AZURE_KEY_VAULT_NAME" --name "$1" --query value --output tsv | tr -d '\r\n'
}

echo "Signing in to $KEYCLOAK_URL as admin ..."
token=$(secret keycloak-password \
  | curl --silent --show-error --fail-with-body "$KEYCLOAK_URL/realms/master/protocol/openid-connect/token" \
      --data grant_type=password --data client_id=admin-cli --data username=admin --data-urlencode password@- \
  | jq -r .access_token)

realm=$(USER_ADMIN_SECRET=$(secret keycloak-user-admin-secret) STAFF_READER_SECRET=$(secret keycloak-staff-reader-secret) \
  jq --arg web "$WEB_URL" --arg tenants "$tenants" -f infra/keycloak/deployed-realm.jq src/AppHost/realms/realm.json)

response=$(printf '%s' "$realm" \
  | curl --silent --show-error --write-out '\n%{http_code}' "$KEYCLOAK_URL/admin/realms" \
      --header @<(printf 'Authorization: Bearer %s\n' "$token") --header 'Content-Type: application/json' --data-binary @-)
status="${response##*$'\n'}"

case "$status" in
  201) echo "Created the realm fabric-pilot with the tenant groups: $tenants." ;;
  409) echo "The realm fabric-pilot exists already; nothing changed. Change it in the admin console." ;;
  *) echo "Keycloak answered $status, the realm was not created: ${response%$'\n'*}" >&2; exit 1 ;;
esac

cat <<NEXT

Next, in the admin console ($KEYCLOAK_URL/admin/master/console/#/fabric-pilot), as infra/README.md describes:
  1. Realm settings > Email: the realm's SMTP server.
  2. Users: the first staff member (realm role staff) and the demo tenant's admin (realm role customer-admin,
     group /tenants/<tenant>), each with Credentials > Reset password (temporary) or Send email > Update Password.
NEXT
