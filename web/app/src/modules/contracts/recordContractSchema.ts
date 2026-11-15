import { z } from 'zod';

/** A contract's own limits, as the API checks them (Contract.Conclude in the contracts module). */
export const recordContractSchema = z
  .object({
    tenant: z.string().min(1, 'Choose a tenant.'),
    product: z.string().trim().min(1, 'Name the product.').max(200, 'Keep the product to 200 characters.'),
    startsOn: z.iso.date('Give the first day.'),
    endsOn: z.iso.date('Give the last day.'),
    renews: z.boolean(),
  })
  // ISO dates compare as text in the order of the days they name.
  .refine((contract) => contract.endsOn >= contract.startsOn, {
    path: ['endsOn'],
    message: 'The last day cannot come before the first.',
  });

export type RecordContractForm = z.input<typeof recordContractSchema>;
