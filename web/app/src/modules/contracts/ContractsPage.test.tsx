import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { useGetContractsContractsQuery } from '@/api/generated';
import { ContractsPage } from './ContractsPage';

vi.mock('@/api/generated', () => ({ useGetContractsContractsQuery: vi.fn() }));

const page = {
  items: [
    {
      id: 'c1',
      product: 'Migration project',
      startsOn: '2026-03-01',
      endsOn: '2026-08-31',
      renewsOn: null,
    },
    {
      id: 'c2',
      product: 'Managed printing',
      startsOn: '2026-01-01',
      endsOn: '2026-12-31',
      renewsOn: '2027-01-01',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

beforeEach(() => {
  vi.mocked(useGetContractsContractsQuery).mockReturnValue({ data: page, isLoading: false, isError: false } as never);
});

// Without Vitest's globals, Testing Library does not unmount between tests by itself.
afterEach(cleanup);

function renderPage() {
  return render(
    <MemoryRouter>
      <ContractsPage />
    </MemoryRouter>,
  );
}

test(
  'the list shows each contract with its start, its end and the renewal date the server gives',
  { timeout: 20_000 },
  () => {
    renderPage();

    expect(screen.getByRole('heading', { name: 'Contracts' })).toBeInTheDocument();
    const renewing = screen.getByRole('row', { name: /Managed printing/ });
    expect(within(renewing).getByText('2026-01-01')).toBeInTheDocument();
    expect(within(renewing).getByText('2026-12-31')).toBeInTheDocument();
    expect(within(renewing).getByText('2027-01-01')).toBeInTheDocument();
    expect(useGetContractsContractsQuery).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
  },
);

test('a contract that ends without renewing says so instead of a renewal date', () => {
  renderPage();

  const ending = screen.getByRole('row', { name: /Migration project/ });
  expect(within(ending).getByText('2026-08-31')).toBeInTheDocument();
  expect(within(ending).getByText('Ends without renewal')).toBeInTheDocument();
});

test('a list that cannot be loaded says so', () => {
  vi.mocked(useGetContractsContractsQuery).mockReturnValue({
    data: undefined,
    isLoading: false,
    isError: true,
  } as never);

  renderPage();

  expect(screen.getByRole('alert')).toHaveTextContent('The contracts could not be loaded.');
});

test('the list has no accessibility violations axe can find', async () => {
  const { container } = renderPage();

  expect(await axe(container)).toHaveNoViolations();
});

test('above twenty contracts the list pages through them twenty at a time', async () => {
  const user = userEvent.setup();
  vi.mocked(useGetContractsContractsQuery).mockReturnValue({
    data: { ...page, totalCount: 45 },
    isLoading: false,
    isError: false,
  } as never);
  renderPage();

  expect(screen.getByRole('navigation', { name: 'Pages' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();

  await user.click(screen.getByRole('button', { name: 'Next' }));

  expect(useGetContractsContractsQuery).toHaveBeenLastCalledWith({ page: 2, pageSize: 20 });
  expect(screen.getByRole('button', { name: 'Previous' })).toBeEnabled();
});

test('twenty contracts or fewer need no pages', () => {
  renderPage();

  expect(screen.queryByRole('navigation', { name: 'Pages' })).not.toBeInTheDocument();
});

// The file's first test also pays for warming up jsdom, React and axe, which in a larger project under parallel load
// passes 2.5 s of the default 5 s.
test('the Contracts page has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const { container } = render(
    <MemoryRouter>
      <ContractsPage />
    </MemoryRouter>,
  );
  expect(await screen.findByRole('heading', { name: 'Contracts' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
