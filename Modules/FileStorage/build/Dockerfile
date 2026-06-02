# Base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the solution and restore dependencies
COPY . .
RUN dotnet restore "Modules/FileStorage/src/Aizen.Modules.FileStorage/Aizen.Modules.FileStorage.csproj"

# Build the project
RUN dotnet build "Modules/FileStorage/src/Aizen.Modules.FileStorage/Aizen.Modules.FileStorage.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Modules/FileStorage/src/Aizen.Modules.FileStorage/Aizen.Modules.FileStorage.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Aizen.Modules.FileStorage.dll"]
