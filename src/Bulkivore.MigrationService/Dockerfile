# Stage 1: Build bundle
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Install EF CLI tool
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy source files (excluding bin/obj via .dockerignore)
COPY . .

# Explicitly restore dependencies for both projects targeting Alpine runtime
RUN dotnet restore src/Bulkivore.Api/Bulkivore.Api.csproj -r linux-musl-x64

# Generate migration bundle
RUN dotnet-ef migrations bundle \
    --project src/Bulkivore.Api \
    --startup-project src/Bulkivore.Api \
    --output /app/efbundle \
    --self-contained \
    -r linux-musl-x64

# Stage 2: Minimal runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS final
WORKDIR /app

# Install GSSAPI / Kerberos dependency for Npgsql
RUN apk add --no-cache krb5-libs

COPY --from=build /app/efbundle .
RUN chmod +x ./efbundle

ENTRYPOINT ["./efbundle"]
