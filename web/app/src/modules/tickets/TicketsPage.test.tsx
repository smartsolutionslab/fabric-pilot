import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { useGetTicketsTicketsQuery } from '@/api/generated';
import { TicketsPage } from './TicketsPage';

vi.mock('@/api/generated', () => ({ useGetTicketsTicketsQuery: vi.fn() }));

const page = {
  items: [
    {
      id: 'b1',
      subject: 'Printer offline',
      priority: 'High',
      raisedAt: '2026-11-12T09:30:00+00:00',
      attachmentCount: 2,
    },
    { id: 'a1', subject: 'Password reset', priority: 'Low', raisedAt: '2026-11-11T08:00:00+00:00', attachmentCount: 0 },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

beforeEach(() => {
  vi.mocked(useGetTicketsTicketsQuery).mockReturnValue({ data: page, isLoading: false, isError: false } as never);
});

// Without Vitest's globals, Testing Library does not unmount between tests by itself.
afterEach(cleanup);

function renderPage() {
  return render(
    <MemoryRouter>
      <TicketsPage />
    </MemoryRouter>,
  );
}

test(
  'the list shows each ticket with its priority and how many files it has, linking to the ticket',
  { timeout: 20_000 },
  () => {
    renderPage();

    expect(screen.getByRole('heading', { name: 'Tickets' })).toBeInTheDocument();
    const row = screen.getByRole('row', { name: /Printer offline/ });
    expect(within(row).getByRole('link', { name: 'Printer offline' })).toHaveAttribute('href', '/b1');
    expect(within(row).getByText('High')).toBeInTheDocument();
    expect(within(row).getByText('2')).toBeInTheDocument();
    expect(screen.getByRole('row', { name: /Password reset/ })).toBeInTheDocument();
    expect(useGetTicketsTicketsQuery).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
  },
);

test('the list links to raising a ticket', () => {
  renderPage();

  expect(screen.getByRole('link', { name: 'Raise a ticket' })).toHaveAttribute('href', '/new');
});

test('the list has no accessibility violations axe can find', async () => {
  const { container } = renderPage();

  expect(await axe(container)).toHaveNoViolations();
});

test('above twenty tickets the list pages through them twenty at a time', async () => {
  const user = userEvent.setup();
  vi.mocked(useGetTicketsTicketsQuery).mockReturnValue({
    data: { ...page, totalCount: 45 },
    isLoading: false,
    isError: false,
  } as never);
  renderPage();

  expect(screen.getByRole('navigation', { name: 'Pages' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();

  await user.click(screen.getByRole('button', { name: 'Next' }));

  expect(useGetTicketsTicketsQuery).toHaveBeenLastCalledWith({ page: 2, pageSize: 20 });
  expect(screen.getByRole('button', { name: 'Previous' })).toBeEnabled();
});

test('twenty tickets or fewer need no pages', () => {
  renderPage();

  expect(screen.queryByRole('navigation', { name: 'Pages' })).not.toBeInTheDocument();
});

// The file's first test also pays for warming up jsdom, React and axe, which in a larger project under parallel load
// passes 2.5 s of the default 5 s.
test('the Tickets page has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const { container } = render(
    <MemoryRouter>
      <TicketsPage />
    </MemoryRouter>,
  );
  expect(await screen.findByRole('heading', { name: 'Tickets' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
