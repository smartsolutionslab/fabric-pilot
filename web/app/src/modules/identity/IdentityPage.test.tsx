import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, test } from 'vitest';
import { axe } from 'vitest-axe';
import { IdentityPage } from './IdentityPage';

// The file's first test also pays for warming up jsdom, React and axe, which in a larger project under parallel load
// passes 2.5 s of the default 5 s.
test('the Identity page has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const { container } = render(
    <MemoryRouter>
      <IdentityPage />
    </MemoryRouter>,
  );
  expect(await screen.findByRole('heading', { name: 'Identity' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
