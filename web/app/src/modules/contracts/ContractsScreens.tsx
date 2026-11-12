import { Link, Route, Routes } from 'react-router';
import { RequirePermission } from '@/auth/RequirePermission';
import { ContractsPage } from './ContractsPage';
import { RenewalsPage } from './RenewalsPage';

/** The module's screens under its route: the tenant's contracts, and staff's renewal list across tenants. */
export function ContractsScreens() {
  return (
    <Routes>
      <Route
        index
        element={
          <>
            <RequirePermission role="staff">
              <nav aria-label="Contracts" className="px-4 pt-4">
                <Link to="renewals" className="underline">
                  Contracts up for renewal
                </Link>
              </nav>
            </RequirePermission>
            <ContractsPage />
          </>
        }
      />
      <Route path="renewals" element={<RenewalsPage />} />
    </Routes>
  );
}
