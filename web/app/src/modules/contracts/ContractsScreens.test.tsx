import { cleanup, render, screen } from '@testing-library/react';
import { useAuth } from 'react-oidc-context';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { useGetContractsContractsQuery, useGetContractsContractsUpForRenewalQuery } from '@/api/generated';
import { ContractsScreens } from './ContractsScreens';

vi.mock('@/api/generated', () => ({
  useGetContractsContractsQuery: vi.fn(),
  useGetContractsContractsUpForRenewalQuery: vi.fn(),
}));
vi.mock('react-oidc-context', () => ({ useAuth: vi.fn() }));

const empty = { items: [], totalCount: 0, page: 1, pageSize: 20 };

// The ID tokens Keycloak issues the realm's local users (src/AppHost/realms/realm.json).
const users = {
  staff: { preferred_username: 'staff', realm_access: { roles: ['staff'] } },
  developer: { preferred_username: 'developer', realm_access: { roles: ['manager'] }, department: 'finance' },
};

function signInAs(user: keyof typeof users) {
  vi.mocked(useAuth).mockReturnValue({ user: { profile: users[user] } } as unknown as ReturnType<typeof useAuth>);
}

beforeEach(() => {
  vi.mocked(useGetContractsContractsQuery).mockReturnValue({ data: empty, isLoading: false, isError: false } as never);
  vi.mocked(useGetContractsContractsUpForRenewalQuery).mockReturnValue({
    data: empty,
    isLoading: false,
    isError: false,
  } as never);
});

afterEach(cleanup);

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <ContractsScreens />
    </MemoryRouter>,
  );
}

test('staff are offered the renewal list from the contracts screen', { timeout: 20_000 }, () => {
  signInAs('staff');

  renderAt('/');

  expect(screen.getByRole('link', { name: 'Contracts up for renewal' })).toHaveAttribute('href', '/renewals');
});

test('a tenant user is not offered the renewal list', () => {
  signInAs('developer');

  renderAt('/');

  expect(screen.getByRole('heading', { name: 'Contracts' })).toBeInTheDocument();
  expect(screen.queryByRole('link', { name: 'Contracts up for renewal' })).not.toBeInTheDocument();
});

test('the renewals path shows the renewal list', () => {
  signInAs('staff');

  renderAt('/renewals');

  expect(screen.getByRole('heading', { name: 'Contracts up for renewal' })).toBeInTheDocument();
  expect(useGetContractsContractsQuery).not.toHaveBeenCalled();
});
