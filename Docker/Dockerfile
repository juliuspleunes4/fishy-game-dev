# ------ Base image
FROM ubuntu:22.04 AS builder

RUN apt-get update && apt-get install -y \
    libgtk-3-0 libnss3 libxss1 libasound2 libglu1-mesa libxcursor1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY release/ ./

RUN chmod +x ./release.x86_64

CMD ["./release.x86_64", "-batchmode", "-nographics", "-logFile"]
