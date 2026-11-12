import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { afterEach, beforeEach, expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { usePostTicketsTicketsMutation } from '@/api/generated';
import { RaiseTicketPage } from './RaiseTicketPage';

vi.mock('@/api/generated', () => ({ usePostTicketsTicketsMutation: vi.fn() }));

const raise = vi.fn();
const tenMegabytes = 10 * 1024 * 1024;

beforeEach(() => {
  raise.mockReset();
  raise.mockReturnValue({ unwrap: () => Promise.resolve('new-ticket-id') });
  vi.mocked(usePostTicketsTicketsMutation).mockReturnValue([raise, { isLoading: false }] as never);
});

// Without Vitest's globals, Testing Library does not unmount between tests by itself.
afterEach(cleanup);

function renderForm() {
  return render(
    <MemoryRouter initialEntries={['/tickets/new']}>
      <Routes>
        <Route path="/tickets/new" element={<RaiseTicketPage />} />
        <Route path="/tickets" element={<h1>The ticket list</h1>} />
      </Routes>
    </MemoryRouter>,
  );
}

function file(name: string, size: number, type = 'application/octet-stream') {
  const content = new File(['x'], name, { type });
  Object.defineProperty(content, 'size', { value: size });
  return content;
}

test(
  'a ticket is raised with its subject, description, priority and files, then the list is shown',
  { timeout: 20_000 },
  async () => {
    const user = userEvent.setup();
    renderForm();
    const screenshot = file('screenshot.png', 2048, 'image/png');
    const log = file('app.log', 512, 'text/plain');

    await user.type(screen.getByLabelText('Subject'), 'Printer offline');
    await user.type(screen.getByLabelText('Description'), 'It shows error 42.');
    await user.selectOptions(screen.getByLabelText('Priority'), 'High');
    await user.upload(screen.getByLabelText('Attachments'), [screenshot, log]);
    await user.click(screen.getByRole('button', { name: 'Raise ticket' }));

    expect(await screen.findByRole('heading', { name: 'The ticket list' })).toBeInTheDocument();
    expect(raise).toHaveBeenCalledWith({
      body: {
        subject: 'Printer offline',
        description: 'It shows error 42.',
        priority: 'High',
        attachments: [screenshot, log],
      },
    });
  },
);

test('the priority is Normal unless the customer picks another', () => {
  renderForm();

  expect(screen.getByLabelText('Priority')).toHaveValue('Normal');
  expect(screen.getByRole('option', { name: 'Urgent' })).toBeInTheDocument();
});

test('a file over the size limit is refused with a message next to the field, and nothing is sent', async () => {
  const user = userEvent.setup();
  renderForm();

  await user.type(screen.getByLabelText('Subject'), 'Too big');
  await user.type(screen.getByLabelText('Description'), 'A disk image.');
  await user.upload(screen.getByLabelText('Attachments'), [file('small.txt', 10), file('disk.img', tenMegabytes + 1)]);
  await user.click(screen.getByRole('button', { name: 'Raise ticket' }));

  const message = 'disk.img is larger than 10 MB, the largest file a ticket accepts.';
  expect(await screen.findByRole('alert')).toHaveTextContent(message);
  expect(screen.getByLabelText('Attachments')).toHaveAccessibleDescription(message);
  expect(screen.getByLabelText('Attachments')).toHaveAttribute('aria-invalid', 'true');
  expect(raise).not.toHaveBeenCalled();
});

test('more than five files are refused next to the field', async () => {
  const user = userEvent.setup();
  renderForm();

  await user.type(screen.getByLabelText('Subject'), 'Many');
  await user.type(screen.getByLabelText('Description'), 'Six files.');
  await user.upload(
    screen.getByLabelText('Attachments'),
    [1, 2, 3, 4, 5, 6].map((number) => file(`${number}.txt`, 1)),
  );
  await user.click(screen.getByRole('button', { name: 'Raise ticket' }));

  expect(screen.getByLabelText('Attachments')).toHaveAccessibleDescription('A ticket takes at most 5 attachments.');
  expect(raise).not.toHaveBeenCalled();
});

test('a subject and a description are required', async () => {
  const user = userEvent.setup();
  renderForm();

  await user.click(screen.getByRole('button', { name: 'Raise ticket' }));

  expect(screen.getByLabelText('Subject')).toHaveAccessibleDescription('Give the ticket a subject.');
  expect(screen.getByLabelText('Description')).toHaveAccessibleDescription('Describe what happened.');
  expect(raise).not.toHaveBeenCalled();
});

test('a file the server refuses as too large is shown next to the field', async () => {
  const user = userEvent.setup();
  raise.mockReturnValue({
    unwrap: () =>
      Promise.reject({
        status: 422,
        data: {
          code: 'tickets.attachment-too-large',
          title: 'scan.pdf is larger than 10 MB, the largest file a ticket accepts.',
        },
      }),
  });
  renderForm();

  await user.type(screen.getByLabelText('Subject'), 'Scan');
  await user.type(screen.getByLabelText('Description'), 'The scan.');
  await user.upload(screen.getByLabelText('Attachments'), [file('scan.pdf', 100)]);
  await user.click(screen.getByRole('button', { name: 'Raise ticket' }));

  expect(await screen.findByRole('alert')).toHaveTextContent(
    'scan.pdf is larger than 10 MB, the largest file a ticket accepts.',
  );
  expect(screen.getByLabelText('Attachments')).toHaveAccessibleDescription(
    'scan.pdf is larger than 10 MB, the largest file a ticket accepts.',
  );
  expect(screen.queryByRole('heading', { name: 'The ticket list' })).not.toBeInTheDocument();
});

test('the form has no accessibility violations axe can find', async () => {
  const { container } = renderForm();

  expect(await axe(container)).toHaveNoViolations();
});
