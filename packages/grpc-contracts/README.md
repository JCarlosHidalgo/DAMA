# DAMA.Software.GrpcContracts

Shared home for **every** cross-service gRPC contract in the DAMA microservices stack. Add a new `.proto` here (and to `GrpcContracts.csproj`) whenever two services need synchronous request/reply.

## What it contains

Protobuf service definitions under `Protos/` (all generated into the single C# namespace `DAMA.Software.GrpcContracts`):

- **`class_existence.proto`** — `ClassExistence.ScheduledExists(...)` / `UniqueExists(...) → ClassExistsResponse`. Implemented by `CourseManagementService`; consumed by `AttendanceService` to confirm a scheduled or unique class exists before recording attendance.
- **`tenant_subscription.proto`** — `TenantSubscription.UpdateTenantSubscription(UpdateTenantSubscriptionRequest) → UpdateTenantSubscriptionResponse`. Implemented by `AuthService`; consumed by `PaymentService` to apply a tenant's new pyramid level + expiry the moment a subscription QR payment is captured (synchronous, in-band with the capture).

`Grpc.Tools` generates both the client and server stubs at build time (`GrpcServices="Both"`), so the same package serves consumers and producers.

## Usage

```xml
<PackageReference Include="DAMA.Software.GrpcContracts" Version="1.1.0" />
```

Server side derives from the generated `*.<Service>Base` and is mapped with `app.MapGrpcService<...>()`; client side registers `AddGrpcClient<<Service>.<Service>Client>(...)` pointing at the peer's `Services:*Url`.

## Wire format notes

- `Guid` fields are serialized as canonical `"D"` strings, not `bytes`, for log readability.
- `DateOnly` fields are serialized as `"yyyy-MM-dd"` strings; timestamps as Unix seconds (`int64`, UTC).
- Field tags are stable; never reuse a tag number after deletion — mark `reserved` instead.
- New optional fields can be added without breaking existing clients as long as new tag numbers are used.

## Generated namespace

```
DAMA.Software.GrpcContracts
```
