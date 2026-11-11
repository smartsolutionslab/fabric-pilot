import type { ReactNode } from 'react';
import { useHasClaim } from './useHasClaim';
import { useHasRole } from './useHasRole';

// A role, a claim, or both; both together means the user needs both.
type Requirement = { role: string; claim?: string; value?: string } | { role?: string; claim: string; value?: string };

type RequirePermissionProps = Requirement & {
  /** What to render instead; nothing by default. */
  fallback?: ReactNode;
  children: ReactNode;
};

/**
 * Renders its children only for a user whose ID token meets the requirement, else the fallback. Hiding an element is a
 * convenience, never protection: the backend policy decides.
 */
export function RequirePermission({ role, claim, value, fallback = null, children }: RequirePermissionProps) {
  const hasRole = useHasRole(role ?? '');
  const hasClaim = useHasClaim(claim ?? '', value);
  const allowed = (role === undefined || hasRole) && (claim === undefined || hasClaim);
  return <>{allowed ? children : fallback}</>;
}
