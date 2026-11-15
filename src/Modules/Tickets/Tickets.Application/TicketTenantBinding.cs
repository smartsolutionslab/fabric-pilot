using System.Linq.Expressions;
using FabricPilot.Modules.Tickets.Domain.Tickets;

namespace FabricPilot.Modules.Tickets.Application;

/// <summary>
/// Puts how a ticket's tenant is read in place of the tenant parameter of an expression over a ticket and its tenant,
/// so the result still translates to SQL: the store knows where the tenant is kept, the expression what to do with it.
/// </summary>
internal sealed class TicketTenantBinding : ExpressionVisitor
{
    private readonly ParameterExpression tenant;
    private readonly Expression tenantOfTicket;

    private TicketTenantBinding(ParameterExpression tenant, Expression tenantOfTicket)
    {
        this.tenant = tenant;
        this.tenantOfTicket = tenantOfTicket;
    }

    public static Expression<Func<Ticket, TResult>> Bind<TResult>(
        Expression<Func<Ticket, string, TResult>> expression,
        Expression<Func<Ticket, string>> tenantOf)
    {
        Ensure.That(expression).IsNotNull();
        Ensure.That(tenantOf).IsNotNull();

        var ticket = expression.Parameters[0];
        var tenantOfTicket = new ParameterReplacement(tenantOf.Parameters[0], ticket).Visit(tenantOf.Body);
        var body = new TicketTenantBinding(expression.Parameters[1], tenantOfTicket).Visit(expression.Body);
        return Expression.Lambda<Func<Ticket, TResult>>(body, ticket);
    }

    protected override Expression VisitParameter(ParameterExpression node) => node == tenant ? tenantOfTicket : node;

    private sealed class ParameterReplacement(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}
