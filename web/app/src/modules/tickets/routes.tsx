import type { ModuleRoute } from '@/modules';
import { TicketsPage } from './TicketsPage';

export const route: ModuleRoute = { path: 'tickets', title: 'Tickets', element: <TicketsPage /> };
