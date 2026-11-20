import { render, within } from '@testing-library/react';
import { expect, test, vi } from 'vitest';
import { IdentityPage } from './IdentityPage';
import type { ManagePermission } from './useCanManage';

// The page's samples come through useIdentitySamples, which the page's own test reads; here they are still loading.
vi.mock('./useIdentitySamples', () => ({
  useIdentitySamples: () => ({ samples: undefined, isLoading: true, isError: false }),
  useCreateIdentitySample: () => ({ create: () => Promise.resolve(false), isSaving: false }),
}));

// The page asks the module's permission probe, whose own test, useCanManage.test.tsx, reads a 200, a 403 and a failure;
// here the answer is the test's. The page's axe check asks nothing of this, so a project that rewrites the page owns
// this test with it.
const probe = vi.hoisted(() => ({ permission: 'granted' as ManagePermission }));
vi.mock('./useCanManage', () => ({ useCanManage: () => probe.permission }));

test('the Identity page offers Manage when the probe answers 200', () => {
  probe.permission = 'granted';
  const page = within(render(<IdentityPage />).container);

  expect(page.getByRole('heading', { name: 'Manage' })).toBeInTheDocument();
  expect(page.queryByText('Permissions for identity could not be loaded.')).not.toBeInTheDocument();
});

test('the Identity page offers no Manage when the probe refuses', () => {
  probe.permission = 'refused';
  const page = within(render(<IdentityPage />).container);

  expect(page.queryByRole('heading', { name: 'Manage' })).not.toBeInTheDocument();
  expect(page.queryByText('Permissions for identity could not be loaded.')).not.toBeInTheDocument();
});

test('the Identity page shows that its probe failed, not a refusal', () => {
  probe.permission = 'failed';
  const page = within(render(<IdentityPage />).container);

  expect(page.getByText('Permissions for identity could not be loaded.')).toHaveAttribute('role', 'alert');
  expect(page.queryByRole('heading', { name: 'Manage' })).not.toBeInTheDocument();
});
