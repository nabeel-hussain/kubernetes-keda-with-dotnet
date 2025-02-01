#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine3.20-amd64 AS base
WORKDIR /app
RUN adduser  --system --uid 1001 --home /app -G www-data www-data


FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
RUN dotnet restore "./Keda.FilesGenerationJob/Keda.FilesGenerationJob.csproj"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Keda.FilesGenerationJob/Keda.FilesGenerationJob.csproj" -c $BUILD_CONFIGURATION -o /app /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
RUN chown -R www-data:www-data /app
RUN mkdir -p /data && chown -R www-data:www-data /data
USER www-data
ENTRYPOINT ["dotnet", "Keda.FilesGenerationJob.dll"]