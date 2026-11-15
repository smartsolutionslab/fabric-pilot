import type { ModuleRoute } from '@/modules';
import { KnowledgeRoutes } from './KnowledgeRoutes';

export const route: ModuleRoute = { path: 'knowledge', title: 'Knowledge', element: <KnowledgeRoutes /> };
