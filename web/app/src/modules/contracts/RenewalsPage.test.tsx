import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { useGetContractsContractsUpForRenewalQuery } from '@/api/generated';
import { RenewalsPage } from './RenewalsPage';

vi.mock('@/api/generated', () => ({ useGetContractsContractsUpForRenewalQuery: vi.fn() }));

const page = {
  items: [
    {
      id: 'c1',
      product: 'Managed printing',
      startsOn: '2026-01-01',
      endsOn: '2026-12-31',
      renewsOn: '2027-01-01',
    },
    {
      id: 'c2',
      product: 'Hosting',
      startsOn: '2025-02-01',
      endsOn: '2027-01-31',
      renewsOn: '2027-02-01',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

beforeEach(() => {
  vi.mocked(useGetContractsContractsUpForRenewalQuery).mockReturnValue({
    data: page,
    isLoading: false,
    isError: false,
  } as never);
});

afterEach(cleanup);

function renderPage() {
  return render(
    <MemoryRouter>
      <RenewalsPage />
    </MemoryRouter>,
  );
}

// The file's first test also pays for warming up jsdom, React and axe.
test('the renewal list has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const { container } = renderPage();
  expect(await screen.findByRole('heading', { name: 'Contracts up for renewal' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});

test('the list shows each contract up for renewal with its end and the renewal date the server gives', () => {
  renderPage();

  const printing = screen.getByRole('row', { name: /Managed printing/ });
  expect(within(printing).getByText('2026-12-31')).toBeInTheDocument();
  expect(within(printing).getByText('2027-01-01')).toBeInTheDocument();
  expect(screen.getByRole('row', { name: /Hosting/ })).toBeInTheDocument();
  expect(useGetContractsContractsUpForRenewalQuery).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
});

test('a renewal list that cannot be loaded says so', () => {
  vi.mocked(useGetContractsContractsUpForRenewalQuery).mockReturnValue({
    data: undefined,
    isLoading: false,
    isError: true,
  } as never);

  renderPage();

  expect(screen.getByRole('alert')).toHaveTextContent('The contracts up for renewal could not be loaded.');
});

test('above twenty contracts the renewal list pages through them twenty at a time', async () => {
  const user = userEvent.setup();
  vi.mocked(useGetContractsContractsUpForRenewalQuery).mockReturnValue({
    data: { ...page, totalCount: 45 },
    isLoading: false,
    isError: false,
  } as never);
  renderPage();

  await user.click(screen.getByRole('button', { name: 'Next' }));

  expect(useGetContractsContractsUpForRenewalQuery).toHaveBeenLastCalledWith({ page: 2, pageSize: 20 });
});
