import { cleanup, render } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { SignedInAsDeveloper } from '@/auth/SignedInAsDeveloper';
import * as data from './TicketsPage.data';
import { TicketsPage } from './TicketsPage';

// The screen's axe check, generated with it. It runs whatever the screen's own component test does, so a check skipped
// or commented out there never leaves the screen unchecked. It asks nothing of what the screen shows, only that axe
// finds no violations in it: what the screen does is its own tests'.

// The page renders on its own, without the store or the host. Each hook of the generated client answers
// as if its request were still on its way or, once the page is shown with data, with its example in the data file:
// examples, or emptyExamples for the page with nothing to show.
const shown = vi.hoisted(() => ({ examples: undefined as Record<string, unknown> | undefined }));
vi.mock('@/api/generated', async (importOriginal) => {
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
    shown.examples !== undefined && hook in shown.examples
      ? state(shown.examples[hook], false)
      : state(undefined, true);
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

const { examples } = data;
// A data file written before it had an empty example has none: the page is then not checked with nothing to show.
const emptyExamples = (data as { emptyExamples?: Record<string, unknown> }).emptyExamples ?? {};

// Whatever else asks the host waits for an answer that never comes.
vi.stubGlobal('fetch', () => new Promise<Response>(() => undefined));

// Without Vitest's globals, Testing Library does not unmount between tests by itself.
afterEach(cleanup);
afterEach(() => {
  shown.examples = undefined;
});

// The app's other themes: each check runs again in each, switched on as the app switches it before the screen renders.
const themes = [
  {
    name: 'dark',
    // As the theme toggle does: the choice stored, the class on <html>.
    switchOn: () => {
      localStorage.setItem('theme', 'dark');
      document.documentElement.classList.add('dark');
    },
  },
];

afterEach(() => {
  document.documentElement.removeAttribute('class');
  localStorage.clear();
});

// The screen has rendered when render returns.
// The screen renders for the local user developer, signed in, without the sign-in provider: the check sees the screen
// as the developer does.
function renderPage() {
  return render(
    <SignedInAsDeveloper>
      <MemoryRouter>
        <TicketsPage />
      </MemoryRouter>
    </SignedInAsDeveloper>,
  ).container;
}

// Whichever test renders first in a file also pays for warming up jsdom, React and axe, which in a larger project under
// parallel load passes 2.5 s of the default 5 s.
test('the Tickets page has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const container = renderPage();

  expect(await axe(container)).toHaveNoViolations();
});

test.each(themes)(
  'the Tickets page has no accessibility violations axe can find in the $name theme',
  async ({ switchOn }) => {
    switchOn();
    const container = renderPage();

    expect(await axe(container)).toHaveNoViolations();
  },
);

// Until TicketsPage.data.ts has an example (AGENTS.md), the page is checked only as it loads.
test.runIf(Object.keys(examples).length > 0)('the Tickets page passes axe with its example data', async () => {
  shown.examples = examples;
  const container = renderPage();

  expect(await axe(container)).toHaveNoViolations();
});

test.runIf(Object.keys(examples).length > 0).each(themes)(
  'the Tickets page passes axe with its example data in the $name theme',
  async ({ switchOn }) => {
    shown.examples = examples;
    switchOn();
    const container = renderPage();

    expect(await axe(container)).toHaveNoViolations();
  },
);

// Until TicketsPage.data.ts has an empty example (AGENTS.md), the page is not checked with nothing to show.
test.runIf(Object.keys(emptyExamples).length > 0)(
  'the Tickets page passes axe when it has nothing to show',
  async () => {
    shown.examples = emptyExamples;
    const container = renderPage();

    expect(await axe(container)).toHaveNoViolations();
  },
);

test.runIf(Object.keys(emptyExamples).length > 0).each(themes)(
  'the Tickets page passes axe when it has nothing to show in the $name theme',
  async ({ switchOn }) => {
    shown.examples = emptyExamples;
    switchOn();
    const container = renderPage();

    expect(await axe(container)).toHaveNoViolations();
  },
);
