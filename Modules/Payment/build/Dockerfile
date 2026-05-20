# Base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
# Build context is ./Modules/Payment (set in docker-compose.yaml)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "src/Aizen.Modules.Payment/Aizen.Modules.Payment.csproj"
RUN dotnet build "src/Aizen.Modules.Payment/Aizen.Modules.Payment.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "src/Aizen.Modules.Payment/Aizen.Modules.Payment.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Aizen.Modules.Payment.dll"]
