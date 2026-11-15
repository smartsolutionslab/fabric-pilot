import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { useGetTenantsQuery, usePostContractsContractsByTenantMutation } from '@/api/generated';
import { RecordContractPage } from './RecordContractPage';

vi.mock('@/api/generated', () => ({
  useGetTenantsQuery: vi.fn(),
  usePostContractsContractsByTenantMutation: vi.fn(),
}));

const record = vi.fn();

const tenants = { items: ['tenant-a', 'tenant-b'], totalCount: 2, page: 1, pageSize: 100 };

beforeEach(() => {
  record.mockReset();
  record.mockReturnValue({ unwrap: () => Promise.resolve('new-contract-id') });
  vi.mocked(usePostContractsContractsByTenantMutation).mockReturnValue([record, { isLoading: false }] as never);
  vi.mocked(useGetTenantsQuery).mockReturnValue({ data: tenants, isLoading: false, isError: false } as never);
});

afterEach(cleanup);

function renderPage() {
  return render(
    <MemoryRouter>
      <RecordContractPage />
    </MemoryRouter>,
  );
}

async function fillIn(user: ReturnType<typeof userEvent.setup>, product: string, startsOn: string, endsOn: string) {
  await user.selectOptions(screen.getByLabelText('Tenant'), 'tenant-b');
  await user.type(screen.getByLabelText('Product'), product);
  await user.type(screen.getByLabelText('First day'), startsOn);
  await user.type(screen.getByLabelText('Last day'), endsOn);
}

// The file's first test also pays for warming up jsdom, React and axe.
test('the form to record a contract has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const { container } = renderPage();
  expect(await screen.findByRole('heading', { name: 'Record a contract' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});

test('staff choose the tenant from the tenants the host lists', () => {
  renderPage();

  expect(screen.getByRole('option', { name: 'tenant-a' })).toBeInTheDocument();
  expect(screen.getByRole('option', { name: 'tenant-b' })).toBeInTheDocument();
  expect(useGetTenantsQuery).toHaveBeenCalledWith({ page: 1, pageSize: 100 });
});

test('a contract is recorded for the chosen tenant with its product, its days and whether it renews', async () => {
  const user = userEvent.setup();
  renderPage();

  await fillIn(user, 'Managed printing', '2026-01-01', '2026-12-31');
  await user.click(screen.getByLabelText('Renews after its last day'));
  await user.click(screen.getByRole('button', { name: 'Record' }));

  expect(await screen.findByRole('status')).toHaveTextContent('Recorded Managed printing for tenant-b.');
  expect(record).toHaveBeenCalledWith({
    tenant: 'tenant-b',
    recordContract: { product: 'Managed printing', startsOn: '2026-01-01', endsOn: '2026-12-31', renews: false },
  });
});

test('a contract renews unless staff say otherwise', async () => {
  const user = userEvent.setup();
  renderPage();

  await fillIn(user, 'Hosting', '2026-01-01', '2026-12-31');
  await user.click(screen.getByRole('button', { name: 'Record' }));

  expect(await screen.findByRole('status')).toBeInTheDocument();
  expect(record).toHaveBeenCalledWith({
    tenant: 'tenant-b',
    recordContract: { product: 'Hosting', startsOn: '2026-01-01', endsOn: '2026-12-31', renews: true },
  });
});

test('a contract without a tenant or a product is refused next to the fields, and nothing is sent', async () => {
  const user = userEvent.setup();
  renderPage();

  await user.type(screen.getByLabelText('First day'), '2026-01-01');
  await user.type(screen.getByLabelText('Last day'), '2026-12-31');
  await user.click(screen.getByRole('button', { name: 'Record' }));

  expect(await screen.findByText('Choose a tenant.')).toBeInTheDocument();
  expect(screen.getByText('Name the product.')).toBeInTheDocument();
  expect(screen.getByLabelText('Tenant')).toHaveAttribute('aria-invalid', 'true');
  expect(screen.getByLabelText('Product')).toHaveAttribute('aria-invalid', 'true');
  expect(record).not.toHaveBeenCalled();
});

test('a last day before the first is refused next to the last day, and nothing is sent', async () => {
  const user = userEvent.setup();
  renderPage();

  await fillIn(user, 'Hosting', '2026-12-31', '2026-01-01');
  await user.click(screen.getByRole('button', { name: 'Record' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('The last day cannot come before the first.');
  expect(screen.getByLabelText('Last day')).toHaveAttribute('aria-invalid', 'true');
  expect(record).not.toHaveBeenCalled();
});

test("the API's refusal is shown next to the field it is about", async () => {
  const user = userEvent.setup();
  record.mockReturnValue({
    unwrap: () =>
      Promise.reject({
        status: 422,
        data: { code: 'contracts.invalid-product', title: 'A contract needs a product of 1 to 200 characters.' },
      }),
  });
  renderPage();

  await fillIn(user, 'Hosting', '2026-01-01', '2026-12-31');
  await user.click(screen.getByRole('button', { name: 'Record' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('A contract needs a product of 1 to 200 characters.');
  expect(screen.getByLabelText('Product')).toHaveAttribute('aria-invalid', 'true');
  expect(screen.queryByRole('status')).not.toBeInTheDocument();
});

test('a tenant list that cannot be loaded says so', () => {
  vi.mocked(useGetTenantsQuery).mockReturnValue({ data: undefined, isLoading: false, isError: true } as never);

  renderPage();

  expect(screen.getByRole('alert')).toHaveTextContent('The tenants could not be loaded.');
});
