using System;

namespace Shop.Events;

public record OrderCreatedEvent(Guid OrderId, Guid ProductId, int Quantity, DateTime CreatedAt);