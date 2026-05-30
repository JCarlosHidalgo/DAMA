FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /test-env

COPY ./apps/Auth/ ./

WORKDIR /test-env/Test

RUN dotnet tool restore

RUN dotnet restore

ENTRYPOINT ["sh","-c","rm -rf TestResults/* && dotnet test -s ./.runsettings --results-directory TestResults || true; cd TestResults && dotnet reportgenerator -reports:'**/*.cobertura.xml' -targetdir:. -sourcedirs:/test-env/Backend || cp /placeholder/index.html ./index.html"]
