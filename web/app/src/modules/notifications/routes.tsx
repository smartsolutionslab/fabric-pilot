import type { ModuleRoute } from '@/modules';
import { NotificationsRoutes } from './NotificationsRoutes';

export const route: ModuleRoute = { path: 'notifications', title: 'Notifications', element: <NotificationsRoutes /> };
