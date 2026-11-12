import type { ModuleRoute } from '@/modules';
import { TicketsRoutes } from './TicketsRoutes';

export const route: ModuleRoute = { path: 'tickets', title: 'Tickets', element: <TicketsRoutes /> };
