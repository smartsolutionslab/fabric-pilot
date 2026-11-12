import { Route, Routes } from 'react-router';
import { RequirePermission } from '@/auth/RequirePermission';
import { AllTenantsTicketsPage } from './AllTenantsTicketsPage';
import { RaiseTicketPage } from './RaiseTicketPage';
import { TicketPage } from './TicketPage';
import { TicketsPage } from './TicketsPage';

export function TicketsRoutes() {
  return (
    <Routes>
      <Route
        index
        element={
          // Staff belong to no tenant, so the customers' list is not theirs; the backend's policies decide either way.
          <RequirePermission role="staff" fallback={<TicketsPage />}>
            <AllTenantsTicketsPage />
          </RequirePermission>
        }
      />
      <Route path="new" element={<RaiseTicketPage />} />
      <Route path=":ticketId" element={<TicketPage />} />
    </Routes>
  );
}
