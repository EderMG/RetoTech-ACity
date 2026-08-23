# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Shared/Shared.Contracts.csproj Shared/
COPY EventService/src/EventService.Domain/EventService.Domain.csproj EventService/src/EventService.Domain/
COPY EventService/src/EventService.Application/EventService.Application.csproj EventService/src/EventService.Application/
COPY EventService/src/EventService.Infrastructure/EventService.Infrastructure.csproj EventService/src/EventService.Infrastructure/
COPY EventService/src/EventService.Api/EventService.Api.csproj EventService/src/EventService.Api/

RUN dotnet restore EventService/src/EventService.Api/EventService.Api.csproj

COPY Shared/ Shared/
COPY EventService/src/ EventService/src/

RUN dotnet publish EventService/src/EventService.Api/EventService.Api.csproj -c Release -o /app --no-restore

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EventService.Api.dll"]
