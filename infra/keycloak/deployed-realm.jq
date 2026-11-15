# The deployed realm, made from the local one (src/AppHost/realms/realm.json) so the two cannot drift: the same roles,
# clients, mappers, user profile and service accounts, but none of the local users or their passwords, no Mailpit, the
# deployed web app's address instead of localhost, the deployment's tenants as groups, and no password grant.
#
# Arguments: --arg web <WEB_URL> --arg tenants <comma-separated>; the two client secrets come from the environment
# (USER_ADMIN_SECRET, STAFF_READER_SECRET), never from the command line.
def required(name): ($ENV[name] // "") | if . == "" then error("\(name) is not set") else . end;

del(.smtpServer)
| .users |= map(select(.serviceAccountClientId != null))
| .groups = [{
    name: "tenants",
    subGroups: ($tenants | split(",") | map(gsub("^\\s+|\\s+$"; "")) | map(select(. != "")) | map({ name: . }))
  }]
| .clients |= map(
    if .clientId == "fabric-pilot-web" then
      .redirectUris = [$web + "/*"] | .webOrigins = [$web] | .directAccessGrantsEnabled = false
    elif .clientId == "fabric-pilot-user-admin" then .secret = required("USER_ADMIN_SECRET")
    elif .clientId == "fabric-pilot-staff-reader" then .secret = required("STAFF_READER_SECRET")
    else . end)
| if (.groups[0].subGroups | length) == 0 then error("no tenant given") else . end
| if (tostring | test("\\$\\{[A-Z0-9_]+}")) then error("an environment placeholder of the local realm is left: give it a value here") else . end
| if any(.users[]; has("credentials")) then error("a user with a password is left") else . end
