# Identity: a second, read-only service account lists the staff

**Status:** Accepted (the repository owner's decision on #54, 2026-11-15). A pilot decision, not generated.

## Context

Staff assign a ticket to a colleague (#7). Typed as free text, the name was never checked against anyone. A picker, and a check that answers `422` for a name that is not a staff member's, need the members of the realm role `staff`, and only the identity module talks to Keycloak (ADR-0052). It does so as `fabric-pilot-user-admin`, whose `manage-users` ADR-0052 plans to confine to `/tenants/*` with fine-grained admin permissions; staff are in no tenant, so that client is the wrong one to read them.

The tickets module must not reference the identity module (`ModulesDoNotReferenceEachOther`).

## Decision

- The identity module lists the staff as a second confidential client, `fabric-pilot-staff-reader`, with service accounts only and a token of its own. Its secret is the AppHost's generated parameter `keycloak-staff-reader-secret`, substituted into the realm import like the user admin's; a deployed realm sets it up by hand (`infra/keycloak-user-admin.md`).
- Its service account holds realm-management's **`query-users` and `view-realm`, and nothing else**. Keycloak 26.6 lists a realm role's members (`GET /admin/realms/{realm}/roles/staff/users`) only to a caller that may both query users and view the role, and viewing a realm role is `view-realm`. The issue's guess, `view-users` / `query-users`, is refused: `IdentityStaffReaderClientTests` checks that `query-users`, `view-realm` and `view-users` (which includes `query-users`) each alone answer `403`, that the client holds exactly the two roles, and that it cannot create a user. Both roles are read-only; `view-realm` also lets it read the realm's settings.
- Other modules ask through a contract interface, `IStaffDirectory` with `StaffMember` in `src/Contracts/Identity/`, which the identity module registers: the in-process call ADR-0005 provides for. An integration event cannot answer a question asked during a request, and Keycloak announces no role changes a projection could follow.
- The tickets module checks an assignee through it, after finding the ticket and before changing it: `422 tickets.assignee-not-staff` for any other name, blank included.
- The picker reads `GET /identity/staff` (paged; policy `identity.list-staff`, role `staff`): staff are users, the identity module's data.

## Consequences

- Each assignment and each list asks Keycloak for the role's members, at most the first 1,000 (a warning is logged at the cut); nothing is cached, so a role granted or removed counts at once.
- Assignments already made by free text stay as they were; only new ones are checked.
- The ticket keeps the user name, not the Keycloak id: a renamed or removed staff member's tickets keep the old name.
- The alternatives were weighed and not taken: listing every user with `view-users` and reading each one's roles (one call per user of every tenant), or a `/staff` group whose members would have to be kept in step with the role.
- Should the user admin be confined to `/tenants/*` (ADR-0052), nothing here changes.
