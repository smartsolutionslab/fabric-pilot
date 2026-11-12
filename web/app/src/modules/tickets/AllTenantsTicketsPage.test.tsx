import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { useGetTicketsTicketsAllTenantsQuery } from '@/api/generated';
import { AllTenantsTicketsPage } from './AllTenantsTicketsPage';

vi.mock('@/api/generated', () => ({ useGetTicketsTicketsAllTenantsQuery: vi.fn() }));

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
  vi.mocked(useGetTicketsTicketsAllTenantsQuery).mockReturnValue({
    data: page,
    isLoading: false,
    isError: false,
  } as never);
});

afterEach(cleanup);

function renderPage() {
  return render(
    <MemoryRouter>
      <AllTenantsTicketsPage />
    </MemoryRouter>,
  );
}

test('staff see every tenant’s tickets with their priority and files, read only', { timeout: 20_000 }, () => {
  renderPage();

  expect(screen.getByRole('heading', { name: 'Tickets of every tenant' })).toBeInTheDocument();
  const row = screen.getByRole('row', { name: /Printer offline/ });
  expect(within(row).getByText('High')).toBeInTheDocument();
  expect(within(row).getByText('2')).toBeInTheDocument();
  expect(screen.getByRole('row', { name: /Password reset/ })).toBeInTheDocument();
  expect(screen.queryByRole('link')).not.toBeInTheDocument();
  expect(useGetTicketsTicketsAllTenantsQuery).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
});

test('a list that cannot be loaded says so', () => {
  vi.mocked(useGetTicketsTicketsAllTenantsQuery).mockReturnValue({
    data: undefined,
    isLoading: false,
    isError: true,
  } as never);
  renderPage();

  expect(screen.getByRole('alert')).toHaveTextContent('The tickets could not be loaded.');
});

test('above twenty tickets the list pages through them twenty at a time', async () => {
  const user = userEvent.setup();
  vi.mocked(useGetTicketsTicketsAllTenantsQuery).mockReturnValue({
    data: { ...page, totalCount: 45 },
    isLoading: false,
    isError: false,
  } as never);
  renderPage();

  await user.click(screen.getByRole('button', { name: 'Next' }));

  expect(useGetTicketsTicketsAllTenantsQuery).toHaveBeenLastCalledWith({ page: 2, pageSize: 20 });
});

test('the all-tenants list has no accessibility violations axe can find', async () => {
  const { container } = renderPage();
  expect(await screen.findByRole('heading', { name: 'Tickets of every tenant' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
