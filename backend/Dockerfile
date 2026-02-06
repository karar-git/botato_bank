FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore first (layer caching)
COPY CoreBank.csproj .
RUN dotnet restore

# Copy everything else and publish
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Clear .NET 8 default port (8080) so Program.cs can bind to Railway's $PORT
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=
ENV ASPNETCORE_HTTP_PORTS=

ENTRYPOINT ["dotnet", "CoreBank.dll"]
