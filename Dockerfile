FROM microsoft/dotnet:2.2-sdk-stretch AS build

ARG VERSION=0.0.0

WORKDIR /app

COPY /. ./

RUN dotnet restore

RUN dotnet publish -c Release -r linux-x64 -o out -p:Version=$VERSION.0

FROM microsoft/dotnet:2.2-runtime-deps-stretch-slim as runtime

RUN apt update && apt install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    wget \
    --no-install-recommends \
    && curl -sL https://deb.nodesource.com/setup_10.x | bash - \
    && apt-get update && apt-get install -y \
	nodejs \
    npm \
    --no-install-recommends

# Prepare for DotnetCore SDK 3.0 install
RUN wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg \
    && mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/ \
    && wget -q https://packages.microsoft.com/config/debian/9/prod.list \
    && mv prod.list /etc/apt/sources.list.d/microsoft-prod.list \
    && chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg \
    && chown root:root /etc/apt/sources.list.d/microsoft-prod.list

# Install DotnetCoreSDK 3.0
RUN apt-get update && apt-get install -y dotnet-sdk-3.0 --no-install-recommends

# Install OpenJDK-8
RUN mkdir -p /usr/share/man/man1 && \
    apt-get update && \
    apt-get install -y openjdk-8-jre-headless && \
    apt-get install -y ant && \
    apt-get clean;

# Fix certificate issues
RUN apt-get update && \
    apt-get install ca-certificates-java && \
    apt-get clean && \
    update-ca-certificates -f;

# Setup JAVA_HOME -- useful for docker commandline
ENV JAVA_HOME /usr/lib/jvm/java-8-openjdk-amd64/
RUN export JAVA_HOME

RUN apt-get -y install gcc git

RUN apt-get purge --auto-remove -y curl gnupg \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/out ./

RUN ./JsMinBenchmark init-tools

ENTRYPOINT [ "./JsMinBenchmark", "benchmark",  "--output", "console" ]
