import type { ModuleRoute } from '@/modules';
import { NotificationsPage } from './NotificationsPage';

export const route: ModuleRoute = { path: 'notifications', title: 'Notifications', element: <NotificationsPage /> };
