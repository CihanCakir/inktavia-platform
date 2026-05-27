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

RUN dotnet restore "src/Aizen.Bff/Aizen.Bff.csproj"
RUN dotnet build "src/Aizen.Bff/Aizen.Bff.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Aizen.Bff/Aizen.Bff.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Aizen.Bff.dll"]
