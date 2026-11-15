// The example data BillingPage's axe check shows the page with, by hook of the generated client, for
// instance { useGetInvoicesQuery: { items: [{ number: 'INV-1' }], totalCount: 1, page: 1, pageSize: 20 } }. Without an
// example, BillingPage.axe.test.tsx checks the page only as it loads.
export const examples: Record<string, unknown> = {
  useGetBillingInvoicesQuery: {
    items: [
      {
        id: 'i1',
        number: '2026-0041',
        issuedOn: '2026-10-01',
        dueOn: '2026-10-31',
        currency: 'EUR',
        total: 25,
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
  },
  useGetBillingPermissionsManageQuery: {
    name: 'billing.manage',
  },
};
