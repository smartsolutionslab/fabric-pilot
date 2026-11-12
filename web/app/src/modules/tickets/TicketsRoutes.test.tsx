import { cleanup, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { useAuth } from 'react-oidc-context';
import { afterEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { TicketsRoutes } from './TicketsRoutes';

vi.mock('react-oidc-context', () => ({ useAuth: vi.fn() }));
vi.mock('./TicketsPage', () => ({ TicketsPage: () => <h1>Tenant tickets</h1> }));
vi.mock('./AllTenantsTicketsPage', () => ({ AllTenantsTicketsPage: () => <h1>Every tenant</h1> }));
vi.mock('./RaiseTicketPage', () => ({ RaiseTicketPage: () => <h1>Raise a ticket</h1> }));
vi.mock('./TicketPage', () => ({ TicketPage: () => <h1>One ticket</h1> }));
vi.mock('./StaffTicketPage', () => ({ StaffTicketPage: () => <h1>A tenant's ticket for staff</h1> }));

afterEach(cleanup);

// The ID tokens Keycloak issues the realm's local users (src/AppHost/realms/realm.json): customer-admin includes
// customer-user, and Keycloak lists both. The visitor holds no realm role, so the token has no realm_access.
const users = {
  developer: {
    preferred_username: 'developer',
    realm_access: { roles: ['manager', 'customer-admin', 'customer-user'] },
  },
  'developer-b': { preferred_username: 'developer-b', realm_access: { roles: ['manager', 'customer-user'] } },
  staff: { preferred_username: 'staff', realm_access: { roles: ['staff'] } },
  visitor: { preferred_username: 'visitor' },
};

function renderAs(user: keyof typeof users, path = '/') {
  vi.mocked(useAuth).mockReturnValue({ user: { profile: users[user] } } as unknown as ReturnType<typeof useAuth>);
  return render(
    <MemoryRouter initialEntries={[path]}>
      <TicketsRoutes />
    </MemoryRouter>,
  );
}

test('a customer opening tickets sees their tenant’s tickets', () => {
  renderAs('developer');

  expect(screen.getByRole('heading', { name: 'Tenant tickets' })).toBeInTheDocument();
});

test('a customer user who is not an admin sees their tenant’s tickets, can raise one and open one', () => {
  for (const [path, heading] of [
    ['/', 'Tenant tickets'],
    ['/new', 'Raise a ticket'],
    ['/b1', 'One ticket'],
  ] as const) {
    const { unmount } = renderAs('developer-b', path);

    expect(screen.getByRole('heading', { name: heading }), path).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Access denied' }), path).not.toBeInTheDocument();
    unmount();
  }
});

test('staff opening tickets see every tenant’s tickets', () => {
  renderAs('staff');

  expect(screen.getByRole('heading', { name: 'Every tenant' })).toBeInTheDocument();
});

test('staff opening a ticket see it as staff, with the suggested answer, not the customer’s page', () => {
  renderAs('staff', '/b1');

  expect(screen.getByRole('heading', { name: "A tenant's ticket for staff" })).toBeInTheDocument();
  expect(screen.queryByRole('heading', { name: 'One ticket' })).not.toBeInTheDocument();
});

test('a signed-in user without customer-user opening tickets is told access is denied instead of the list', () => {
  renderAs('visitor');

  expect(screen.getByRole('heading', { name: 'Access denied' })).toBeInTheDocument();
  expect(screen.queryByRole('heading', { name: 'Tenant tickets' })).not.toBeInTheDocument();
  expect(screen.queryByRole('link', { name: 'Raise a ticket' })).not.toBeInTheDocument();
});

test('a signed-in user without customer-user is told access is denied when raising or opening a ticket', () => {
  for (const [path, heading] of [
    ['/new', 'Raise a ticket'],
    ['/b1', 'One ticket'],
  ] as const) {
    const { unmount } = renderAs('visitor', path);

    expect(screen.getByRole('heading', { name: 'Access denied' }), path).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: heading }), path).not.toBeInTheDocument();
    unmount();
  }
});

// The file's first axe check also pays for warming up jsdom, React and axe; see TicketsPage.test.tsx.
test('the access-denied state has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const { container } = renderAs('visitor');
  expect(screen.getByRole('heading', { name: 'Access denied' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
