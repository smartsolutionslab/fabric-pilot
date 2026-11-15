import { z } from 'zod';

/** The ticket's own limit on an assignee's user name, as the API checks it (Ticket in the tickets module). */
export const maximumAssigneeLength = 255;

export const statusSchema = z.object({ status: z.enum(['Open', 'InProgress', 'Resolved', 'Closed']) });

export const colleagueSchema = z.object({
  assignee: z
    .string()
    .trim()
    .min(1, 'Enter the colleague’s user name.')
    .max(maximumAssigneeLength, `Keep the user name to ${maximumAssigneeLength} characters.`),
});

export type StatusForm = z.input<typeof statusSchema>;
export type ColleagueForm = z.input<typeof colleagueSchema>;
