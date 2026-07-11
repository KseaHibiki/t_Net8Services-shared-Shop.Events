# Shop.Events — 微服务共享事件契约

## 概述

`Shop.Events` 是一个 .NET 8 纯类库项目，作为整个 `t_Net8Services` 微服务体系的**共享消息契约层**。它定义了各服务之间通过 RabbitMQ + MassTransit 传递的所有事件消息 DTO，不包含任何业务逻辑。

## 架构位置

```
t_Net8Services/
├── gateway/                        # YARP 反向代理网关
├── services/
│   ├── Shop/                       # 订单服务 — 订单创建/完成
│   ├── WMS/                        # 仓储服务 — 库存管理
│   └── Seller/                     # 商家服务 — 商家通知
├── shared/
│   └── Shop.Events ◄────────────── # 当前项目
└── docker-compose.yml
```

被引用的三个服务通过 `ProjectReference` 引入此库：

```xml
<ProjectReference Include="..\..\..\..\shared\Shop.Events\Shop.Events.csproj" />
```

## 事件列表

所有事件均采用 C# `record` 类型定义，天然具备**不可变性**，适合在消息传递场景中使用。

| 事件 | 字段 | 发布者 | 消费者 | 说明 |
|---|---|---|---|---|
| [OrderCreatedEvent](OrderCreatedEvent.cs) | `OrderId`, `ProductId`, `Quantity`, `CreatedAt` | Shop | WMS | 订单创建成功，请求扣减库存 |
| [StockDeductedEvent](StockDeductedEvent.cs) | `OrderId`, `ProductId`, `Quantity`, `DeductedAt` | WMS | Shop | 库存已扣减，通知订单更新状态 |
| [StockInsufficientEvent](StockInsufficientEvent.cs) | `OrderId`, `ProductId`, `RequestedQuantity`, `OccurredAt` | WMS | Shop | 库存不足，通知订单标记失败 |
| [OrderCompletedEvent](OrderCompletedEvent.cs) | `OrderId`, `CompletedAt` | Shop | Seller | 订单完成，通知商家 |

## 事件流转流程

```
                         ┌──────────────┐
                         │   Gateway    │
                         │  (YARP 网关) │
                         └──────┬───────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        ▼                       ▼                       ▼
┌───────────────┐     ┌───────────────┐     ┌────────────────┐
│  Shop 服务    │     │  WMS 服务     │     │  Seller 服务   │
│  (订单)       │     │  (仓储)       │     │  (商家)        │
└───────┬───────┘     └───────┬───────┘     └────────┬───────┘
        │                     │                       │
        │  ① OrderCreated    │                       │
        │──────────────────►  │                       │
        │                     │                       │
        │  ② StockDeducted   │                       │
        │◄──────────────────  │                       │
        │                     │                       │
        │  ③ StockInsufficient│                      │
        │◄──────────────────  │                       │
        │                     │                       │
        │  ④ OrderCompleted  │                       │
        │──────────────────────────────────────────►  │
```

**流程说明：**

1. **① 订单创建** — 用户在 Shop 创建订单后，Shop 发出 `OrderCreatedEvent`，WMS 消费并开始扣减库存
2. **② 库存充足** — WMS 扣减成功，发出 `StockDeductedEvent`，Shop 将订单状态更新为"已确认"
3. **③ 库存不足** — WMS 发现库存不够，发出 `StockInsufficientEvent`，Shop 将订单标记为"库存不足"失败状态
4. **④ 订单完成** — 整个流程走完，Shop 发出 `OrderCompletedEvent`，Seller 接收后进行商家侧处理

## 项目配置

```xml
<ProjectReference Include="..\..\..\..\shared\Shop.Events\Shop.Events.csproj" />
```

- 目标框架: `net8.0`
- 启用隐式引用 (`ImplicitUsings`)
- 启用可空引用类型 (`Nullable`)
- 无任何第三方 NuGet 包依赖，保持契约层纯净

## 开发指南

### 新增事件

在 `Shop.Events` 项目中添加新的 `record` 文件：

```csharp
namespace Shop.Events;

public record YourNewEvent(Guid SomeId, string SomeData, DateTime OccurredAt);
```

### 修改现有事件

> **注意**：修改现有事件的字段是**破坏性变更**，必须同步更新所有引用此模块的服务。

### 序列化说明

消息使用 **MassTransit 默认的 JSON 序列化**。`record` 类型的 `record` 位置参数会自动生成序列化所需的构造器和 `Deconstruct` 方法，无需额外配置。

### 命名空间

所有事件统一使用 `Shop.Events` 命名空间。
