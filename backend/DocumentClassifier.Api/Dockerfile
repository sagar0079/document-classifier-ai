# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DocumentClassifier.Api.csproj .
RUN dotnet restore DocumentClassifier.Api.csproj

COPY . .
RUN dotnet publish DocumentClassifier.Api.csproj -c Release -o /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# tesseract-ocr does the OCR; poppler-utils converts PDF pages to images before OCR
RUN apt-get update && \
    apt-get install -y --no-install-recommends tesseract-ocr poppler-utils && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

# Render (and most PaaS platforms) inject $PORT at runtime - bind to it, falling back to 8080 locally
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet DocumentClassifier.Api.dll"]
