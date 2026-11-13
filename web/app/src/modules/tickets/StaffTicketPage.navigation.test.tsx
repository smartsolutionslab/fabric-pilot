import { configureStore } from '@reduxjs/toolkit';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { Link, MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { baseApi } from '@/api/baseApi';
import { StaffTicketPage } from './StaffTicketPage';

const firstId = '019a7b3c-5d2e-7f10-8a4b-3c2d1e0f9a8b';
const secondId = '019a7b3c-5d2e-7f10-8a4b-3c2d1e0f9a8c';
const ticketOf = (id: string, subject: string) => ({
  id,
  subject,
  description: 'It shows error 42.',
  priority: 'High',
  raisedAt: '2026-11-12T09:30:00+00:00',
  attachments: [],
});
const drafted = { drafted: true, text: 'Please restart the print spooler.', sources: [] };

const json = (body: unknown) => new Response(JSON.stringify(body), { headers: { 'Content-Type': 'application/json' } });

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

test('switching tickets hides the previous ticket and its draft until the next one has loaded', async () => {
  let answerSecond: (response: Response) => void = () => undefined;
  const secondAnswered = new Promise<Response>((resolve) => {
    answerSecond = resolve;
  });
  const fetch = vi.fn((request: Request) => {
    const url = new URL(request.url, 'http://localhost');
    if (url.pathname === `/api/tickets/tickets/all-tenants/${firstId}`) {
      return Promise.resolve(json(ticketOf(firstId, 'Printer offline')));
    }
    if (url.pathname === `/api/tickets/tickets/all-tenants/${secondId}`) {
      return secondAnswered;
    }
    if (url.pathname === '/api/knowledge/suggested-answers') {
      return Promise.resolve(json(drafted));
    }
    return Promise.resolve(new Response(null, { status: 404 }));
  });
  vi.stubGlobal('fetch', fetch);
  // Node's Request, which the tests run with, refuses the relative '/api/...' URLs a browser resolves against the page.
  vi.stubGlobal(
    'Request',
    class extends Request {
      constructor(input: RequestInfo | URL, init?: RequestInit) {
        super(typeof input === 'string' ? new URL(input, window.location.href) : input, init);
      }
    },
  );
  const store = configureStore({
    reducer: { [baseApi.reducerPath]: baseApi.reducer },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(baseApi.middleware),
  });
  const user = userEvent.setup();
  const { container } = render(
    <Provider store={store}>
      <MemoryRouter initialEntries={[`/tickets/${firstId}`]}>
        <Link to={`/tickets/${secondId}`}>Next ticket</Link>
        <Routes>
          <Route path="/tickets/:ticketId" element={<StaffTicketPage />} />
        </Routes>
      </MemoryRouter>
    </Provider>,
  );
  await screen.findByRole('heading', { name: 'Printer offline' });
  await user.click(screen.getByRole('button', { name: 'Suggest an answer' }));
  await screen.findByRole('textbox', { name: 'Draft answer' });

  await user.click(screen.getByRole('link', { name: 'Next ticket' }));
  await waitFor(() =>
    expect(fetch.mock.calls.map(([request]) => new URL(request.url, 'http://localhost').pathname)).toContain(
      `/api/tickets/tickets/all-tenants/${secondId}`,
    ),
  );

  expect(screen.queryByRole('heading', { name: 'Printer offline' })).not.toBeInTheDocument();
  expect(screen.queryByRole('textbox', { name: 'Draft answer' })).not.toBeInTheDocument();
  expect(screen.getByText('Loading…')).toBeInTheDocument();
  expect(await axe(container)).toHaveNoViolations();

  answerSecond(json(ticketOf(secondId, 'Password reset')));

  expect(await screen.findByRole('heading', { name: 'Password reset' })).toBeInTheDocument();
  expect(screen.queryByRole('textbox', { name: 'Draft answer' })).not.toBeInTheDocument();
});
