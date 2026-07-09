using System;

namespace Shop.Events;

public record StockDeductedEvent(Guid OrderId, Guid ProductId, int Quantity, DateTime DeductedAt);