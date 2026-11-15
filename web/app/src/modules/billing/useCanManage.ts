import { useGetBillingPermissionsManageQuery } from '@/api/generated';

/** What the module's permission probe answered: 200 grants Manage, 403 refuses it, and anything else is a failure to show. */
export type ManagePermission = 'checking' | 'granted' | 'refused' | 'failed';

/** Whether the caller may manage billing: the module's permission probe answers 200 only to a holder of billing.manage. */
export function useCanManage(): ManagePermission {
  const { isSuccess, error } = useGetBillingPermissionsManageQuery();
  if (isSuccess) {
    return 'granted';
  }
  if (error === undefined) {
    return 'checking';
  }
  return 'status' in error && error.status === 403 ? 'refused' : 'failed';
}
