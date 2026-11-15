// The example data KnowledgePage's axe check shows the page with, by hook of the generated client, for
// instance { useGetInvoicesQuery: { items: [{ number: 'INV-1' }], totalCount: 1, page: 1, pageSize: 20 } }. Without an
// example, KnowledgePage.axe.test.tsx checks the page only as it loads.
export const examples: Record<string, unknown> = {
  useGetKnowledgeArticlesQuery: {
    items: [
      {
        id: 'b1',
        title: 'Printer offline',
        state: 'Published',
        changedAt: '2026-11-12T09:30:00+00:00',
        publishedAt: '2026-11-12T09:30:00+00:00',
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20,
  },
  useGetKnowledgePermissionsManageQuery: {
    name: 'knowledge.manage',
  },
};
