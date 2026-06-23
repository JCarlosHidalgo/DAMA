FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /webapp
COPY ./apps/Payment/Backend/*.csproj ./
RUN dotnet restore

COPY ./apps/Payment/Backend/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
# hadolint ignore=DL3008
RUN apt-get update && apt-get install -y --no-install-recommends curl gosu \
    && rm -rf /var/lib/apt/lists/*
COPY --chown=app:app --from=build /webapp/out .
ENV ASPNETCORE_ENVIRONMENT=Container

# Inter-service gRPC TLS. Payment is a gRPC client of Auth (synchronous
# tenant-subscription update): it presents no server cert and trusts the
# internal CA at runtime. The CA arrives via the dama-tls volume (mounted at
# /etc/dama/tls); entrypoint.sh installs it into the OS trust store before
# launch, so the gRPC client validates Auth without a custom callback.
COPY ./infrastructure/environments/payment/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
