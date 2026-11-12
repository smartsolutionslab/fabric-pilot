import { z } from 'zod';

/** The ticket's own limits, as the API checks them (Ticket and Attachment in the tickets module). */
export const maximumAttachments = 5;
export const maximumFileSize = 10 * 1024 * 1024;
export const priorities = ['Low', 'Normal', 'High', 'Urgent'] as const;

const files = z
  .preprocess(
    (value) => (value instanceof FileList ? Array.from(value) : (value ?? [])),
    z.array(z.instanceof(File)).max(maximumAttachments, `A ticket takes at most ${maximumAttachments} attachments.`),
  )
  .superRefine((attachments, context) => {
    const tooLarge = attachments.find((file) => file.size > maximumFileSize);
    if (tooLarge) {
      context.addIssue({
        code: 'custom',
        message: `${tooLarge.name} is larger than ${maximumFileSize / (1024 * 1024)} MB, the largest file a ticket accepts.`,
      });
    }
  });

export const raiseTicketSchema = z.object({
  subject: z.string().trim().min(1, 'Give the ticket a subject.').max(200, 'Keep the subject to 200 characters.'),
  description: z
    .string()
    .trim()
    .min(1, 'Describe what happened.')
    .max(4000, 'Keep the description to 4000 characters.'),
  priority: z.enum(priorities),
  attachments: files,
});

export type RaiseTicketForm = z.input<typeof raiseTicketSchema>;
