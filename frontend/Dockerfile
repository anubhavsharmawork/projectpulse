# Angular UI Dockerfile (multi-stage)
FROM node:18-alpine AS build
WORKDIR /app

# Unique build arg to ensure a new image digest each build (use --build-arg BUILD_VERSION=...)
ARG BUILD_VERSION=dev

# Install deps first (better cache)
COPY package*.json ./
COPY angular.json ./
COPY tsconfig*.json ./
COPY src ./src
# Use npm install to avoid lockfile requirement in CI
RUN npm install --no-audit --no-fund && npm run build

FROM node:18-alpine AS runtime
WORKDIR /app

# Bring through build arg for labeling
ARG BUILD_VERSION=dev

# Helpful metadata to break cache and track releases
LABEL org.opencontainers.image.version=$BUILD_VERSION
ENV UI_BUILD_VERSION=$BUILD_VERSION

RUN npm i -g serve@14.2.3
# Copy built app
COPY --from=build /app/dist ./dist
ENV NODE_ENV=production
EXPOSE 8080
CMD ["sh", "-c", "serve -s dist -l ${PORT:-8080}"]
