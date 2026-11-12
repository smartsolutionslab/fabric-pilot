# Browser sign-in: oidc-client-ts

**Status:** Accepted (generated)

## Decision

The browser signs in with the authorization-code flow and PKCE through `oidc-client-ts`, wrapped by `react-oidc-context`. The whole app sits behind `SignedIn`: an anonymous visitor is sent to the identity provider. Every API call carries the user's access token, added in `src/api/baseApi.ts`.

The identity provider's address arrives as `VITE_OIDC_AUTHORITY` (the AppHost sets it); the client id is the realm's `<project>-web` client.

**Showing and hiding by role and claim.** With `backend.authorization` `[roles, claims]`, `src/auth/` has `useHasRole(role)`, `useHasClaim(type, value?)`, and `<RequirePermission role claim value fallback>`, which renders its children for a user who meets the requirement (both, when `role` and `claim` are given together) and the fallback, nothing by default, for anyone else. They read the ID token's claims (`useAuth().user.profile`) synchronously: no request, no token parsing. The Keycloak realm puts the realm roles (`realm_access.roles`, realm roles only, not the API client's roles) and `department` into the ID token for this. The source, the realm roles and the API were decided by the repository owner (2026-11-04).

Hiding is a convenience, never protection: the browser and its ID token are the user's to change, and the backend policy decides. A user who should not see a button gets a 403 from its endpoint whether or not the button is hidden. The ID token is read at sign-in, so a role or claim granted later shows once the user signs in again (or the token is renewed); the backend sees it on the next access token.

## Gates

`src/auth/RequirePermission.test.tsx` shows an element to `developer` and hides it from `visitor` (role `manager`), shows it to `analyst` and hides it from `clerk` (`department=finance`), and shows one requiring both only to `developer`, not to `clerk` (only the role) or `analyst` (only the claim), with `useAuth` mocked to the ID token claims of the realm's local users. It cannot see whether an endpoint behind a hidden element has a policy: that is the backend's gate.

## Consequences

- Tokens live in session storage: closing the tab signs the user out of the app, not out of the identity provider.
- Components read the user with `useAuth()`; nothing else talks to the identity provider.
