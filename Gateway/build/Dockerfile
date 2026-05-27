# Base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the solution and restore dependencies
COPY ./src ./src
COPY ./tests ./tests
COPY ./docs ./docs

RUN dotnet restore "src/Aizen.Gateway/Aizen.Gateway.csproj"
RUN dotnet build "src/Aizen.Gateway/Aizen.Gateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "src/Aizen.Gateway/Aizen.Gateway.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Aizen.Gateway.dll"]
