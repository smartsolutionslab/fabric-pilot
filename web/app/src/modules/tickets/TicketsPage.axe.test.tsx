import { cleanup, render } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { examples } from './TicketsPage.data';
import { TicketsPage } from './TicketsPage';

// The screen's axe check, generated with it. It runs whatever the screen's own component test does, so a check skipped
// or commented out there never leaves the screen unchecked. It asks nothing of what the screen shows, only that axe
// finds no violations in it: what the screen does is its own tests'.

// The page renders on its own, without the store or the host. Each hook of the generated client answers
// as if its request were still on its way or, once the page is shown with its data, with its example in the data file.
const shown = vi.hoisted(() => ({ withExamples: false }));
vi.mock('@/api/generated', async (importOriginal) => {
  const { examples } = await import('@/modules/tickets/TicketsPage.data');
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
  // A mutation nobody has triggered is idle, not saving: the page shows as it first does.
  const idle = {
    data: undefined,
    error: undefined,
    isUninitialized: true,
    isLoading: false,
    isSuccess: false,
    isError: false,
    reset: vi.fn(),
  };
  const generated = await importOriginal<Record<string, unknown>>();
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
});

// Whatever else asks the host waits for an answer that never comes.
vi.stubGlobal('fetch', () => new Promise<Response>(() => undefined));

// Without Vitest's globals, Testing Library does not unmount between tests by itself.
afterEach(cleanup);
afterEach(() => {
  shown.withExamples = false;
});

// The screen has rendered when render returns.
function renderPage() {
  return render(
    <MemoryRouter>
      <TicketsPage />
    </MemoryRouter>,
  ).container;
}

// Whichever test renders first in a file also pays for warming up jsdom, React and axe, which in a larger project under
// parallel load passes 2.5 s of the default 5 s.
test('the Tickets page has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const container = renderPage();

  expect(await axe(container)).toHaveNoViolations();
});

// Until TicketsPage.data.ts has an example (AGENTS.md), the page is checked only as it loads.
test.runIf(Object.keys(examples).length > 0)('the Tickets page passes axe with its example data', async () => {
  shown.withExamples = true;
  const container = renderPage();

  expect(await axe(container)).toHaveNoViolations();
});
