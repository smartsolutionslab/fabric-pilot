import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { useGetTenantsQuery, useGetTicketsTicketsAllTenantsQuery } from '@/api/generated';
import { AllTenantsTicketsPage } from './AllTenantsTicketsPage';

vi.mock('@/api/generated', () => ({ useGetTicketsTicketsAllTenantsQuery: vi.fn(), useGetTenantsQuery: vi.fn() }));

const page = {
  items: [
    {
      id: 'b1',
      tenant: 'tenant-b',
      subject: 'Printer offline',
      priority: 'High',
      status: 'InProgress',
      assignee: 'staff',
      raisedAt: '2026-11-12T09:30:00+00:00',
      attachmentCount: 2,
    },
    {
      id: 'a1',
      tenant: 'tenant-a',
      subject: 'Password reset',
      priority: 'Low',
      status: 'Open',
      assignee: null,
      raisedAt: '2026-11-11T08:00:00+00:00',
      attachmentCount: 0,
    },
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
  vi.mocked(useGetTenantsQuery).mockReturnValue({
    data: { items: ['tenant-a', 'tenant-b'], totalCount: 2, page: 1, pageSize: 100 },
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

const lastQuery = () => vi.mocked(useGetTicketsTicketsAllTenantsQuery).mock.lastCall?.[0];

test(
  'staff see every tenant’s tickets with their tenant, priority, status and assignee, each subject opening the ticket',
  { timeout: 20_000 },
  () => {
    renderPage();

    expect(screen.getByRole('heading', { name: 'Triage queue' })).toBeInTheDocument();
    const row = screen.getByRole('row', { name: /Printer offline/ });
    expect(within(row).getByText('tenant-b')).toBeInTheDocument();
    expect(within(row).getByText('High')).toBeInTheDocument();
    expect(within(row).getByText('In progress')).toBeInTheDocument();
    expect(within(row).getByText('staff')).toBeInTheDocument();
    expect(within(row).getByRole('link', { name: 'Printer offline' })).toHaveAttribute('href', '/b1');
    const unassigned = screen.getByRole('row', { name: /Password reset/ });
    expect(within(unassigned).getByText('tenant-a')).toBeInTheDocument();
    expect(within(unassigned).getByText('Open')).toBeInTheDocument();
    expect(within(unassigned).getByText('Unassigned')).toBeInTheDocument();
    expect(useGetTicketsTicketsAllTenantsQuery).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
  },
);

test('the queue is filtered by tenant, chosen from the tenants the host serves', async () => {
  const user = userEvent.setup();
  renderPage();

  await user.selectOptions(screen.getByRole('combobox', { name: 'Tenant' }), 'tenant-b');

  expect(useGetTenantsQuery).toHaveBeenCalledWith({ page: 1, pageSize: 100 });
  expect(lastQuery()).toEqual({ page: 1, pageSize: 20, tenant: 'tenant-b' });
});

test('the queue is filtered by status and by priority, together', async () => {
  const user = userEvent.setup();
  renderPage();

  await user.selectOptions(screen.getByRole('combobox', { name: 'Status' }), 'Resolved');
  await user.selectOptions(screen.getByRole('combobox', { name: 'Priority' }), 'Urgent');

  expect(lastQuery()).toEqual({ page: 1, pageSize: 20, status: 'Resolved', priority: 'Urgent' });
});

test('a filter set back to every value asks for every ticket again', async () => {
  const user = userEvent.setup();
  renderPage();

  await user.selectOptions(screen.getByRole('combobox', { name: 'Status' }), 'Closed');
  await user.selectOptions(screen.getByRole('combobox', { name: 'Status' }), 'All statuses');

  expect(lastQuery()).toEqual({ page: 1, pageSize: 20 });
});

test('the queue is sorted by age, oldest first and back to newest first', async () => {
  const user = userEvent.setup();
  renderPage();

  await user.click(screen.getByRole('button', { name: 'Oldest first' }));

  expect(lastQuery()).toEqual({ page: 1, pageSize: 20, sortBy: 'RaisedAt', sortDirection: 'Ascending' });

  await user.click(screen.getByRole('button', { name: 'Newest first' }));

  expect(lastQuery()).toEqual({ page: 1, pageSize: 20, sortBy: 'RaisedAt', sortDirection: 'Descending' });
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

  expect(lastQuery()).toEqual({ page: 2, pageSize: 20 });
});

test('changing a filter starts again at the first page', async () => {
  const user = userEvent.setup();
  vi.mocked(useGetTicketsTicketsAllTenantsQuery).mockReturnValue({
    data: { ...page, totalCount: 45 },
    isLoading: false,
    isError: false,
  } as never);
  renderPage();
  await user.click(screen.getByRole('button', { name: 'Next' }));

  await user.selectOptions(screen.getByRole('combobox', { name: 'Priority' }), 'High');

  expect(lastQuery()).toEqual({ page: 1, pageSize: 20, priority: 'High' });
});

test('the triage queue has no accessibility violations axe can find', async () => {
  const { container } = renderPage();
  expect(await screen.findByRole('heading', { name: 'Triage queue' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
