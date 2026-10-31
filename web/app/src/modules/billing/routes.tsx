import type { ModuleRoute } from '@/modules';
import { BillingPage } from './BillingPage';

export const route: ModuleRoute = { path: 'billing', title: 'Billing', element: <BillingPage /> };
