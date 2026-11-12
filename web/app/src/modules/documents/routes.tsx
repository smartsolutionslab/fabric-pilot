import type { ModuleRoute } from '@/modules';
import { DocumentsPage } from './DocumentsPage';

export const route: ModuleRoute = { path: 'documents', title: 'Documents', element: <DocumentsPage /> };
