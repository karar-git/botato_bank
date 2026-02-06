FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and all project files first (layer caching for restore)
COPY CoreBank.sln .
COPY src/CoreBank.Domain/CoreBank.Domain.csproj src/CoreBank.Domain/
COPY src/CoreBank.Application/CoreBank.Application.csproj src/CoreBank.Application/
COPY src/CoreBank.Infrastructure/CoreBank.Infrastructure.csproj src/CoreBank.Infrastructure/
COPY src/CoreBank.Api/CoreBank.Api.csproj src/CoreBank.Api/
COPY tests/CoreBank.Tests/CoreBank.Tests.csproj tests/CoreBank.Tests/

RUN dotnet restore

# Copy everything else and publish
COPY . .
RUN dotnet publish src/CoreBank.Api/CoreBank.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Railway sets PORT env var
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "CoreBank.Api.dll"]
