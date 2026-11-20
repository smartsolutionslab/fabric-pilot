import { api, type NotificationsSampleDto, useGetNotificationsSamplesQuery, usePostNotificationsSamplesMutation } from '@/api/generated';

/** The page of samples the module's page lists: the first, as many as the host gives by default. */
export const samplesPage = { page: 1, pageSize: 20 };

let pending = 0;

/** The cached page with the new sample in it, by name as the host lists them, until the host's own answer replaces it. */
function withPending(samples: NotificationsSampleDto[], name: string): NotificationsSampleDto[] {
  pending += 1;
  return [...samples, { id: `pending-${pending}`, name }].sort((first, second) =>
    first.name < second.name ? -1 : first.name > second.name ? 1 : 0,
  );
}

// A created sample shows in the list at once: it is put into the cached page as the request leaves, taken out again
// when the host refuses it, and the page is asked for again once the host has it, with the id the host gave it.
api.enhanceEndpoints({
  endpoints: {
    postNotificationsSamples: {
      async onQueryStarted({ createNotificationsSampleRequest }, { dispatch, queryFulfilled }) {
        const patch = dispatch(
          api.util.updateQueryData('getNotificationsSamples', samplesPage, (page) => {
            page.items = withPending(page.items, createNotificationsSampleRequest.name);
            page.totalCount = Number(page.totalCount) + 1;
          }),
        );
        try {
          await queryFulfilled;
          void dispatch(
            api.endpoints.getNotificationsSamples.initiate(samplesPage, { subscribe: false, forceRefetch: true }),
          );
        } catch {
          patch.undo();
        }
      },
    },
  },
});

/** The module's samples, a page at a time, and the page they are of: undefined until the host answers. */
export function useNotificationsSamples() {
  const { data, isLoading, isError } = useGetNotificationsSamplesQuery(samplesPage);
  return { samples: data?.items, page: data, isLoading, isError };
}

/** Creates a sample; resolves to whether the host created it. */
export function useCreateNotificationsSample() {
  const [post, { isLoading }] = usePostNotificationsSamplesMutation();
  const create = async (name: string) => !('error' in (await post({ createNotificationsSampleRequest: { name } })));
  return { create, isSaving: isLoading };
}
