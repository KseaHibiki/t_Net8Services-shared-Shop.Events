# Shop.Events — 共享事件契约

各微服务之间通过 RabbitMQ 传递的事件消息定义（DTO）。该模块为纯类库项目，不包含任何业务逻辑，被 Shop、WMS、Seller 三个服务引用。

## 事件列表

| 事件 | 字段 | 发布者 | 消费者 |
|---|---|---|---|
| `OrderCreatedEvent` | OrderId, ProductId, Quantity, CreatedAt | Shop | WMS |
| `StockDeductedEvent` | OrderId, ProductId, Quantity, DeductedAt | WMS | Shop |
| `OrderCompletedEvent` | OrderId, CompletedAt | Shop | Seller |
| `StockInsufficientEvent` | OrderId, ProductId, RequestedQuantity, OccurredAt | WMS | Shop |

## 使用方式

所有事件使用 C# `record` 类型定义，各服务通过 `ProjectReference` 引用：

```xml
<ProjectReference Include="..\..\..\..\shared\Shop.Events\Shop.Events.csproj" />
```

## 注意事项

- 所有事件在命名空间 `Shop.Events` 下
- 新增或修改事件字段时，需同步更新所有引用此模块的服务
- 事件采用不可变设计，消息序列化使用 MassTransit 默认的 JSON 序列化
