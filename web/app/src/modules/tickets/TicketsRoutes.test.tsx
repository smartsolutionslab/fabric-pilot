import { cleanup, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { useAuth } from 'react-oidc-context';
import { afterEach, expect, test, vi } from 'vitest';
import { TicketsRoutes } from './TicketsRoutes';

vi.mock('react-oidc-context', () => ({ useAuth: vi.fn() }));
vi.mock('./TicketsPage', () => ({ TicketsPage: () => <h1>Tenant tickets</h1> }));
vi.mock('./AllTenantsTicketsPage', () => ({ AllTenantsTicketsPage: () => <h1>Every tenant</h1> }));

afterEach(cleanup);

// The ID tokens Keycloak issues the realm's local users (src/AppHost/realms/realm.json): customer-admin includes
// customer-user, and Keycloak lists both.
const users = {
  developer: {
    preferred_username: 'developer',
    realm_access: { roles: ['manager', 'customer-admin', 'customer-user'] },
  },
  staff: { preferred_username: 'staff', realm_access: { roles: ['staff'] } },
};

function renderAs(user: keyof typeof users) {
  vi.mocked(useAuth).mockReturnValue({ user: { profile: users[user] } } as unknown as ReturnType<typeof useAuth>);
  render(
    <MemoryRouter>
      <TicketsRoutes />
    </MemoryRouter>,
  );
}

test('a customer opening tickets sees their tenant’s tickets', () => {
  renderAs('developer');

  expect(screen.getByRole('heading', { name: 'Tenant tickets' })).toBeInTheDocument();
});

test('staff opening tickets see every tenant’s tickets', () => {
  renderAs('staff');

  expect(screen.getByRole('heading', { name: 'Every tenant' })).toBeInTheDocument();
});
