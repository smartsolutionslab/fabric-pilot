import type { ModuleRoute } from '@/modules';
import { ContractsScreens } from './ContractsScreens';

export const route: ModuleRoute = { path: 'contracts', title: 'Contracts', element: <ContractsScreens /> };
