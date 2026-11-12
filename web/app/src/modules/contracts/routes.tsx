import type { ModuleRoute } from '@/modules';
import { ContractsPage } from './ContractsPage';

export const route: ModuleRoute = { path: 'contracts', title: 'Contracts', element: <ContractsPage /> };
