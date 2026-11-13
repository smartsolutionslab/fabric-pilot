import { cleanup, render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { useGetNotificationsNoticesQuery } from '@/api/generated';
import { NotificationsPage } from './NotificationsPage';

vi.mock('@/api/generated', () => ({ useGetNotificationsNoticesQuery: vi.fn() }));

const page = {
  items: [
    {
      id: 'n2',
      ticketId: 't2',
      text: 'Your ticket "VPN access" is now Resolved.',
      createdAt: '2026-11-13T10:00:00+00:00',
    },
    {
      id: 'n1',
      ticketId: 't1',
      text: 'Your ticket "Printer offline" is now InProgress.',
      createdAt: '2026-11-13T09:00:00+00:00',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

beforeEach(() => {
  vi.mocked(useGetNotificationsNoticesQuery).mockReturnValue({ data: page, isLoading: false, isError: false } as never);
});

// Without Vitest's globals, Testing Library does not unmount between tests by itself.
afterEach(cleanup);

function renderPage() {
  return render(
    <MemoryRouter>
      <NotificationsPage />
    </MemoryRouter>,
  );
}

// The file's first test also pays for warming up jsdom, React and axe, which in a larger project under parallel load
// passes 2.5 s of the default 5 s.
test('the Notifications page has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const { container } = renderPage();
  expect(await screen.findByRole('heading', { name: 'Notifications' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});

test('each notice links to the ticket it is about', () => {
  renderPage();

  const row = screen.getByRole('row', { name: /VPN access/ });
  expect(within(row).getByRole('link', { name: 'Your ticket "VPN access" is now Resolved.' })).toHaveAttribute(
    'href',
    '/tickets/t2',
  );
  expect(screen.getByRole('row', { name: /Printer offline/ })).toBeInTheDocument();
});

test('the page asks for new notices again by itself, so one appears without a reload', () => {
  renderPage();

  expect(useGetNotificationsNoticesQuery).toHaveBeenCalledWith({ page: 1, pageSize: 20 }, { pollingInterval: 15_000 });
});

test('without notices the page says there are none', () => {
  vi.mocked(useGetNotificationsNoticesQuery).mockReturnValue({
    data: { items: [], totalCount: 0, page: 1, pageSize: 20 },
    isLoading: false,
    isError: false,
  } as never);
  renderPage();

  expect(screen.getByText('No notifications yet.')).toBeInTheDocument();
});

test('a failed load is announced', () => {
  vi.mocked(useGetNotificationsNoticesQuery).mockReturnValue({
    data: undefined,
    isLoading: false,
    isError: true,
  } as never);
  renderPage();

  expect(screen.getByRole('alert')).toHaveTextContent('The notifications could not be loaded.');
});
