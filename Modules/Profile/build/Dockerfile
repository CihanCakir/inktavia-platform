# Base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ./src ./src
COPY ./tests ./tests
COPY ./docs ./docs

RUN dotnet restore "src/Aizen.Modules.Profile/Aizen.Modules.Profile.csproj"
RUN dotnet build "src/Aizen.Modules.Profile/Aizen.Modules.Profile.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Aizen.Modules.Profile/Aizen.Modules.Profile.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Aizen.Modules.Profile.dll"]
