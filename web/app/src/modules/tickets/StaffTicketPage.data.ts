// The example data StaffTicketPage's axe check shows the page with, by hook of the generated client (AGENTS.md).
export const examples: Record<string, unknown> = {
  useGetIdentityStaffQuery: {
    items: [
      { userName: 'staff', firstName: 'Local', lastName: 'Staff' },
      { userName: 'staff-b', firstName: 'Second', lastName: 'Staff' },
    ],
    totalCount: 2,
    page: 1,
    pageSize: 100,
  },
  useGetTicketsTicketsAllTenantsByTicketIdQuery: {
    id: '019a7b3c-5d2e-7f10-8a4b-3c2d1e0f9a8b',
    tenant: 'tenant-a',
    subject: 'Printer offline',
    description: 'It shows error 42.',
    priority: 'High',
    status: 'Open',
    assignee: null,
    raisedAt: '2026-11-12T09:30:00+00:00',
    attachments: [
      {
        id: 'f1',
        fileName: 'screenshot.png',
        contentType: 'image/png',
        size: 2048,
      },
    ],
  },
};
