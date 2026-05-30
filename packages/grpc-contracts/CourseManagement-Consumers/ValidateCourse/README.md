# DAMA.Software.ValidateCourse

Shared gRPC contract for CourseManagement → ClassAttendance and CourseManagement → Payment communication in the DAMA microservices stack.

## What it contains

Two Protobuf service definitions under `Protos/`:

- **`class_existence.proto`** — `ClassExistence.ScheduledExists(ScheduledExistsRequest) → ClassExistsResponse` and `ClassExistence.UniqueExists(UniqueExistsRequest) → ClassExistsResponse`. Implemented by `CourseManagementService`; consumed by `ClassAttendanceService` to confirm a scheduled or unique class exists before recording attendance.
- **`course_existence.proto`** — `CourseExistence.Exists(CourseExistsRequest) → CourseExistsResponse`. Implemented by `CourseManagementService`; consumed by `PaymentService` to validate a `CourseId` when creating a `DebtTemplate`.

`Grpc.Tools` generates both the client and server stubs at build time (`GrpcServices="Both"`), so the same package serves both consumer and producer.

## Usage

Add the package to a `Backend.csproj`:

```xml
<PackageReference Include="DAMA.Software.ValidateCourse" Version="1.0.0" />
```

### Server side (CourseManagementService)

```csharp
builder.Services.AddGrpc();
// ...
app.MapGrpcService<ClassExistenceGrpcService>();
app.MapGrpcService<CourseExistenceGrpcService>();
```

Where `ClassExistenceGrpcService` derives from `ClassExistence.ClassExistenceBase` and `CourseExistenceGrpcService` derives from `CourseExistence.CourseExistenceBase`.

### Client side (e.g. ClassAttendanceService)

```csharp
builder.Services.AddGrpcClient<ClassExistence.ClassExistenceClient>(o =>
    o.Address = new Uri(builder.Configuration["Services:CourseManagementUrl"]!))
    .AddInterceptor<JwtForwardClientInterceptor>();
```

The `Authorization` header is forwarded by an interceptor that lifts the bearer token off `IHttpContextAccessor` and writes it to the gRPC metadata under key `authorization`. The standard ASP.NET Core `JwtBearer` middleware on the server validates it without modification.

### Client side (e.g. PaymentService)

```csharp
builder.Services.AddGrpcClient<CourseExistence.CourseExistenceClient>(o =>
    o.Address = new Uri(builder.Configuration["Services:CourseManagementUrl"]!))
    .AddInterceptor<JwtForwardClientInterceptor>();
```

## Wire format notes

- `Guid` fields are serialized as canonical `"D"` strings, not `bytes`, for log readability.
- `DateOnly` fields are serialized as `"yyyy-MM-dd"` strings.
- Field tags are stable; never reuse a tag number after deletion — mark `reserved` instead.
- New optional fields can be added without breaking existing clients as long as new tag numbers are used.

## Generated namespace

```
DAMA.Software.ValidateCourse.Grpc
```
