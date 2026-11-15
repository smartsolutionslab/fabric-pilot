import { cleanup, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { examples } from './IdentityPage.data';
import { IdentityPage } from './IdentityPage';

// The screen's axe check, generated with it. It runs whatever the screen's own component test does, so a check skipped
// or commented out there never leaves the screen unchecked.

// The page renders on its own, without the store or the host. Each hook of the generated client answers
// as if its request were still on its way or, once the page is shown with its data, with its example in the data file.
const shown = vi.hoisted(() => ({ withExamples: false, refusing: '', failing: '' }));
vi.mock('@/api/generated', async (importOriginal) => {
  const { examples } = await import('@/modules/identity/IdentityPage.data');
  const state = (data: unknown, loading: boolean) => ({
    data,
    currentData: data,
    error: undefined,
    isLoading: loading,
    isFetching: loading,
    isSuccess: !loading,
    isError: false,
  });
  // A request that failed, as RTK Query reports it.
  const failure = (status: number) => ({
    ...state(undefined, false),
    error: { status, data: '' },
    isSuccess: false,
    isError: true,
  });
  const answer = (hook: string) => {
    // A request the host refused: what a permission probe answers a caller without the permission.
    if (hook === shown.refusing) {
      return failure(403);
    }
    // A request that failed for any other reason.
    if (hook === shown.failing) {
      return failure(500);
    }
    return shown.withExamples && hook in examples ? state(examples[hook], false) : state(undefined, true);
  };
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
  shown.refusing = '';
  shown.failing = '';
});

async function renderPage() {
  const { container } = render(
    <MemoryRouter>
      <IdentityPage />
    </MemoryRouter>,
  );
  expect(await screen.findByRole('heading', { name: 'Identity' })).toBeInTheDocument();
  return container;
}

// Whichever test renders first in a file also pays for warming up jsdom, React and axe, which in a larger project under
// parallel load passes 2.5 s of the default 5 s.
test('the Identity page has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const container = await renderPage();

  expect(await axe(container)).toHaveNoViolations();
});

// Until IdentityPage.data.ts has an example (AGENTS.md), the page is checked only as it loads.
test.runIf(Object.keys(examples).length > 0)('the Identity page passes axe with its example data', async () => {
  shown.withExamples = true;
  const container = await renderPage();

  expect(await axe(container)).toHaveNoViolations();
});

// With an example for useGetIdentityPermissionsManageQuery in IdentityPage.data.ts, the probe answers 200 and the page
// shows "Manage".
const probeAnswers = 'useGetIdentityPermissionsManageQuery' in examples;

test.runIf(probeAnswers)('the Identity page offers Manage once its probe answers', async () => {
  shown.withExamples = true;
  await renderPage();

  expect(screen.getByRole('heading', { name: 'Manage' })).toBeInTheDocument();
});

test('the Identity page offers no Manage when the probe refuses', async () => {
  shown.withExamples = true;
  shown.refusing = 'useGetIdentityPermissionsManageQuery';
  await renderPage();

  expect(screen.queryByRole('heading', { name: 'Manage' })).not.toBeInTheDocument();
  expect(screen.queryByText('Permissions for identity could not be loaded.')).not.toBeInTheDocument();
});

test('the Identity page shows that its probe failed, not a refusal', async () => {
  shown.withExamples = true;
  shown.failing = 'useGetIdentityPermissionsManageQuery';
  const container = await renderPage();

  expect(screen.getByText('Permissions for identity could not be loaded.')).toHaveAttribute('role', 'alert');
  expect(screen.queryByRole('heading', { name: 'Manage' })).not.toBeInTheDocument();
  expect(await axe(container)).toHaveNoViolations();
});
