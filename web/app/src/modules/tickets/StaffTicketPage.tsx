import { skipToken } from '@reduxjs/toolkit/query';
import { Link, useParams } from 'react-router';
import { z } from 'zod';
import { useGetTicketsTicketsAllTenantsByTicketIdQuery } from '@/api/generated';
import { SuggestedAnswer } from './SuggestedAnswer';

const ticketIdSchema = z.uuid();

/**
 * One tenant's ticket as staff read it, across tenants, with an answer to suggest. Read only: its files are listed but
 * stay the tenant's to download.
 */
export function StaffTicketPage() {
  const ticketId = ticketIdSchema.safeParse(useParams().ticketId);
  const { data: ticket, isError } = useGetTicketsTicketsAllTenantsByTicketIdQuery(
    ticketId.success ? { ticketId: ticketId.data } : skipToken,
  );

  if (!ticketId.success || isError) {
    return (
      <main className="p-4">
        <p role="alert">{ticketId.success ? 'The ticket could not be loaded.' : 'There is no such ticket.'}</p>
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
        <ul className="flex flex-col gap-1 text-sm">
          {ticket.attachments.map((attachment) => (
            <li key={attachment.id}>{attachment.fileName}</li>
          ))}
        </ul>
      )}
      <SuggestedAnswer question={`${ticket.subject}\n\n${ticket.description}`} />
    </main>
  );
}
