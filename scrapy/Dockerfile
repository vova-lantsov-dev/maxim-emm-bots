FROM python:3.7.2-alpine
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