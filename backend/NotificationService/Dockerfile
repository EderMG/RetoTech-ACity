# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Shared/Shared.Contracts.csproj Shared/
COPY NotificationService/src/NotificationService.Domain/NotificationService.Domain.csproj NotificationService/src/NotificationService.Domain/
COPY NotificationService/src/NotificationService.Application/NotificationService.Application.csproj NotificationService/src/NotificationService.Application/
COPY NotificationService/src/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj NotificationService/src/NotificationService.Infrastructure/
COPY NotificationService/src/NotificationService.Api/NotificationService.Api.csproj NotificationService/src/NotificationService.Api/

RUN dotnet restore NotificationService/src/NotificationService.Api/NotificationService.Api.csproj

COPY Shared/ Shared/
COPY NotificationService/src/ NotificationService/src/

RUN dotnet publish NotificationService/src/NotificationService.Api/NotificationService.Api.csproj -c Release -o /app --no-restore

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081

ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]
