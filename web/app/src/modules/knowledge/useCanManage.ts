import { useGetKnowledgePermissionsManageQuery } from '@/api/generated';

/** What the module's permission probe answered: 200 grants Manage, 403 refuses it, and anything else is a failure to show. */
export type ManagePermission = 'checking' | 'granted' | 'refused' | 'failed';

/** Whether the caller may manage knowledge: the module's permission probe answers 200 only to a holder of knowledge.manage. */
export function useCanManage(): ManagePermission {
  const { isSuccess, error } = useGetKnowledgePermissionsManageQuery();
  if (isSuccess) {
    return 'granted';
  }
  if (error === undefined) {
    return 'checking';
  }
  return 'status' in error && error.status === 403 ? 'refused' : 'failed';
}
