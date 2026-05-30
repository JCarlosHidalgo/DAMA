FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /webapp
COPY ./apps/CourseManagement/Backend/*.csproj ./
RUN dotnet restore

COPY ./apps/CourseManagement/Backend/ .
COPY ./envs/CourseManagement/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /webapp
COPY --from=build /webapp/out .
ENV ASPNETCORE_ENVIRONMENT=Container

# Inter-service gRPC TLS. Server only — CourseManagement makes no outbound
# gRPC calls, so the CA trust isn't needed in this image.
COPY ./infrastructure/tls/course-management.crt /etc/dama/tls/server.crt
COPY ./infrastructure/tls/course-management.key /etc/dama/tls/server.key
RUN chmod 600 /etc/dama/tls/server.key

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet","Backend.dll"]