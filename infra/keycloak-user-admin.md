# Keycloak: the identity module's service accounts in a deployed realm

The local realm (`src/AppHost/realms/realm.json`) is imported on first start and is for development only. A deployed realm is not imported, so the two confidential clients the `identity` module uses are set up there by hand, once per deployment, and kept in step with the local ones: `fabric-pilot-user-admin`, which manages a tenant's users (#14, docs/adr/0052), and `fabric-pilot-staff-reader`, which only lists the staff (#54, docs/adr/0053).

## In the deployed realm

1. **Clients → Create client**: client ID `fabric-pilot-user-admin`, OpenID Connect.
   - Client authentication: **on** (confidential). Authentication flow: **Service accounts roles** only; standard flow, direct access grants and implicit flow off.
2. **Credentials** tab: copy the client secret (or regenerate it). It goes into the secret store below, nowhere else.
3. **Service account roles** tab → Assign role → filter by clients → `realm-management` → **`manage-users`**, and nothing else. In particular not `realm-admin`, `manage-realm`, `manage-clients`, `view-realm`, `view-users` or `query-groups`: `manage-users` alone permits every call the module makes, and the integration tests check that the local client holds exactly that.
4. **Realm settings → Events → Admin events settings**: save events **on**, so Keycloak records each change the service account makes. The host logs who asked for it.
5. **Realm settings → Email**: the realm's real SMTP server (ADR-0015 already requires it), so invitations arrive. Invitation links expire after Keycloak's execute-actions lifespan (12 hours by default); an admin can have one sent again (`PUT /identity/users/{userId}/invitation`).
6. The realm must have the roles `customer-user` and `customer-admin` (a composite including `customer-user`) and a group `/tenants/<tenant>` per tenant; the module creates neither.

## The staff reader, in the deployed realm

1. **Clients → Create client**: client ID `fabric-pilot-staff-reader`, OpenID Connect, client authentication **on**, **Service accounts roles** only, as above.
2. **Credentials** tab: copy the client secret into the secret store below.
3. **Service account roles** tab → Assign role → filter by clients → `realm-management` → **`query-users`** and **`view-realm`**, and nothing else. Listing the members of the realm role `staff` needs both; `view-users` alone, or `query-users` alone, is refused (the integration tests check that the local client holds exactly these two). Never `manage-users`: this client only reads.
4. The realm must have the realm role `staff`; every user holding it is offered as an assignee, and only they may be assigned a ticket.

## In the deployment

- The secrets: the AppHost parameters `keycloak-user-admin-secret` and `keycloak-staff-reader-secret`, from the deployment's secret store (Key Vault). Never in `appsettings*.json`, a pipeline variable in clear, or the repository.
- The host's settings the AppHost sets locally: `KeycloakAdmin:BaseAddress` (the deployed Keycloak's internal URL, the one the host reaches it by), `KeycloakAdmin:Realm` (`fabric-pilot`), `KeycloakAdmin:ClientId` (`fabric-pilot-user-admin`), `KeycloakAdmin:ClientSecret` (the first parameter), `KeycloakAdmin:StaffReaderClientId` (`fabric-pilot-staff-reader`) and `KeycloakAdmin:StaffReaderClientSecret` (the second).

## Rotating a secret

Either client's the same way: regenerate it on the client's Credentials tab, update the secret store, and restart the host. Tokens already minted stay valid until they expire (5 minutes by default); the host mints a new one with the new secret on its next call after that.

## Locally

Nothing to do: the AppHost generates both secrets once, keeps them in its user secrets and passes them to Keycloak, whose realm import substitutes them into the clients. A Keycloak volume created before a client existed keeps the old realm, which is never imported again: remove the volume once (ADR-0015).
