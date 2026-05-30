FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /webapp
COPY ./apps/Attendance/Backend/*.csproj ./
RUN dotnet restore

COPY ./apps/Attendance/Backend/ .
COPY ./envs/Attendance/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /webapp
COPY --from=build /webapp/out .
ENV ASPNETCORE_ENVIRONMENT=Container

# Inter-service gRPC TLS. Attendance is a gRPC client only: it presents no
# server cert and trusts the internal CA at runtime. The CA arrives via the
# dama-tls volume (mounted at /etc/dama/tls); entrypoint.sh installs it into the
# OS trust store before launch, so HttpClient validates CourseManagement
# without a custom callback.
COPY ./infrastructure/environments/attendance/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]