// The example data AllTenantsTicketsPage's axe check shows the page with, by hook of the generated client (AGENTS.md).
export const examples: Record<string, unknown> = {
  useGetTicketsTicketsAllTenantsQuery: {
    items: [
      {
        id: '019a7b3c-5d2e-7f10-8a4b-3c2d1e0f9a8b',
        tenant: 'tenant-a',
        subject: 'Printer offline',
        priority: 'High',
        status: 'Open',
        assignee: null,
        raisedAt: '2026-11-12T09:30:00+00:00',
        attachmentCount: 1,
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
  },
  useGetTenantsQuery: { items: ['tenant-a', 'tenant-b'], totalCount: 2, page: 1, pageSize: 100 },
};
