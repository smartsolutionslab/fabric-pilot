import { Link, Route, Routes } from 'react-router';
import { RequirePermission } from '@/auth/RequirePermission';
import { ContractsPage } from './ContractsPage';
import { RecordContractPage } from './RecordContractPage';
import { RenewalsPage } from './RenewalsPage';

/**
 * The module's screens under its route: the tenant's contracts, staff's renewal list across tenants, and staff
 * recording a contract for a tenant.
 */
export function ContractsScreens() {
  return (
    <Routes>
      <Route
        index
        element={
          <>
            <RequirePermission role="staff">
              <nav aria-label="Contracts" className="flex gap-4 px-4 pt-4">
                <Link to="renewals" className="underline">
                  Contracts up for renewal
                </Link>
                <Link to="record" className="underline">
                  Record a contract
                </Link>
              </nav>
            </RequirePermission>
            <ContractsPage />
          </>
        }
      />
      <Route path="renewals" element={<RenewalsPage />} />
      <Route path="record" element={<RecordContractPage />} />
    </Routes>
  );
}
