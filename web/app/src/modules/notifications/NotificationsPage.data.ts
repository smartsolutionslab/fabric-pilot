// The example data NotificationsPage's axe check shows the page with, by hook of the generated client, for
// instance { useGetInvoicesQuery: { items: [{ number: 'INV-1' }], totalCount: 1, page: 1, pageSize: 20 } }. Without an
// example, NotificationsPage.axe.test.tsx checks the page only as it loads.
export const examples: Record<string, unknown> = {
  useGetNotificationsNoticesQuery: {
    items: [
      {
        id: 'n1',
        ticketId: '019a7b3c-5d2e-7f10-8a4b-3c2d1e0f9a8b',
        text: 'Your ticket "Printer offline" is now InProgress.',
        createdAt: '2026-11-13T09:00:00+00:00',
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
  },
};
