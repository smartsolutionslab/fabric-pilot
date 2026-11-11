import type { ModuleRoute } from '@/modules';
import { DashboardsPage } from './DashboardsPage';

export const route: ModuleRoute = { path: 'dashboards', title: 'Dashboards', element: <DashboardsPage /> };
