import { expect, test } from 'vitest';
import { readResponse, withFiles } from './baseApi';

test('a body that carries files is sent as form data, one field per value and one part per file', () => {
  const screenshot = new File(['png'], 'screenshot.png', { type: 'image/png' });
  const log = new File(['log'], 'app.log', { type: 'text/plain' });

  const body = withFiles({ subject: 'Printer offline', priority: 'High', attachments: [screenshot, log] });

  expect(body).toBeInstanceOf(FormData);
  const form = body as FormData;
  expect(form.get('subject')).toBe('Printer offline');
  expect(form.get('priority')).toBe('High');
  expect(form.getAll('attachments').map((part) => (part as File).name)).toEqual(['screenshot.png', 'app.log']);
});

test('a body without files is left as it is, to be sent as JSON', () => {
  const body = { name: 'plain' };

  expect(withFiles(body)).toBe(body);
  expect(withFiles(undefined)).toBeUndefined();
});

test('a JSON answer is read as JSON, an empty one as null', async () => {
  expect(
    await readResponse(new Response('{"id":1}', { headers: { 'Content-Type': 'application/json; charset=utf-8' } })),
  ).toEqual({ id: 1 });
  expect(
    await readResponse(new Response('{"code":"x"}', { headers: { 'Content-Type': 'application/problem+json' } })),
  ).toEqual({ code: 'x' });
  expect(await readResponse(new Response(null, { status: 204 }))).toBeNull();
});

test('a file is read as its bytes, unchanged', async () => {
  const bytes = new Uint8Array([0, 255, 13, 10, 128]);

  const body = await readResponse(new Response(bytes, { headers: { 'Content-Type': 'application/octet-stream' } }));

  expect(new Uint8Array(await (body as Blob).arrayBuffer())).toEqual(bytes);
});
