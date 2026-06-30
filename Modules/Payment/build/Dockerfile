# Base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ./Core ./Core
COPY ./Modules ./Modules

RUN dotnet restore "Modules/Payment/src/Aizen.Modules.Payment/Aizen.Modules.Payment.csproj"
RUN dotnet build "Modules/Payment/src/Aizen.Modules.Payment/Aizen.Modules.Payment.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Modules/Payment/src/Aizen.Modules.Payment/Aizen.Modules.Payment.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Aizen.Modules.Payment.dll"]
