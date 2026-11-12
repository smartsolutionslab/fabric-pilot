import { skipToken } from '@reduxjs/toolkit/query';
import { Link, useParams } from 'react-router';
import { z } from 'zod';
import { useGetTicketsTicketsByTicketIdQuery } from '@/api/generated';
import { AttachmentDownloadButton } from './AttachmentDownloadButton';

const ticketIdSchema = z.uuid();

/** One ticket as the customer raised it, with its files to download. */
export function TicketPage() {
  const ticketId = ticketIdSchema.safeParse(useParams().ticketId);
  const { data: ticket, isError } = useGetTicketsTicketsByTicketIdQuery(
    ticketId.success ? { ticketId: ticketId.data } : skipToken,
  );

  if (!ticketId.success) {
    return (
      <main className="p-4">
        <p role="alert">There is no such ticket.</p>
        <Link to="..">Back to the tickets</Link>
      </main>
    );
  }

  if (isError) {
    return (
      <main className="p-4">
        <p role="alert">The ticket could not be loaded.</p>
        <Link to="..">Back to the tickets</Link>
      </main>
    );
  }
  if (!ticket) {
    return (
      <main className="p-4">
        <p>Loading…</p>
      </main>
    );
  }

  return (
    <main className="mx-auto flex max-w-2xl flex-col gap-4 p-4">
      <Link to="..">Back to the tickets</Link>
      <h1 className="text-2xl font-semibold">{ticket.subject}</h1>
      <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
        <dt className="font-medium">Priority</dt>
        <dd>{ticket.priority}</dd>
        <dt className="font-medium">Raised</dt>
        <dd>{new Date(ticket.raisedAt).toLocaleString()}</dd>
      </dl>
      <p className="whitespace-pre-wrap">{ticket.description}</p>
      <h2 className="text-lg font-semibold">Attachments</h2>
      {ticket.attachments.length === 0 ? (
        <p>No attachments.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {ticket.attachments.map((attachment) => (
            <li key={attachment.id} className="flex items-center gap-3">
              <AttachmentDownloadButton ticketId={ticket.id} attachment={attachment} />
              <span className="text-sm text-muted-foreground">{Math.ceil(Number(attachment.size) / 1024)} KB</span>
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
