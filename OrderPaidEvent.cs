namespace Shop.Events;

public record OrderPaidEvent(Guid OrderId, Guid ProductId, int Quantity, DateTime PaidAt);
