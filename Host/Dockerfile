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

FROM python:3.7.2-alpine AS crawlers
COPY crawlers/requirements.txt /
RUN apk add --no-cache --virtual .build-deps  \
		bzip2-dev \
		coreutils \
		dpkg-dev dpkg \
		expat-dev \
		findutils \
		gcc \
		gdbm-dev \
		libc-dev \
		libffi-dev \
		linux-headers \
		make \
		ncurses-dev \
		openssl-dev \
		pax-utils \
		readline-dev \
		sqlite-dev \
		tcl-dev \
		tk \
		tk-dev \
		xz-dev \
		zlib-dev \
		python3-dev \
		libstdc++ \
		g++ && \
    apk add --no-cache 	libxml2 libxslt-dev && \
    pip install -r /requirements.txt && \
    apk del --no-cache .build-deps
COPY ./crawlers /app
# WORKDIR /app
# CMD ["ash"]

FROM base AS final
WORKDIR /crawlers
COPY --from=crawlers /app .
WORKDIR /app
COPY --from=publish /app .

EXPOSE 80

ENTRYPOINT ["dotnet", "Host.dll"]