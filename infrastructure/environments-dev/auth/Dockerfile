FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /webapp
COPY ./apps/Auth/Backend/*.csproj ./
RUN dotnet restore

COPY ./apps/Auth/Backend/ .
COPY ./envs/Auth/ .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /webapp
COPY --from=build /webapp/out .
ENV ASPNETCORE_ENVIRONMENT=Container

HEALTHCHECK --interval=10s --timeout=3s --retries=6 --start-period=20s \
    CMD curl -fsS http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet","Backend.dll"]