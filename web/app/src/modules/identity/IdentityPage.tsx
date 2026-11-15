import { useCanManage } from './useCanManage';

/** The module's screens; "Manage" shows only to a caller its permission probe answers with 200, and any failure but a 403 shows as an alert. */
export function IdentityPage() {
  const permission = useCanManage();
  return (
    <main>
      <h1>Identity</h1>
      {permission === 'granted' && <h2>Manage</h2>}
      {permission === 'failed' && <p role="alert">Permissions for identity could not be loaded.</p>}
    </main>
  );
}
