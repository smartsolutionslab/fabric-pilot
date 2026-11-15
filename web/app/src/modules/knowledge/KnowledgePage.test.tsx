import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useAuth } from 'react-oidc-context';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { useGetKnowledgeArticlesQuery } from '@/api/generated';
import { KnowledgePage } from './KnowledgePage';

vi.mock('@/api/generated', () => ({ useGetKnowledgeArticlesQuery: vi.fn() }));
vi.mock('react-oidc-context', () => ({ useAuth: vi.fn() }));

const page = {
  items: [
    {
      id: 'b1',
      title: 'Printer offline',
      state: 'Published',
      changedAt: '2026-11-12T09:30:00+00:00',
      publishedAt: '2026-11-12T09:30:00+00:00',
    },
    {
      id: 'a1',
      title: 'Resetting a password',
      state: 'Published',
      changedAt: '2026-11-11T08:00:00+00:00',
      publishedAt: '2026-11-11T08:00:00+00:00',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

// The ID tokens Keycloak issues the realm's local users (src/AppHost/realms/realm.json).
const users = {
  staff: { preferred_username: 'staff', realm_access: { roles: ['staff'] } },
  visitor: { preferred_username: 'visitor' },
};

function signInAs(user: keyof typeof users) {
  vi.mocked(useAuth).mockReturnValue({ user: { profile: users[user] } } as unknown as ReturnType<typeof useAuth>);
}

beforeEach(() => {
  signInAs('visitor');
  vi.mocked(useGetKnowledgeArticlesQuery).mockReturnValue({ data: page, isLoading: false, isError: false } as never);
});

// Without Vitest's globals, Testing Library does not unmount between tests by itself.
afterEach(cleanup);

function renderPage() {
  return render(
    <MemoryRouter>
      <KnowledgePage />
    </MemoryRouter>,
  );
}

test('the knowledge base lists the published articles, each linking to the article', { timeout: 20_000 }, () => {
  renderPage();

  expect(screen.getByRole('heading', { name: 'Knowledge' })).toBeInTheDocument();
  const row = screen.getByRole('row', { name: /Printer offline/ });
  expect(within(row).getByRole('link', { name: 'Printer offline' })).toHaveAttribute('href', '/b1');
  expect(screen.getByRole('row', { name: /Resetting a password/ })).toBeInTheDocument();
  expect(useGetKnowledgeArticlesQuery).toHaveBeenCalledWith({ search: '', page: 1, pageSize: 20 });
});

test('a search asks for the articles matching the term, from the first page', async () => {
  const user = userEvent.setup();
  vi.mocked(useGetKnowledgeArticlesQuery).mockReturnValue({
    data: { ...page, totalCount: 45 },
    isLoading: false,
    isError: false,
  } as never);
  renderPage();
  await user.click(screen.getByRole('button', { name: 'Next' }));

  await user.type(screen.getByRole('searchbox', { name: 'Search articles' }), 'printer');
  await user.click(screen.getByRole('button', { name: 'Search' }));

  expect(useGetKnowledgeArticlesQuery).toHaveBeenLastCalledWith({ search: 'printer', page: 1, pageSize: 20 });
});

test('a search that finds nothing says so', () => {
  vi.mocked(useGetKnowledgeArticlesQuery).mockReturnValue({
    data: { ...page, items: [], totalCount: 0 },
    isLoading: false,
    isError: false,
  } as never);
  renderPage();

  expect(screen.getByText('No published article matches.')).toBeInTheDocument();
});

test('staff are offered to manage the articles, customers are not', () => {
  renderPage();
  expect(screen.queryByRole('link', { name: 'Manage articles' })).not.toBeInTheDocument();
  cleanup();

  signInAs('staff');
  renderPage();
  expect(screen.getByRole('link', { name: 'Manage articles' })).toHaveAttribute('href', '/manage');
});

// The file's first test also pays for warming up jsdom, React and axe, which in a larger project under parallel load
// passes 2.5 s of the default 5 s.
test('the Knowledge page has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  signInAs('staff');
  const { container } = renderPage();
  expect(await screen.findByRole('heading', { name: 'Knowledge' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
