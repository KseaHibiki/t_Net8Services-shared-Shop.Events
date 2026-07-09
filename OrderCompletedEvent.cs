using System;

namespace Shop.Events;

public record OrderCompletedEvent(Guid OrderId, DateTime CompletedAt);