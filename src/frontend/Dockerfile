# Stage 1: Build the client-side Nuxt SPA
FROM oven/bun:latest AS build
WORKDIR /app

COPY package.json bun.lock* ./
RUN bun install --frozen-lockfile

COPY . .
# Generates static assets into .output/public
RUN bun run generate

# Stage 2: Serve with Nginx
FROM nginx:1-alpine-slim

# Copy static assets to Nginx html directory
COPY --from=build /app/.output/public /usr/share/nginx/html

# Copy template for runtime environment variable substitution
COPY nginx.conf.template /etc/nginx/templates/default.conf.template

EXPOSE 80