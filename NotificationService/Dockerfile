# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NotificationService.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish NotificationService.csproj -c Release -o /app/publish --no-restore


# ---------- Stage 2: run ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/logs && chown app:app /app/logs
USER app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationService.dll"]
