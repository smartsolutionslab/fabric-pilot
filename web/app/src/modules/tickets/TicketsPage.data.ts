// The example data TicketsPage's axe check shows the page with, by hook of the generated client, for
// instance { useGetInvoicesQuery: { items: [{ number: 'INV-1' }], totalCount: 1, page: 1, pageSize: 20 } }. Without an
// example, TicketsPage.axe.test.tsx checks the page only as it loads.
export const examples: Record<string, unknown> = {
  useGetTicketsTicketsQuery: {
    items: [
      {
        id: '019a7b3c-5d2e-7f10-8a4b-3c2d1e0f9a8b',
        subject: 'Printer offline',
        priority: 'High',
        raisedAt: '2026-11-12T09:30:00+00:00',
        attachmentCount: 1,
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
  },
  useGetTicketsPermissionsManageQuery: {
    name: 'tickets.manage',
  },
};
