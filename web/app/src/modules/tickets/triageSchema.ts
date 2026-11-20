import { z } from 'zod';

export const statusSchema = z.object({ status: z.enum(['Open', 'InProgress', 'Resolved', 'Closed']) });

/** A colleague is picked from the staff by user name; the API refuses anyone else. */
export const colleagueSchema = z.object({
  assignee: z.string().min(1, 'Choose a colleague.'),
});

export type StatusForm = z.input<typeof statusSchema>;
export type ColleagueForm = z.input<typeof colleagueSchema>;
