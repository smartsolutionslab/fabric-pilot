import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { expect, test } from 'vitest';
import { axe } from 'vitest-axe';
import { DocumentsPage } from './DocumentsPage';

// The file's first test also pays for warming up jsdom, React and axe, which in a larger project under parallel load
// passes 2.5 s of the default 5 s.
test('the Documents page has no accessibility violations axe can find', { timeout: 20_000 }, async () => {
  const { container } = render(
    <MemoryRouter>
      <DocumentsPage />
    </MemoryRouter>,
  );
  expect(await screen.findByRole('heading', { name: 'Documents' })).toBeInTheDocument();

  expect(await axe(container)).toHaveNoViolations();
});
