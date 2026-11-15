import { cleanup, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { useAuth } from 'react-oidc-context';
import { afterEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { NotificationsRoutes } from './NotificationsRoutes';

vi.mock('react-oidc-context', () => ({ useAuth: vi.fn() }));
vi.mock('./NotificationsPage', () => ({ NotificationsPage: () => <h1>My notices</h1> }));

afterEach(cleanup);

// The ID tokens Keycloak issues the realm's local users (src/AppHost/realms/realm.json).
const users = {
  developer: {
    preferred_username: 'developer',
    realm_access: { roles: ['manager', 'customer-admin', 'customer-user'] },
  },
  'developer-b': { preferred_username: 'developer-b', realm_access: { roles: ['manager', 'customer-user'] } },
  staff: { preferred_username: 'staff', realm_access: { roles: ['staff'] } },
  visitor: { preferred_username: 'visitor' },
};

function renderAs(user: keyof typeof users) {
  vi.mocked(useAuth).mockReturnValue({ user: { profile: users[user] } } as unknown as ReturnType<typeof useAuth>);
  return render(
    <MemoryRouter>
      <NotificationsRoutes />
    </MemoryRouter>,
  );
}

test('a customer user sees their notices', () => {
  for (const user of ['developer', 'developer-b'] as const) {
    const { unmount } = renderAs(user);

    expect(screen.getByRole('heading', { name: 'My notices' }), user).toBeInTheDocument();
    unmount();
  }
});

test('a signed-in user without customer-user is told access is denied instead', async () => {
  for (const user of ['staff', 'visitor'] as const) {
    const { container, unmount } = renderAs(user);

    expect(screen.getByRole('heading', { name: 'Access denied' }), user).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'My notices' }), user).not.toBeInTheDocument();
    expect(await axe(container)).toHaveNoViolations();
    unmount();
  }
});
