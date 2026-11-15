import type { TicketStatus } from '@/api/generated';

/** A ticket's statuses in the order a ticket passes through them, each with how a person reads it. */
export const ticketStatuses: readonly { value: TicketStatus; label: string }[] = [
  { value: 'Open', label: 'Open' },
  { value: 'InProgress', label: 'In progress' },
  { value: 'Resolved', label: 'Resolved' },
  { value: 'Closed', label: 'Closed' },
];

/** How a person reads the status the API names; a name this screen does not know is shown as the API sent it. */
export const statusLabel = (status: string) => ticketStatuses.find(({ value }) => value === status)?.label ?? status;
