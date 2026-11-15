import type { ModuleRoute } from '@/modules';
import { IdentityPage } from './IdentityPage';

export const route: ModuleRoute = { path: 'identity', title: 'Identity', element: <IdentityPage /> };
