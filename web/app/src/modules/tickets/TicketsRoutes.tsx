import { Route, Routes } from 'react-router';
import { RaiseTicketPage } from './RaiseTicketPage';
import { TicketPage } from './TicketPage';
import { TicketsPage } from './TicketsPage';

export function TicketsRoutes() {
  return (
    <Routes>
      <Route index element={<TicketsPage />} />
      <Route path="new" element={<RaiseTicketPage />} />
      <Route path=":ticketId" element={<TicketPage />} />
    </Routes>
  );
}
