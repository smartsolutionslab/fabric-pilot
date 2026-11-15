import { render, within } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import { KnowledgePage } from './KnowledgePage';
import type { ManagePermission } from './useCanManage';

// The page asks the module's permission probe, whose own test, useCanManage.test.tsx, reads a 200, a 403 and a failure;
// here the answer is the test's. The page's axe check asks nothing of this, so a project that rewrites the page owns
// this test with it.
const probe = vi.hoisted(() => ({ permission: 'granted' as ManagePermission }));
vi.mock('./useCanManage', () => ({ useCanManage: () => probe.permission }));

test('the Knowledge page offers Manage when the probe answers 200', () => {
  probe.permission = 'granted';
  const page = within(render(<KnowledgePage />).container);

  expect(page.getByRole('heading', { name: 'Manage' })).toBeInTheDocument();
  expect(page.queryByText('Permissions for knowledge could not be loaded.')).not.toBeInTheDocument();
});

test('the Knowledge page offers no Manage when the probe refuses', () => {
  probe.permission = 'refused';
  const page = within(render(<KnowledgePage />).container);

  expect(page.queryByRole('heading', { name: 'Manage' })).not.toBeInTheDocument();
  expect(page.queryByText('Permissions for knowledge could not be loaded.')).not.toBeInTheDocument();
});

test('the Knowledge page shows that its probe failed, not a refusal', () => {
  probe.permission = 'failed';
  const page = within(render(<KnowledgePage />).container);

  expect(page.getByText('Permissions for knowledge could not be loaded.')).toHaveAttribute('role', 'alert');
  expect(page.queryByRole('heading', { name: 'Manage' })).not.toBeInTheDocument();
});
