import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useAuth } from 'react-oidc-context';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import {
  usePutTicketsTicketsByTenantAndTicketIdAssigneeMutation,
  usePutTicketsTicketsByTenantAndTicketIdStatusMutation,
} from '@/api/generated';
import { TicketTriage } from './TicketTriage';

vi.mock('@/api/generated', () => ({
  usePutTicketsTicketsByTenantAndTicketIdStatusMutation: vi.fn(),
  usePutTicketsTicketsByTenantAndTicketIdAssigneeMutation: vi.fn(),
}));
vi.mock('react-oidc-context', () => ({ useAuth: vi.fn() }));

const ticketId = '019a7b3c-5d2e-7f10-8a4b-3c2d1e0f9a8b';
const ticket = {
  id: ticketId,
  tenant: 'tenant-b',
  subject: 'Printer offline',
  description: 'It shows error 42.',
  priority: 'High',
  status: 'Open',
  assignee: null,
  raisedAt: '2026-11-12T09:30:00+00:00',
  attachments: [],
};
const changeStatus = vi.fn();
const assign = vi.fn();
const succeeding = () => ({ unwrap: () => Promise.resolve(null) });
const failing = () => ({ unwrap: () => Promise.reject(new Error('500')) });

beforeEach(() => {
  changeStatus.mockReset().mockImplementation(succeeding);
  assign.mockReset().mockImplementation(succeeding);
  vi.mocked(usePutTicketsTicketsByTenantAndTicketIdStatusMutation).mockReturnValue([
    changeStatus,
    { isLoading: false },
  ] as never);
  vi.mocked(usePutTicketsTicketsByTenantAndTicketIdAssigneeMutation).mockReturnValue([
    assign,
    { isLoading: false },
  ] as never);
  vi.mocked(useAuth).mockReturnValue({
    user: { profile: { sub: 's1', preferred_username: 'staff' } },
  } as unknown as ReturnType<typeof useAuth>);
});

afterEach(cleanup);

test('the ticket’s status is chosen, and an unassigned ticket says so', { timeout: 20_000 }, () => {
  render(<TicketTriage ticket={ticket} />);

  expect(screen.getByRole('heading', { name: 'Triage' })).toBeInTheDocument();
  expect(screen.getByRole('combobox', { name: 'Status' })).toHaveValue('Open');
  expect(screen.getByText('Not assigned yet.')).toBeInTheDocument();
});

test('a ticket assigned to someone names them', () => {
  render(<TicketTriage ticket={{ ...ticket, assignee: 'colleague' }} />);

  expect(screen.getByText('Assigned to colleague.')).toBeInTheDocument();
});

test('staff change the status in the ticket’s own tenant', async () => {
  const user = userEvent.setup();
  render(<TicketTriage ticket={ticket} />);

  await user.selectOptions(screen.getByRole('combobox', { name: 'Status' }), 'Resolved');
  await user.click(screen.getByRole('button', { name: 'Change status' }));

  expect(changeStatus).toHaveBeenCalledWith({
    tenant: 'tenant-b',
    ticketId,
    ticketStatusChange: { status: 'Resolved' },
  });
  expect(await screen.findByRole('status')).toHaveTextContent('The status is now Resolved.');
});

test('a status that cannot be changed says so', async () => {
  const user = userEvent.setup();
  changeStatus.mockImplementation(failing);
  render(<TicketTriage ticket={ticket} />);

  await user.selectOptions(screen.getByRole('combobox', { name: 'Status' }), 'Closed');
  await user.click(screen.getByRole('button', { name: 'Change status' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('The status could not be changed.');
});

test('staff assign the ticket to themselves by their user name', async () => {
  const user = userEvent.setup();
  render(<TicketTriage ticket={ticket} />);

  await user.click(screen.getByRole('button', { name: 'Assign to me' }));

  expect(assign).toHaveBeenCalledWith({ tenant: 'tenant-b', ticketId, ticketAssignment: { assignee: 'staff' } });
  expect(await screen.findByRole('status')).toHaveTextContent('Assigned to staff.');
});

test('staff assign the ticket to a colleague by their user name', async () => {
  const user = userEvent.setup();
  render(<TicketTriage ticket={ticket} />);

  await user.type(screen.getByRole('textbox', { name: 'Colleague’s user name' }), '  colleague ');
  await user.click(screen.getByRole('button', { name: 'Assign to colleague' }));

  expect(assign).toHaveBeenCalledWith({ tenant: 'tenant-b', ticketId, ticketAssignment: { assignee: 'colleague' } });
});

test('a colleague without a user name is refused before anything is sent', async () => {
  const user = userEvent.setup();
  render(<TicketTriage ticket={ticket} />);

  await user.click(screen.getByRole('button', { name: 'Assign to colleague' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('Enter the colleague’s user name.');
  expect(assign).not.toHaveBeenCalled();
});

test('an assignment that fails says so', async () => {
  const user = userEvent.setup();
  assign.mockImplementation(failing);
  render(<TicketTriage ticket={ticket} />);

  await user.click(screen.getByRole('button', { name: 'Assign to me' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('The ticket could not be assigned.');
});

test('the triage controls have no accessibility violations axe can find', async () => {
  const { container } = render(<TicketTriage ticket={{ ...ticket, assignee: 'staff' }} />);
  expect(await screen.findByRole('heading', { name: 'Triage' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
