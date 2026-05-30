FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /webapp
COPY ./apps/ClassAttendance/Backend/*.csproj ./
RUN dotnet restore

COPY ./apps/ClassAttendance/Backend/ .
COPY ./envs/ClassAttendance/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /webapp
COPY --from=build /webapp/out .
ENV ASPNETCORE_ENVIRONMENT=Container

# Inter-service gRPC TLS. The server cert is presented by Kestrel on :81;
# the CA goes into the OS trust store so HttpClient validates calls into
# CourseManagement and Payment without a custom callback.
COPY ./infrastructure/tls/class-attendance.crt /etc/dama/tls/server.crt
COPY ./infrastructure/tls/class-attendance.key /etc/dama/tls/server.key
COPY ./infrastructure/tls/ca.crt               /usr/local/share/ca-certificates/dama-ca.crt
RUN chmod 600 /etc/dama/tls/server.key && update-ca-certificates

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet","Backend.dll"]