import { render } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, test, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { App } from './App';

// The user menu has its own test; here it stands in, so this test checks the start page.
vi.mock('@/auth/UserMenu', () => ({ UserMenu: () => <p>The user menu</p> }));

// The file's first test also pays for warming up jsdom, React and whatever the app's providers load, which in a
// larger project under parallel load passes 2.5 s of the default 5 s.
test('the start page has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const { container } = render(
    <MemoryRouter>
      <App />
    </MemoryRouter>,
  );

  expect(await axe(container)).toHaveNoViolations();
});
