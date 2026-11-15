import { cleanup, render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { examples } from './StaffTicketPage.data';
import { StaffTicketPage } from './StaffTicketPage';

// The screen's axe check, beside it as AGENTS.md asks of a screen written by hand. It renders the screen without the
// store or the host, as the generated <Name>Page.axe.test.tsx files do: each hook of the generated client answers as if
// its request were still on its way or, once the page is shown with its data, with its example in the data file.
const shown = vi.hoisted(() => ({ withExamples: false }));
vi.mock('@/api/generated', async (importOriginal) => {
  const { standInForTheGeneratedClient } = await import('@/test/standInForTheGeneratedClient');
  const { examples } = await import('@/modules/tickets/StaffTicketPage.data');
  return standInForTheGeneratedClient(await importOriginal(), examples, shown);
});

// The triage controls name the signed-in staff member; outside the app there is no sign-in to read it from.
vi.mock('react-oidc-context', () => ({
  useAuth: () => ({ user: { profile: { preferred_username: 'staff' } } }),
}));

// Whatever else asks the host waits for an answer that never comes.
vi.stubGlobal('fetch', () => new Promise<Response>(() => undefined));

// Without Vitest's globals, Testing Library does not unmount between tests by itself.
afterEach(cleanup);
afterEach(() => {
  shown.withExamples = false;
});

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/tickets/019a7b3c-5d2e-7f10-8a4b-3c2d1e0f9a8b']}>
      <Routes>
        <Route path="/tickets/:ticketId" element={<StaffTicketPage />} />
      </Routes>
    </MemoryRouter>,
  ).container;
}

// Whichever test renders first in a file also pays for warming up jsdom, React and axe.
test('the Staff ticket page has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const container = renderPage();

  expect(await axe(container)).toHaveNoViolations();
});

// Until StaffTicketPage.data.ts has an example, the page is checked only as it loads.
test.runIf(Object.keys(examples).length > 0)('the Staff ticket page passes axe with its example data', async () => {
  shown.withExamples = true;
  const container = renderPage();
  expect(await screen.findByRole('heading', { name: 'Printer offline' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
