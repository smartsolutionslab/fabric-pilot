import { readFile } from 'node:fs/promises';
import { expect, type Page, test } from '@playwright/test';

/** Every byte value, over and over: a file a lossy round trip (text decoding, a changed line ending) would change. */
const screenshot = Buffer.from(Array.from({ length: 70_000 }, (_, index) => index % 256));
const log = Buffer.from('2026-11-12 09:30 error 42\r\n2026-11-12 09:31 retry\n');

async function signInAndOpenTickets(page: Page) {
  await page.goto('/');
  await page.getByLabel('Username or email').fill('developer');
  await page.getByLabel('Password', { exact: true }).fill('developer');
  await page.getByRole('button', { name: 'Sign In' }).click();
  await page.getByRole('navigation', { name: 'Modules' }).getByRole('link', { name: 'Tickets' }).click();
  await page.getByRole('link', { name: 'Raise a ticket' }).click();
}

test('a customer raises a ticket with two attachments, sees it in their list and downloads both unchanged', async ({
  page,
}) => {
  const subject = `Printer offline ${Date.now()}`;
  await signInAndOpenTickets(page);

  await page.getByLabel('Subject').fill(subject);
  await page.getByLabel('Description').fill('It shows error 42 after switching on.');
  await page.getByLabel('Priority').selectOption('High');
  await page.getByLabel('Attachments').setInputFiles([
    { name: 'screenshot.png', mimeType: 'image/png', buffer: screenshot },
    { name: 'app.log', mimeType: 'text/plain', buffer: log },
  ]);
  await page.getByRole('button', { name: 'Raise ticket' }).click();

  const row = page.getByRole('row', { name: new RegExp(subject) });
  await expect(row).toBeVisible();
  await expect(row.getByRole('cell', { name: 'High' })).toBeVisible();
  await row.getByRole('link', { name: subject }).click();
  await expect(page.getByRole('heading', { name: subject })).toBeVisible();

  for (const [fileName, content] of [
    ['screenshot.png', screenshot],
    ['app.log', log],
  ] as const) {
    const downloading = page.waitForEvent('download');
    await page.getByRole('button', { name: `Download ${fileName}` }).click();
    const download = await downloading;

    expect(download.suggestedFilename()).toBe(fileName);
    expect(Buffer.compare(await readFile(await download.path()), content)).toBe(0);
  }
});

test('a file over the size limit is refused with a message next to the field', async ({ page }) => {
  await signInAndOpenTickets(page);

  await page.getByLabel('Subject').fill(`Too big ${Date.now()}`);
  await page.getByLabel('Description').fill('A disk image.');
  await page
    .getByLabel('Attachments')
    .setInputFiles([
      { name: 'disk.img', mimeType: 'application/octet-stream', buffer: Buffer.alloc(10 * 1024 * 1024 + 1) },
    ]);
  await page.getByRole('button', { name: 'Raise ticket' }).click();

  const message = 'disk.img is larger than 10 MB, the largest file a ticket accepts.';
  await expect(page.getByLabel('Attachments')).toHaveAccessibleDescription(message);
  await expect(page.getByRole('alert')).toHaveText(message);
  await expect(page.getByRole('button', { name: 'Raise ticket' })).toBeVisible();
});
