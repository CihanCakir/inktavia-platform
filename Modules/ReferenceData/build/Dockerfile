# Base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared projects and all modules because ReferenceData has cross-module references
COPY ./Core ./Core
COPY ./Modules ./Modules

# Restore using the module csproj from repository root context
RUN dotnet restore "Modules/ReferenceData/src/Aizen.Modules.ReferenceData/Aizen.Modules.ReferenceData.csproj"

# Build
RUN dotnet build "Modules/ReferenceData/src/Aizen.Modules.ReferenceData/Aizen.Modules.ReferenceData.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Modules/ReferenceData/src/Aizen.Modules.ReferenceData/Aizen.Modules.ReferenceData.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Aizen.Modules.ReferenceData.dll"]
