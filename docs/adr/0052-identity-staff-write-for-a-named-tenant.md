# Identity: staff manage a named tenant's admins, with the tenant from the route

**Status:** Accepted (the repository owner's decision on #14, 2026-11-13). A pilot decision, not generated.

## Context

A customer admin manages the users of their own tenant (#14). The `identity` module does it through Keycloak's admin API; Keycloak is its only store and it has no table. The tenant is the caller's, from their token (`ITenantContext`), as everywhere else (ADR-0018).

Staff must also manage any tenant's admins, for instance to give a new customer its first admin. Staff belong to no tenant, so the caller's token names none. The one existing way for staff to reach every tenant, `AcrossTenants()` (ADR-0018), opens `TenantScope.AllTenants()`, whose contract is that it **writes nothing**; its interceptor guards the module's DbContext, which a call to Keycloak never passes through. Using it for these writes would break what it means while passing its checks.

## Decision

Staff manage a tenant's admins on ordinary endpoints that name the tenant in the route:

- `GET`, `POST /identity/tenants/{tenantId}/admins` and `PUT /identity/tenants/{tenantId}/admins/{userId}/role`, under the policy `identity.manage-tenant-admins`, which requires `CrossTenantRole.Name` (`staff`).
- They are **not** mapped with `AcrossTenants()` or `WritesSharedRecords()`. Their handlers take the tenant from the route, check that its group `/tenants/{tenantId}` exists (`404 identity.tenant-not-found` otherwise, and for an id that could name another group), open no `TenantScope` and touch no DbContext.
- They grant only the tenant roles (`customer-user`, `customer-admin`), as a customer admin's endpoints do, and act only on users who are members of exactly that tenant's group and are not staff (`404 identity.user-not-found` for anyone else).

Customer admins use `/identity/users`, under `identity.manage-users` (`customer-admin`), with the tenant from their token only.

The module calls the admin API as the confidential client `fabric-pilot-user-admin`, whose service account holds realm-management's `manage-users` and no other role: the integration tests found that it alone permits every call the module makes (`IdentityUserAdminClientTests`). It can still map any realm role to any user, `staff` included, so the allow-list of tenant roles and the tenant check in the module's code are what confine it.

## Consequences

- "Staff write for one named tenant" is a shape of its own, next to `AcrossTenants()` (read every tenant) and `WritesSharedRecords()` (write what every tenant reads). No architecture rule checks it yet; review does. The generator should own it (a `.ForTenant()` endpoint filter opening `TenantScope.Begin(routeTenant)` after authorization, with a rule requiring `CrossTenantRole.Name`), so the next project does not invent it again.
- Should these endpoints ever need the module's DbContext, they must open `TenantScope.Begin(tenantId)` themselves first; today they need none.
- Keycloak's admin events name the service account, not the person: each handler logs who asked for a change (`IdentityAudit`), and the realm records admin events.
- Restricting the service account to `/tenants/*` and to the two tenant roles with Keycloak's fine-grained admin permissions is a follow-up, before the second real customer joins.
- Deployed realms are not imported: the client, its role and its secret are set up by hand (`infra/keycloak-user-admin.md`).
