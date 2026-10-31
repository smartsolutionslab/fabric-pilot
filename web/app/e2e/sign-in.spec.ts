import { expect, test } from '@playwright/test';

test('a realm user signs in and sees the application', async ({ page }) => {
  await page.goto('/');

  await page.getByLabel('Username or email').fill('developer');
  await page.getByLabel('Password', { exact: true }).fill('developer');
  await page.getByRole('button', { name: 'Sign In' }).click();

  await expect(page.getByText('Signed in as developer')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'fabric-pilot' })).toBeVisible();
});
