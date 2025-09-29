# Real-Time Task Management Tool

This repository contains a real-time task management tool.

Stack
- Frontend: Angular 16 + Angular Material (built in Docker and served by Kestrel from wwwroot)
- Backend: .NET 6 Web API + SignalR (Docker on Heroku)
- Database: PostgreSQL  
- File Storage: AWS S3 via AWS SDK (optional, currently limited to 40KB attachments)
- Security: JWT auth + ASP.NET Core role-based policies (Admin, Member)
- Patterns: Clean Architecture + CQRS with MediatR
- Logging: Serilog to STDOUT (Papertrail add-on)
- CI/CD: GitHub Actions (build, test, deploy to Heroku container registry)

Repository Structure
- src/
  - Domain/ (entities, enums)
  - Application/ (CQRS, DTOs, interfaces)
  - Infrastructure/ (EF Core, AWS S3, persistence, DI)
  - API/ (Web API, SignalR, Swagger, auth)
- frontend/ (Angular 16 app compiled into API image)
- heroku.yml (Docker deploy)
- app.json 

Deployment Platform
- Salesforce Heroku

Deployed App
- Single Heroku app: Docker image runs the API and serves the compiled SPA from wwwroot.

Prerequisites
- .NET 6 SDK
- Node.js 18+ and npm
- Angular CLI 16+
- Heroku CLI (optional)
- PostgreSQL client tools

Local Development
1) Copy env template
   - cp .env.template .env
   - Use ASPNETCORE_ENVIRONMENT=Development

2) Backend
   - dotnet restore && dotnet build
   - cd src/API && dotnet run
   - Swagger: https://localhost:5001/swagger

3) Database
   - Configure ConnectionStrings__Default or DATABASE_URL
   - Migrations run on startup (Program.cs); can also run `dotnet-ef` locally if preferred

4) Frontend
   - cd frontend && npm install
   - npm start
   - http://localhost:4200
   - For production deploy, the Angular app is built inside the Docker image.

Security
- JWT issuer/audience/secret via env vars (uses ASP.NET Core’s double-underscore mapping):
  - JWT__Issuer
  - JWT__Audience
  - JWT__Key (32+ bytes recommended)
- Role-based policies:
  - AdminPolicy (Admin)
  - MemberPolicy (Member, Admin)

Real-Time
- SignalR hub: /hubs/project
- Hub methods: JoinProject, TaskUpdate, Notify
- Server broadcasts a TaskUpdated event after a task is completed, enabling live updates on the dashboard.

File Storage
- AWS S3 SDK. 40KB max upload enforced server-side.
- Supported env vars for bucket/region:
  - Bucket: S3:Bucket, S3__Bucket, S3_BUCKET, S3_BUCKET_NAME
  - Region: S3:Region, S3__Region, AWS_REGION, AWS_DEFAULT_REGION
- Credentials: AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY
- Return URL is https://{bucket}.s3.{region}.amazonaws.com/{key}. For private buckets, switch to pre-signed URLs.

Logging & Observability
- Serilog logs to STDOUT (captured by Papertrail)
- Health checks: /health/ready and /health/live

Scalability
- API runs in a single dyno; scale with: heroku ps:scale web=2
- Static SPA assets are served by Kestrel (no separate Node dyno)

CI/CD (GitHub Actions)
- Builds Docker image, pushes to Heroku container registry, releases the app
- Frontend is compiled within the Docker build

Required GitHub Secrets
- HEROKU_API_KEY
- HEROKU_BACKEND_APP (the Heroku app name)

Heroku Add-ons
- heroku-postgresql
- papertrail
- (No dedicated aws-s3 add-on is required; S3 uses standard AWS env vars)

Destructive DB Reset (Important)
- Program.cs supports destructive resets and currently defaults them to ON in Production if not explicitly set:
  - DROP_SCHEMA_BEFORE_MIGRATE (default: true in Production)
  - WIPE_DB_ON_STARTUP (default: true in Production)
- For persistent environments, set both to false:
  - heroku config:set DROP_SCHEMA_BEFORE_MIGRATE=false WIPE_DB_ON_STARTUP=false -a <app>

Operational Init Endpoint (optional)
- POST /api/_ops/init?secret=XYZ
- Runs migrations and table checks
- Protect by setting INIT_SECRET in env; omit in production if not required

Manual Deploy (optional)
- heroku container:login
- heroku container:push web -a <app>
- heroku container:release web -a <app>

Notes
- If enabling file uploads in the UI, ensure S3 env vars are properly configured or switch to pre-signed URLs for private buckets.