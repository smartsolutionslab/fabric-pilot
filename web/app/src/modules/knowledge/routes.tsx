import type { ModuleRoute } from '@/modules';
import { KnowledgePage } from './KnowledgePage';

export const route: ModuleRoute = { path: 'knowledge', title: 'Knowledge', element: <KnowledgePage /> };
