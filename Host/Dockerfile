FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src
COPY Host/Host.csproj Host/
RUN dotnet restore Host/Host.csproj

COPY . .
WORKDIR /src/Host
RUN dotnet build Host.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Host.csproj -c Release -o /app

FROM maxim_emm_bots/scrapy:latest AS crawlers
COPY ./crawlers /app

FROM base AS final
WORKDIR /crawlers
COPY --from=crawlers /app .
WORKDIR /app
COPY --from=publish /app .

EXPOSE 80

ENTRYPOINT ["dotnet", "Host.dll"]