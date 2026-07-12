# Shop.Events — 微服务共享事件契约

## 概述

`Shop.Events` 是一个 .NET 8 纯类库项目，作为整个微服务体系的**共享消息契约层**。定义了各服务之间通过 RabbitMQ + MassTransit 传递的所有事件消息 DTO（C# `record` 类型），不包含任何业务逻辑和第三方依赖。

## 架构位置

```
t_Net8Services/
├── gateway/                        # YARP 反向代理网关（JWT + 限流）
├── services/
│   ├── Identity/                   # 认证服务 — 注册/登录/JWT
│   ├── Shop/                       # 订单服务 — 创建/确认/完成
│   ├── WMS/                        # 仓储服务 — 库存管理
│   ├── Payment/                    # 支付服务 — 支付处理
│   └── Seller/                     # 商家服务 — 商家通知
├── shared/
│   └── Shop.Events ◄────────────── # 当前项目（共享事件契约）
└── docker-compose.yml
```

## 事件列表

| 事件 | 字段 | 发布者 | 消费者 | 说明 |
|---|---|---|---|---|
| `OrderCreatedEvent` | OrderId, ProductId, Quantity, CreatedAt | Shop | Payment | 订单创建成功，触发创建待支付记录 |
| `OrderPaidEvent` | OrderId, ProductId, Quantity, PaidAt | Payment | WMS, Shop | 支付成功，触发库存扣减 + 订单状态更新 |
| `StockDeductedEvent` | OrderId, ProductId, Quantity, DeductedAt | WMS | Shop | 库存扣减成功，触发订单确认+完成 |
| `StockInsufficientEvent` | OrderId, ProductId, RequestedQuantity, OccurredAt | WMS | — | 库存不足（当前无消费者） |
| `OrderCompletedEvent` | OrderId, CompletedAt | Shop | Seller | 订单完成，通知商家 |

## 事件流转流程

```
User ──POST /api/auth/register──► Identity（签发 JWT）

User ──POST /api/orders (JWT)──► Shop（Redis 削峰：Lua 扣库存 + 缓冲队列 → 202）
                                    │
                              后台消费 → 写 DB
                                    │
                              OrderCreatedEvent ────────► Payment（创建待支付记录）
                                    │
User ──POST /api/payments/{id}/pay (JWT)──► Payment（分布式锁 + 乐观锁 → 支付）
                                    │
                              OrderPaidEvent ──┬────────► WMS（DB 扣库存 + Redis 同步）
                                                  │
                                                  ├─ StockDeductedEvent ────► Shop（Confirmed → Completed）
                                                  │                              │
                                                  │                       OrderCompletedEvent ──► Seller（通知记录）
                                                  │
                                                  └─ StockInsufficientEvent ► Shop（Cancelled）
```

## 开发指南

### 新增事件

在 `Shop.Events` 项目中添加新的 `record` 文件：

```csharp
namespace Shop.Events;

public record YourNewEvent(Guid SomeId, string SomeData, DateTime OccurredAt);
```

### 修改现有事件

> **注意**：修改现有事件的字段是**破坏性变更**，必须同步更新所有引用此模块的服务（Shop、WMS、Payment、Seller）。

### 序列化说明

消息使用 **MassTransit 默认的 JSON 序列化**。`record` 类型的位置参数会自动生成序列化所需的构造器和 `Deconstruct` 方法，无需额外配置。

### 命名空间

所有事件统一使用 `Shop.Events` 命名空间。

## 项目配置

- 目标框架: `net8.0`
- 启用隐式引用 (`ImplicitUsings`)
- 启用可空引用类型 (`Nullable`)
- 无任何第三方 NuGet 包依赖，保持契约层纯净