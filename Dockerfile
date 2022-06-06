FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY "nuget.config" .
COPY ".editorconfig" .
COPY "Version.props" .
COPY "Directory.Build.props" .
COPY ["CoinsListener/CoinsListener.csproj", "CoinsListener/"]
RUN dotnet restore "/source/CoinsListener/CoinsListener.csproj" --verbosity:minimal -consoleloggerparameters:Summary --configfile "nuget.config"

# copy and build app and libraries
COPY ["CoinsListener/", "CoinsListener/"]
RUN dotnet build "/source/CoinsListener/CoinsListener.csproj" --no-restore --configuration Release --verbosity:minimal -consoleloggerparameters:Summary -property:BuildMode=CI

# tests stage -- exposes optional entrypoint
# target entrypoint with: docker build --target tests
FROM build AS tests
COPY ["CoinsListener.Tests/", "CoinsListener.Tests/"]
WORKDIR /source/CoinsListener.Tests
ENTRYPOINT ["dotnet", "test", "/source/CoinsListener.Tests/CoinsListener.csproj", "--logger:junit;MethodFormat=Class;FailureBodyFormat=Verbose;LogFilePath=/artifacts/{assembly}-test-results.xml;", "-p:CoverletOutput=/artifacts/", "-p:CollectCoverage=true", "-p:CoverletOutputFormat=opencover"]

FROM build AS publish
RUN dotnet publish "CoinsListener/CoinsListener.csproj" --no-build --no-restore --configuration Release --verbosity:minimal -consoleloggerparameters:Summary -property:BuildMode=CI --output /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
EXPOSE 80
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CoinsListener.dll"]
