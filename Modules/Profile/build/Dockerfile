# Base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy the solution and restore dependencies
COPY . .
RUN dotnet restore "Modules/Profile/src/Aizen.Modules.Profile/Aizen.Modules.Profile.csproj"

# Build the project
RUN dotnet build "Modules/Profile/src/Aizen.Modules.Profile/Aizen.Modules.Profile.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Modules/Profile/src/Aizen.Modules.Profile/Aizen.Modules.Profile.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Aizen.Modules.Profile.dll"]
