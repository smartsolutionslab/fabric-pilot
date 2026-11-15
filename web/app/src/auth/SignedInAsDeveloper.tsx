import type { ReactNode } from 'react';
import { AuthContext, type AuthContextProps } from 'react-oidc-context';

// The local user developer as signed in, with the ID token it has locally.
const developer = {
  user: {
    profile: {
      preferred_username: 'developer',
      realm_access: { roles: ['manager'] },
      department: 'finance',
    },
  },
  isAuthenticated: true,
  isLoading: false,
} as unknown as AuthContextProps;

/**
 * Renders its children for the local user developer, signed in, without the sign-in provider: for a test that renders a
 * screen on its own, as each screen's axe check does.
 */
export function SignedInAsDeveloper({ children }: { children: ReactNode }) {
  return <AuthContext.Provider value={developer}>{children}</AuthContext.Provider>;
}
