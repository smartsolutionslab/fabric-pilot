import { skipToken } from '@reduxjs/toolkit/query';
import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import {
  useGetTicketsTicketsByTicketIdQuery,
  useLazyGetTicketsTicketsByTicketIdAttachmentsAndAttachmentIdQuery,
} from '@/api/generated';
import { TicketPage } from './TicketPage';

vi.mock('@/api/generated', () => ({
  useGetTicketsTicketsByTicketIdQuery: vi.fn(),
  useLazyGetTicketsTicketsByTicketIdAttachmentsAndAttachmentIdQuery: vi.fn(),
}));

const ticketId = '019a7b3c-5d2e-7f10-8a4b-3c2d1e0f9a8b';
const ticket = {
  id: ticketId,
  subject: 'Printer offline',
  description: 'It shows error 42.',
  priority: 'High',
  raisedAt: '2026-11-12T09:30:00+00:00',
  attachments: [
    { id: 'f1', fileName: 'screenshot.png', contentType: 'image/png', size: 2048 },
    { id: 'f2', fileName: 'app.log', contentType: 'text/plain', size: 512 },
  ],
};
const download = vi.fn();
const content = new Blob(['the log']);

beforeEach(() => {
  vi.mocked(useGetTicketsTicketsByTicketIdQuery).mockReturnValue({
    data: ticket,
    isLoading: false,
    isError: false,
  } as never);
  download.mockReset();
  download.mockReturnValue({ unwrap: () => Promise.resolve(content) });
  vi.mocked(useLazyGetTicketsTicketsByTicketIdAttachmentsAndAttachmentIdQuery).mockReturnValue([download] as never);
});

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function renderTicket(path = `/tickets/${ticketId}`) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/tickets/:ticketId" element={<TicketPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

test('a ticket shows its subject, description, priority and attachments', { timeout: 20_000 }, () => {
  renderTicket();

  expect(useGetTicketsTicketsByTicketIdQuery).toHaveBeenCalledWith({ ticketId });
  expect(screen.getByRole('heading', { name: 'Printer offline' })).toBeInTheDocument();
  expect(screen.getByText('It shows error 42.')).toBeInTheDocument();
  expect(screen.getByText('High')).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Download screenshot.png' })).toBeInTheDocument();
  expect(screen.getByRole('button', { name: 'Download app.log' })).toBeInTheDocument();
});

test('downloading an attachment saves its content under its file name', async () => {
  const user = userEvent.setup();
  const createObjectURL = vi.fn(() => 'blob:the-log');
  const revokeObjectURL = vi.fn();
  vi.stubGlobal('URL', Object.assign(URL, { createObjectURL, revokeObjectURL }));
  const clicked: { href: string; download: string }[] = [];
  const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
    clicked.push({ href: this.href, download: this.download });
  });
  renderTicket();

  await user.click(screen.getByRole('button', { name: 'Download app.log' }));

  expect(download).toHaveBeenCalledWith({ ticketId, attachmentId: 'f2' });
  expect(createObjectURL).toHaveBeenCalledWith(content);
  expect(clicked).toEqual([{ href: 'blob:the-log', download: 'app.log' }]);
  expect(revokeObjectURL).toHaveBeenCalledWith('blob:the-log');
  click.mockRestore();
});

test('the ticket has no accessibility violations axe can find', async () => {
  const { container } = renderTicket();

  expect(await axe(container)).toHaveNoViolations();
});

test('an address whose ticket id is not a GUID never reaches the API', () => {
  vi.mocked(useGetTicketsTicketsByTicketIdQuery).mockReturnValue({
    data: undefined,
    isLoading: false,
    isError: false,
  } as never);
  renderTicket('/tickets/not-a-guid');

  expect(useGetTicketsTicketsByTicketIdQuery).toHaveBeenCalledWith(skipToken);
  expect(useGetTicketsTicketsByTicketIdQuery).not.toHaveBeenCalledWith({ ticketId: 'not-a-guid' });
  expect(screen.getByRole('alert')).toHaveTextContent('There is no such ticket.');
  expect(screen.getByRole('link', { name: 'Back to the tickets' })).toBeInTheDocument();
});

test('an address with no such ticket has no accessibility violations axe can find', async () => {
  vi.mocked(useGetTicketsTicketsByTicketIdQuery).mockReturnValue({
    data: undefined,
    isLoading: false,
    isError: false,
  } as never);
  const { container } = renderTicket('/tickets/not-a-guid');
  expect(screen.getByRole('alert')).toHaveTextContent('There is no such ticket.');

  expect(await axe(container)).toHaveNoViolations();
});
