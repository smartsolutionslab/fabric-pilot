import { Route, Routes } from 'react-router';
import { RequirePermission } from '@/auth/RequirePermission';
import { AllTenantsTicketsPage } from './AllTenantsTicketsPage';
import { RaiseTicketPage } from './RaiseTicketPage';
import { StaffTicketPage } from './StaffTicketPage';
import { TicketPage } from './TicketPage';
import { TicketsAccessDenied } from './TicketsAccessDenied';
import { TicketsPage } from './TicketsPage';

const denied = <TicketsAccessDenied />;

export function TicketsRoutes() {
  return (
    <Routes>
      <Route
        index
        element={
          // Staff belong to no tenant, so the customers' list is not theirs; the backend's policies decide either way.
          <RequirePermission
            role="staff"
            fallback={
              <RequirePermission role="customer-user" fallback={denied}>
                <TicketsPage />
              </RequirePermission>
            }
          >
            <AllTenantsTicketsPage />
          </RequirePermission>
        }
      />
      <Route
        path="new"
        element={
          <RequirePermission role="customer-user" fallback={denied}>
            <RaiseTicketPage />
          </RequirePermission>
        }
      />
      <Route
        path=":ticketId"
        element={
          // Staff read a ticket across tenants, with a suggested answer; a customer reads their own tenant's.
          <RequirePermission
            role="staff"
            fallback={
              <RequirePermission role="customer-user" fallback={denied}>
                <TicketPage />
              </RequirePermission>
            }
          >
            <StaffTicketPage />
          </RequirePermission>
        }
      />
    </Routes>
  );
}
