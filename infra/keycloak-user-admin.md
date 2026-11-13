# Keycloak: the identity module's service account in a deployed realm

The local realm (`src/AppHost/realms/realm.json`) is imported on first start and is for development only. A deployed realm is not imported, so the confidential client the `identity` module uses (#14, docs/adr/0052) is set up there by hand, once per deployment, and kept in step with the local one.

## In the deployed realm

1. **Clients → Create client**: client ID `fabric-pilot-user-admin`, OpenID Connect.
   - Client authentication: **on** (confidential). Authentication flow: **Service accounts roles** only; standard flow, direct access grants and implicit flow off.
2. **Credentials** tab: copy the client secret (or regenerate it). It goes into the secret store below, nowhere else.
3. **Service account roles** tab → Assign role → filter by clients → `realm-management` → **`manage-users`**, and nothing else. In particular not `realm-admin`, `manage-realm`, `manage-clients`, `view-realm`, `view-users` or `query-groups`: `manage-users` alone permits every call the module makes, and the integration tests check that the local client holds exactly that.
4. **Realm settings → Events → Admin events settings**: save events **on**, so Keycloak records each change the service account makes. The host logs who asked for it.
5. **Realm settings → Email**: the realm's real SMTP server (ADR-0015 already requires it), so invitations arrive. Invitation links expire after Keycloak's execute-actions lifespan (12 hours by default); an admin can have one sent again (`PUT /identity/users/{userId}/invitation`).
6. The realm must have the roles `customer-user` and `customer-admin` (a composite including `customer-user`) and a group `/tenants/<tenant>` per tenant; the module creates neither.

## In the deployment

- The secret: the AppHost parameter `keycloak-user-admin-secret`, from the deployment's secret store (Key Vault). Never in `appsettings*.json`, a pipeline variable in clear, or the repository.
- The host's settings the AppHost sets locally: `KeycloakAdmin:BaseAddress` (the deployed Keycloak's internal URL, the one the host reaches it by), `KeycloakAdmin:Realm` (`fabric-pilot`), `KeycloakAdmin:ClientId` (`fabric-pilot-user-admin`) and `KeycloakAdmin:ClientSecret` (the parameter above).

## Rotating the secret

Regenerate it on the client's Credentials tab, update the secret store, and restart the host. Tokens already minted stay valid until they expire (5 minutes by default); the host mints a new one with the new secret on its next call after that.

## Locally

Nothing to do: the AppHost generates the secret once, keeps it in its user secrets and passes it to Keycloak, whose realm import substitutes it into the client. A Keycloak volume created before this client existed keeps the old realm, which is never imported again: remove the volume once (ADR-0015).
