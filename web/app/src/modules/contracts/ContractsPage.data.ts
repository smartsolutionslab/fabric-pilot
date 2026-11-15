// The example data ContractsPage's axe check shows the page with, by hook of the generated client, for
// instance { useGetInvoicesQuery: { items: [{ number: 'INV-1' }], totalCount: 1, page: 1, pageSize: 20 } }. Without an
// example, ContractsPage.axe.test.tsx checks the page only as it loads.
export const examples: Record<string, unknown> = {
  useGetContractsContractsQuery: {
    items: [
      {
        id: 'c1',
        product: 'Managed printing',
        startsOn: '2026-01-01',
        endsOn: '2026-12-31',
        renewsOn: '2026-12-01',
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
  },
  useGetContractsPermissionsManageQuery: {
    name: 'contracts.manage',
  },
};
