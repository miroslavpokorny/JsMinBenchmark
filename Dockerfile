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
    --no-install-recommends \
    && curl -sL https://deb.nodesource.com/setup_10.x | bash - \
    && apt-get update && apt-get install -y \
	nodejs \
    npm \
    --no-install-recommends

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

RUN apt-get purge --auto-remove -y curl gnupg \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/out ./

RUN ./JsMinBenchmark init-tools

ENTRYPOINT [ "./JsMinBenchmark", "benchmark" ]
