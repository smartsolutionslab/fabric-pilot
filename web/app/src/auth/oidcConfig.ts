import type { AuthProviderProps } from 'react-oidc-context';

export const oidcConfig = {
  authority: import.meta.env.VITE_OIDC_AUTHORITY ?? 'http://localhost:8080/realms/fabric-pilot',
  client_id: 'fabric-pilot-web',
  redirect_uri: window.location.origin,
  post_logout_redirect_uri: window.location.origin,
  // Drop ?code=...&state=... from the address once the sign-in has completed.
  onSigninCallback: () => window.history.replaceState({}, document.title, window.location.pathname),
} satisfies AuthProviderProps;
