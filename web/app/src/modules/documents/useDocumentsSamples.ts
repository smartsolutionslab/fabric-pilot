import { api, type DocumentsSampleDto, useGetDocumentsSamplesQuery, usePostDocumentsSamplesMutation } from '@/api/generated';

/** The page of samples the module's page lists: the first, as many as the host gives by default. */
export const samplesPage = { page: 1, pageSize: 20 };

let pending = 0;

/** The cached page with the new sample in it, by name as the host lists them, until the host's own answer replaces it. */
function withPending(samples: DocumentsSampleDto[], name: string): DocumentsSampleDto[] {
  pending += 1;
  return [...samples, { id: `pending-${pending}`, name }].sort((first, second) =>
    first.name < second.name ? -1 : first.name > second.name ? 1 : 0,
  );
}

// A created sample shows in the list at once: it is put into the cached page as the request leaves, taken out again
// when the host refuses it, and the page is asked for again once the host has it, with the id the host gave it.
api.enhanceEndpoints({
  endpoints: {
    postDocumentsSamples: {
      async onQueryStarted({ createDocumentsSampleRequest }, { dispatch, queryFulfilled }) {
        const patch = dispatch(
          api.util.updateQueryData('getDocumentsSamples', samplesPage, (page) => {
            page.items = withPending(page.items, createDocumentsSampleRequest.name);
            page.totalCount = Number(page.totalCount) + 1;
          }),
        );
        try {
          await queryFulfilled;
          void dispatch(
            api.endpoints.getDocumentsSamples.initiate(samplesPage, { subscribe: false, forceRefetch: true }),
          );
        } catch {
          patch.undo();
        }
      },
    },
  },
});

/** The module's samples, a page at a time, and the page they are of: undefined until the host answers. */
export function useDocumentsSamples() {
  const { data, isLoading, isError } = useGetDocumentsSamplesQuery(samplesPage);
  return { samples: data?.items, page: data, isLoading, isError };
}

/** Creates a sample; resolves to whether the host created it. */
export function useCreateDocumentsSample() {
  const [post, { isLoading }] = usePostDocumentsSamplesMutation();
  const create = async (name: string) => !('error' in (await post({ createDocumentsSampleRequest: { name } })));
  return { create, isSaving: isLoading };
}
