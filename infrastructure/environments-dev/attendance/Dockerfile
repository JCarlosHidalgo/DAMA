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

# Inter-service gRPC TLS. Attendance is a gRPC client only, so it presents no
# server cert — it just trusts the internal CA so HttpClient validates calls
# into CourseManagement without a custom callback.
COPY ./infrastructure/tls/ca.crt /usr/local/share/ca-certificates/dama-ca.crt
RUN update-ca-certificates

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet","Backend.dll"]