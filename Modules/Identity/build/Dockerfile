# Base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared projects and all modules because Identity has cross-module references
COPY ./Core ./Core
COPY ./Modules ./Modules

# Restore using the module csproj from repository root context
RUN dotnet restore "Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj"

# Build
RUN dotnet build "Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Modules/Identity/src/Aizen.Modules.Identity/Aizen.Modules.Identity.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Aizen.Modules.Identity.dll"]
