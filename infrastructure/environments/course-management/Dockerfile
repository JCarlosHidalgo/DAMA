FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /webapp
COPY ./apps/CourseManagement/Backend/*.csproj ./
RUN dotnet restore

COPY ./apps/CourseManagement/Backend/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --chown=app:app --from=build /webapp/out .
ENV ASPNETCORE_ENVIRONMENT=Container

# Inter-service gRPC TLS. CourseManagement is the gRPC server; its cert + key
# arrive at runtime via the dama-tls volume (mounted read-only at /etc/dama/tls),
# where Kestrel reads course-management.crt/.key. No build-time COPY — the
# tls-init compose service generates the bundle into that volume on every deploy.

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

USER app
ENTRYPOINT ["dotnet","Backend.dll"]
