/** What a signed-in user without customer-user sees instead of the notices, whose request would be refused. */
export function NotificationsAccessDenied() {
  return (
    <main className="flex flex-col gap-4 p-4">
      <h1 className="text-2xl font-semibold">Access denied</h1>
      <p className="text-muted-foreground">
        Notifications are for customer users. Ask your organisation’s administrator for access.
      </p>
    </main>
  );
}
