import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useAuth } from 'react-oidc-context';
import {
  type StaffTicketDto,
  type TicketStatus,
  useGetIdentityStaffQuery,
  usePutTicketsTicketsByTenantAndTicketIdAssigneeMutation,
  usePutTicketsTicketsByTenantAndTicketIdStatusMutation,
} from '@/api/generated';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { NativeSelect } from '@/components/ui/native-select';
import { colleagueSchema, statusSchema } from './triageSchema';
import { statusLabel, ticketStatuses } from './ticketStatuses';

type Outcome = { done: string } | { failed: string } | undefined;

/** The most staff the picker lists: one page at its largest size. */
const staffPage = { page: 1, pageSize: 100 };

/**
 * Staff move a ticket to another status or assign it, to themselves or to a colleague picked from the staff, in the
 * ticket's own tenant. The customer who raised it is told of a status change; the page shows the ticket again once it
 * changed.
 */
export function TicketTriage({ ticket }: { ticket: StaffTicketDto }) {
  const { user } = useAuth();
  const me = user?.profile.preferred_username;
  const [outcome, setOutcome] = useState<Outcome>();
  const [changeStatus, { isLoading: isChangingStatus }] = usePutTicketsTicketsByTenantAndTicketIdStatusMutation();
  const [assign, { isLoading: isAssigning }] = usePutTicketsTicketsByTenantAndTicketIdAssigneeMutation();
  const staff = useGetIdentityStaffQuery(staffPage);
  const colleagues = (staff.data?.items ?? []).filter((member) => member.userName !== me);
  const statusForm = useForm({
    resolver: zodResolver(statusSchema),
    defaultValues: { status: ticket.status as TicketStatus },
  });
  const colleagueForm = useForm({ resolver: zodResolver(colleagueSchema), defaultValues: { assignee: '' } });
  const colleagueError = colleagueForm.formState.errors.assignee;
  const { tenant, id: ticketId } = ticket;

  const onChangeStatus = statusForm.handleSubmit(async ({ status }) => {
    setOutcome(undefined);
    try {
      await changeStatus({ tenant, ticketId, ticketStatusChange: { status } }).unwrap();
      setOutcome({ done: `The status is now ${statusLabel(status)}.` });
    } catch {
      setOutcome({ failed: 'The status could not be changed.' });
    }
  });

  const assignTo = async (assignee: string) => {
    setOutcome(undefined);
    try {
      await assign({ tenant, ticketId, ticketAssignment: { assignee } }).unwrap();
      setOutcome({ done: `Assigned to ${assignee}.` });
    } catch {
      setOutcome({ failed: 'The ticket could not be assigned.' });
    }
  };

  const onAssignColleague = colleagueForm.handleSubmit(async ({ assignee }) => {
    await assignTo(assignee);
    colleagueForm.reset();
  });

  return (
    <section aria-labelledby="triage-heading" className="flex flex-col gap-3 rounded-md border p-4">
      <h2 id="triage-heading" className="text-lg font-semibold">
        Triage
      </h2>
      <form noValidate onSubmit={(event) => void onChangeStatus(event)} className="flex items-end gap-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor="status">Status</Label>
          <NativeSelect id="status" {...statusForm.register('status')}>
            {ticketStatuses.map(({ value, label }) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </NativeSelect>
        </div>
        <Button type="submit" disabled={isChangingStatus}>
          Change status
        </Button>
      </form>
      <p>{ticket.assignee ? `Assigned to ${ticket.assignee}.` : 'Not assigned yet.'}</p>
      {me ? (
        <Button variant="outline" className="self-start" disabled={isAssigning} onClick={() => void assignTo(me)}>
          Assign to me
        </Button>
      ) : null}
      <form noValidate onSubmit={(event) => void onAssignColleague(event)} className="flex items-end gap-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor="assignee">Colleague</Label>
          <NativeSelect
            id="assignee"
            disabled={!staff.data}
            aria-invalid={Boolean(colleagueError)}
            aria-describedby={colleagueError ? 'assignee-error' : undefined}
            {...colleagueForm.register('assignee')}
          >
            <option value="">Choose a colleague</option>
            {colleagues.map(({ userName, firstName, lastName }) => (
              <option key={userName} value={userName}>
                {`${firstName} ${lastName} (${userName})`}
              </option>
            ))}
          </NativeSelect>
        </div>
        <Button type="submit" variant="outline" disabled={isAssigning || !staff.data}>
          Assign to colleague
        </Button>
      </form>
      {staff.isError ? (
        <p role="alert" className="text-sm text-destructive">
          The staff could not be listed.
        </p>
      ) : null}
      {colleagueError ? (
        <p id="assignee-error" role="alert" className="text-sm text-destructive">
          {colleagueError.message}
        </p>
      ) : null}
      {outcome && 'done' in outcome ? <p role="status">{outcome.done}</p> : null}
      {outcome && 'failed' in outcome ? (
        <p role="alert" className="text-sm text-destructive">
          {outcome.failed}
        </p>
      ) : null}
    </section>
  );
}
