import { vi } from 'vitest';

/** Whether the screen is shown as it first renders or, once its data came, with the examples in its data file. */
export type Shown = { withExamples: boolean };

/**
 * The generated client as a hand-written screen's axe check renders it, the way each generated `<Name>Page.axe.test.tsx`
 * does: every query hook answers as if its request were still on its way or, while `shown.withExamples` is set, with
 * its example from the screen's `<Name>Page.data.ts`; a mutation nobody triggered is idle.
 */
export function standInForTheGeneratedClient(
  generated: Record<string, unknown>,
  examples: Record<string, unknown>,
  shown: Shown,
): Record<string, unknown> {
  const state = (data: unknown, loading: boolean) => ({
    data,
    currentData: data,
    error: undefined,
    isLoading: loading,
    isFetching: loading,
    isSuccess: !loading,
    isError: false,
  });
  const answer = (hook: string) =>
    shown.withExamples && hook in examples ? state(examples[hook], false) : state(undefined, true);
  const idle = {
    data: undefined,
    error: undefined,
    isUninitialized: true,
    isLoading: false,
    isSuccess: false,
    isError: false,
    reset: vi.fn(),
  };
  return Object.fromEntries(
    Object.entries(generated).map(([name, value]) => {
      if (/^use\w*Mutation$/.test(name)) {
        return [name, () => [vi.fn(), idle]];
      }
      if (/^useLazy\w*Query$/.test(name)) {
        return [name, () => [vi.fn(), answer(name)]];
      }
      return [name, /^use\w*Query$/.test(name) ? () => answer(name) : value];
    }),
  );
}
