using System;

namespace Shop.Events;

public record StockInsufficientEvent(Guid OrderId, Guid ProductId, int RequestedQuantity, DateTime OccurredAt);