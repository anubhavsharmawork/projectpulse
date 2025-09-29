# API + Frontend Dockerfile (multi-stage)
# 1) Build Angular frontend
FROM node:18-alpine AS ui-build
WORKDIR /frontend

# Unique build arg to ensure a new image digest each build (use --build-arg BUILD_VERSION=...)
ARG BUILD_VERSION=dev

# Install deps and build UI
COPY ./frontend/package*.json ./
COPY ./frontend/angular.json ./
COPY ./frontend/tsconfig*.json ./
COPY ./frontend/src ./src
RUN npm install --no-audit --no-fund && npm run build

# 2) Build .NET API
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ./Project.sln ./
COPY ./src/Domain/Domain.csproj ./src/Domain/
COPY ./src/Application/Application.csproj ./src/Application/
COPY ./src/Infrastructure/Infrastructure.csproj ./src/Infrastructure/
COPY ./src/API/API.csproj ./src/API/
# Restore only the API project to avoid solution references to test projects
RUN dotnet restore ./src/API/API.csproj
COPY . .
RUN dotnet publish ./src/API/API.csproj -c Release -o /app/publish /p:UseAppHost=false

# 3) Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
EXPOSE 80

# Copy API publish output
COPY --from=build /app/publish .
# Copy built UI into API wwwroot so ASP.NET can serve it
COPY --from=ui-build /frontend/dist/ ./wwwroot/

# Bind to the Heroku-provided PORT at runtime
CMD ["bash", "-c", "dotnet ProjectApi.dll --urls=http://0.0.0.0:$PORT"]
