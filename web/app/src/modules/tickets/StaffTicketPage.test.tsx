import { skipToken } from '@reduxjs/toolkit/query';
import { cleanup, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Link, MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import {
  useGetTicketsTicketsAllTenantsByTicketIdQuery,
  usePostKnowledgeSuggestedAnswersMutation,
} from '@/api/generated';
import { StaffTicketPage } from './StaffTicketPage';

vi.mock('@/api/generated', () => ({
  useGetTicketsTicketsAllTenantsByTicketIdQuery: vi.fn(),
  usePostKnowledgeSuggestedAnswersMutation: vi.fn(),
}));

const ticketId = '019a7b3c-5d2e-7f10-8a4b-3c2d1e0f9a8b';
const ticket = {
  id: ticketId,
  subject: 'Printer offline',
  description: 'It shows error 42.',
  priority: 'High',
  raisedAt: '2026-11-12T09:30:00+00:00',
  attachments: [{ id: 'f1', fileName: 'screenshot.png', contentType: 'image/png', size: 2048 }],
};
const drafted = {
  drafted: true,
  text: 'Please restart the print spooler.',
  sources: [
    { articleId: '019a7b3c-0000-7000-8000-000000000001', title: 'Printer error 42' },
    { articleId: '019a7b3c-0000-7000-8000-000000000002', title: 'Printer drivers' },
  ],
};
const notDrafted = {
  drafted: false,
  text: 'No article in the knowledge base answers this ticket, so no answer was drafted. Write the answer yourself.',
  sources: [],
};
const suggest = vi.fn();

function answering(suggestion: unknown) {
  suggest.mockReturnValue({ unwrap: () => Promise.resolve(suggestion) });
}

beforeEach(() => {
  vi.mocked(useGetTicketsTicketsAllTenantsByTicketIdQuery).mockReturnValue({
    data: ticket,
    currentData: ticket,
    isLoading: false,
    isError: false,
  } as never);
  suggest.mockReset();
  answering(drafted);
  vi.mocked(usePostKnowledgeSuggestedAnswersMutation).mockReturnValue([suggest, { isLoading: false }] as never);
});

afterEach(cleanup);

function renderTicket(path = `/tickets/${ticketId}`) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/tickets/:ticketId" element={<StaffTicketPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

test('staff see a tenant’s ticket with its subject, description, priority and files', { timeout: 20_000 }, () => {
  renderTicket();

  expect(useGetTicketsTicketsAllTenantsByTicketIdQuery).toHaveBeenCalledWith({ ticketId });
  expect(screen.getByRole('heading', { name: 'Printer offline' })).toBeInTheDocument();
  expect(screen.getByText('It shows error 42.')).toBeInTheDocument();
  expect(screen.getByText('High')).toBeInTheDocument();
  expect(screen.getByText('screenshot.png')).toBeInTheDocument();
  expect(screen.getByRole('link', { name: 'Back to the tickets' })).toBeInTheDocument();
});

test('asking for a suggestion sends the ticket’s subject and description as the question', async () => {
  const user = userEvent.setup();
  renderTicket();

  await user.click(screen.getByRole('button', { name: 'Suggest an answer' }));

  expect(suggest).toHaveBeenCalledWith({ suggestionRequest: { question: 'Printer offline\n\nIt shows error 42.' } });
});

test('a drafted answer is shown for staff to edit, with each source article linked', async () => {
  const user = userEvent.setup();
  renderTicket();

  await user.click(screen.getByRole('button', { name: 'Suggest an answer' }));

  const draft = await screen.findByRole('textbox', { name: 'Draft answer' });
  expect(draft).toHaveValue('Please restart the print spooler.');
  const sources = screen.getByRole('list', { name: 'Sources' });
  expect(within(sources).getByRole('link', { name: 'Printer error 42' })).toHaveAttribute(
    'href',
    '/knowledge/019a7b3c-0000-7000-8000-000000000001',
  );
  expect(within(sources).getByRole('link', { name: 'Printer drivers' })).toHaveAttribute(
    'href',
    '/knowledge/019a7b3c-0000-7000-8000-000000000002',
  );

  await user.clear(draft);
  await user.type(draft, 'Dear customer, restart the spooler.');

  expect(draft).toHaveValue('Dear customer, restart the spooler.');
});

test('a source article opens beside the draft, so reading it keeps the edits', async () => {
  const user = userEvent.setup();
  renderTicket();

  await user.click(screen.getByRole('button', { name: 'Suggest an answer' }));
  await screen.findByRole('textbox', { name: 'Draft answer' });

  const source = within(screen.getByRole('list', { name: 'Sources' })).getByRole('link', { name: 'Printer error 42' });
  expect(source).toHaveAttribute('target', '_blank');
  expect(source).toHaveAttribute('rel', 'noopener noreferrer');
});

test('opening another ticket leaves the previous ticket’s draft and sources behind', async () => {
  const otherId = '019a7b3c-5d2e-7f10-8a4b-3c2d1e0f9a8c';
  vi.mocked(useGetTicketsTicketsAllTenantsByTicketIdQuery).mockImplementation((argument) => {
    const shown =
      argument !== skipToken && argument.ticketId === otherId
        ? { ...ticket, id: otherId, subject: 'Password reset' }
        : ticket;
    return { data: shown, currentData: shown, isLoading: false, isError: false } as never;
  });
  const user = userEvent.setup();
  render(
    <MemoryRouter initialEntries={[`/tickets/${ticketId}`]}>
      <Link to={`/tickets/${otherId}`}>Next ticket</Link>
      <Routes>
        <Route path="/tickets/:ticketId" element={<StaffTicketPage />} />
      </Routes>
    </MemoryRouter>,
  );
  await user.click(screen.getByRole('button', { name: 'Suggest an answer' }));
  await screen.findByRole('textbox', { name: 'Draft answer' });

  await user.click(screen.getByRole('link', { name: 'Next ticket' }));

  expect(await screen.findByRole('heading', { name: 'Password reset' })).toBeInTheDocument();
  expect(screen.queryByRole('textbox', { name: 'Draft answer' })).not.toBeInTheDocument();
  expect(screen.queryByRole('list', { name: 'Sources' })).not.toBeInTheDocument();
});

test('nothing is sent to the customer: the page offers no way to send the draft', async () => {
  const user = userEvent.setup();
  renderTicket();

  await user.click(screen.getByRole('button', { name: 'Suggest an answer' }));
  await screen.findByRole('textbox', { name: 'Draft answer' });

  expect(screen.queryByRole('button', { name: /send/i })).not.toBeInTheDocument();
  expect(screen.getByText(/is not sent to the customer/)).toBeInTheDocument();
});

test('when no article answers the ticket the page says so and drafts nothing', async () => {
  const user = userEvent.setup();
  answering(notDrafted);
  renderTicket();

  await user.click(screen.getByRole('button', { name: 'Suggest an answer' }));

  expect(await screen.findByRole('status')).toHaveTextContent('No article in the knowledge base answers this ticket');
  expect(screen.queryByRole('textbox', { name: 'Draft answer' })).not.toBeInTheDocument();
  expect(screen.queryByRole('list', { name: 'Sources' })).not.toBeInTheDocument();
});

test('a suggestion that cannot be drafted says so', async () => {
  const user = userEvent.setup();
  suggest.mockReturnValue({ unwrap: () => Promise.reject(new Error('502')) });
  renderTicket();

  await user.click(screen.getByRole('button', { name: 'Suggest an answer' }));

  expect(await screen.findByRole('alert')).toHaveTextContent('No answer could be suggested.');
});

test('a ticket that cannot be loaded says so', () => {
  vi.mocked(useGetTicketsTicketsAllTenantsByTicketIdQuery).mockReturnValue({
    data: undefined,
    currentData: undefined,
    isLoading: false,
    isError: true,
  } as never);
  renderTicket();

  expect(screen.getByRole('alert')).toHaveTextContent('The ticket could not be loaded.');
});

test('an address whose ticket id is not a GUID never reaches the API', () => {
  vi.mocked(useGetTicketsTicketsAllTenantsByTicketIdQuery).mockReturnValue({
    data: undefined,
    currentData: undefined,
    isLoading: false,
    isError: false,
  } as never);
  renderTicket('/tickets/not-a-guid');

  expect(useGetTicketsTicketsAllTenantsByTicketIdQuery).toHaveBeenCalledWith(skipToken);
  expect(screen.getByRole('alert')).toHaveTextContent('There is no such ticket.');
});

test('the staff ticket with a drafted answer has no accessibility violations axe can find', async () => {
  const user = userEvent.setup();
  const { container } = renderTicket();
  await user.click(screen.getByRole('button', { name: 'Suggest an answer' }));
  await screen.findByRole('textbox', { name: 'Draft answer' });

  expect(await axe(container)).toHaveNoViolations();
});
