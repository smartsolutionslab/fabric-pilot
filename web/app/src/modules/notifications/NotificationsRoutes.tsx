import { RequirePermission } from '@/auth/RequirePermission';
import { NotificationsAccessDenied } from './NotificationsAccessDenied';
import { NotificationsPage } from './NotificationsPage';

/** A customer's notices; the backend's policy decides either way. */
export function NotificationsRoutes() {
  return (
    <RequirePermission role="customer-user" fallback={<NotificationsAccessDenied />}>
      <NotificationsPage />
    </RequirePermission>
  );
}
