# GitHub Copilot Instructions 

## Project Context
This is a secure, enterprise-grade ASP.NET Core Web API application. All code must follow OWASP Top 10 security practices, SOLID principles, and Microsoft security guidelines.

---

## Architecture & Design Principles

### Technology Stack
- **Framework**: ASP.NET Core 8+
- **Language**: C# 12+ with latest features (nullable reference types, records, pattern matching)
- **Validation**: FluentValidation for input validation


### Design Patterns
- **Repository Pattern** with Dependency Injection for data access
- **CQRS** (Command Query Responsibility Segregation) for complex operations
- **Async/Await** for all I/O operations (no blocking calls)
- **Factory Pattern** for complex object creation
- **Strategy Pattern** for pluggable business logic
- **Middleware Pattern** for cross-cutting concerns (auth, logging, error handling)


## OWASP Top 10 Security Requirements

### 1. Broken Access Control
- Enforce **global authorization policy**: Require authentication on all endpoints by default
- Implement **role-based access control (RBAC)** with policy-based authorization
- Use `[Authorize(Policy = "PolicyName")]` attributes on all protected endpoints
- Never expose internal identifiers (IDs) directly—use GUIDs or indirect references
- Validate user permissions server-side on every request
- Example (Program.cs):
```csharp
services.AddControllers(config =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    config.Filters.Add(new AuthorizeFilter(policy));
});
```

### 2. Cryptographic Failures
- Encrypt sensitive data **at rest**: Use AES-256-GCM via Azure Key Vault or ProtectedData
- Encrypt data **in transit**: Enforce HTTPS/TLS 1.2+ (Kestrel default)
- Implement **HSTS** (HTTP Strict-Transport-Security) headers
- Rotate encryption keys regularly using Azure Key Vault
- Never hardcode secrets—use environment variables, Azure Key Vault, or Managed Identity
- Use secure algorithms only: AES-GCM (encryption), RSA-OAEP (asymmetric), PBKDF2/Argon2 (passwords)

### 3. Injection (SQL, LDAP, OS Command)
- **Always use parameterized queries** via Entity Framework Core or parameterized SQL
- Validate and sanitize all user inputs using FluentValidation
- Never execute OS commands with user input—use dedicated safe APIs

### 4. Insecure Design
- Apply **threat modeling** at the start of every major feature
- Design with **secure defaults**: Require authentication, enforce validation, minimize permissions
- Implement **least privilege principle**: Users/services get only required permissions

### 5. Security Misconfiguration
- follow best practices

### 6. Vulnerable and Outdated Components
- Keep ASP.NET Core, NuGet packages, and dependencies updated
- Use `dotnet outdated` tool to identify stale packages
- Run automated dependency scanning in CI/CD pipeline
- Review security advisories from NuGet.org and Microsoft Security Advisory
- Pin package versions in production for stability

### 7. Authentication Failures
- Use **Argon2** (preferred) or **PBKDF2** for password hashing (never MD5, SHA1)
- Implement **account lockout**: Lock after 5 failed attempts for 15 minutes
- Generate new session/JWT tokens on login; invalidate old ones
- Use **short-lived JWT tokens** (15-30 min) with refresh tokens (7 days)
- Validate JWT signature and expiry; never accept `"alg": "none"`

### 8. Software and Data Integrity Failures
- Verify integrity of critical updates before deployment
- Use **code signing** for NuGet packages and compiled binaries
- Maintain secure CI/CD pipeline with access controls

### 9. Logging & Monitoring Failures
- Log **all critical security events**: Authentication (success/failure), authorization failures, admin actions
- Include context: timestamp, user ID, IP address, resource accessed, outcome
- **Mask sensitive data** in logs: Passwords, PII, payment info, API keys
- Use **structured logging** via Serilog for better searchability and analysis
- Set up **actionable alerts** for suspicious patterns: Multiple failed logins, unauthorized access attempts
- Never log full request/response bodies if they contain sensitive data


### 10. Server-Side Request Forgery (SSRF)
- Never make outbound HTTP requests to user-supplied URLs without validation
- **Allow-list** only safe, trusted domains and IP ranges
- **Deny localhost** and cloud metadata endpoints (127.0.0.1, 169.254.169.254, etc.)
- Validate URL format using `Uri.IsWellFormedUriString()` before use


## Code Quality Standards

### General Principles
- Use **nullable reference types**: `#nullable enable` in all files
- Favor **async/await** over synchronous calls; never use `.Result` or `.Wait()`
- Implement **proper exception handling**: Catch specific exceptions, log context, return meaningful HTTP status codes
- Use **strong typing**—avoid `dynamic` unless absolutely necessary
- Follow **C# naming conventions**: PascalCase for public members, camelCase for private
- Keep methods focused and under 20 lines; use helper methods for complex logic
- Write code with "composability" in mind—favor composition over inheritance

### Input Validation
- Validate **all user inputs** server-side using FluentValidation
- Define and validate against **allow-lists** when accepting specific values
- Never trust client-side validation alone
- Return **detailed validation errors** to help users correct issues (without exposing system details)


### Error Handling & HTTP Responses
- Return appropriate **HTTP status codes**:
  - 400 Bad Request for validation errors
  - 401 Unauthorized for authentication failures
  - 403 Forbidden for authorization failures
  - 404 Not Found for missing resources
  - 500 Internal Server Error (don't expose stack traces in production)
- Create custom `ProblemDetails` responses for errors
- Never expose implementation details, stack traces, or system information in responses

### Dependency Injection & SOLID Principles
- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Derived classes must be substitutable for base classes
- **Interface Segregation**: Clients depend on specific interfaces, not general ones
- **Dependency Inversion**: Depend on abstractions, not concrete implementations
- Register all dependencies in `Program.cs` using extension methods


### Entity Framework Core
- Use **async queries**: `ToListAsync()`, `FirstOrDefaultAsync()`, `SaveChangesAsync()`
- Implement **change tracking** properly—don't track unnecessary entities
- Use **projections** (`Select`) to retrieve only needed columns
- Implement **soft deletes** for audit trails
- Use **value converters** for domain types and enums

### Unit Tests
- Write **isolated unit tests** that don't depend on other tests' state
- Use **AAA pattern**: Arrange, Act, Assert
- Mock external dependencies (repositories, services, APIs)
- Aim for **85%+ code coverage** on critical paths (business logic, security)
- Use xUnit/NUnit with Moq for mocking

## API Design Standards

### RESTful Endpoints
- Use **meaningful resource names** (nouns): `/users`, `/orders`, `/products`
- Use HTTP methods correctly: `GET` (read), `POST` (create), `PUT`/`PATCH` (update), `DELETE` (delete)
- Return **appropriate HTTP status codes** (200, 201, 400, 404, 500)
- Support **pagination** and **filtering** for list endpoints
- Use **HTTP verb overrides** sparingly and document them
- Example endpoints:
```
GET    /api/v1/users                 # List users
POST   /api/v1/users                 # Create user
GET    /api/v1/users/{id}            # Get specific user
PUT    /api/v1/users/{id}            # Update user
DELETE /api/v1/users/{id}            # Delete user
```

### Request/Response Format
- Use **consistent DTO (Data Transfer Object)** naming: `CreateUserRequest`, `UserResponse`
- Return **wrapped responses** with metadata:
- Always include **API versioning**: `/api/v1/...`
- Document APIs with **Swagger/OpenAPI** annotations

### CORS Configuration
- Define **specific, allowed origins** (never use `*` in production)
- Restrict **allowed methods** to necessary ones (GET, POST, PUT, DELETE)
- Specify **allowed headers** explicitly

## Performance & Optimization

### Database
- Use **indexes** on frequently queried columns and foreign keys
- Implement **query optimization**: Use `Select()` projections, avoid N+1 queries
- Use **caching** for read-heavy operations (Redis, distributed cache)
- Batch operations when processing large datasets
- Monitor slow queries and optimize execution plans

### API Response
- Implement **pagination** for list endpoints (default: 50 items per page)
- Use **compression** (gzip) for responses
- Implement **response caching** headers (ETag, Last-Modified) where appropriate
- Minimize response payload size via projections

### Async/Await
- Use `async` all the way down—never block with `.Result` or `.Wait()`
- Implement **timeout policies** for external API calls (5-30 seconds)
- Use **Polly** for resilience: Retry, circuit breaker, timeout policies

## Logging & Monitoring

### Structured Logging (Serilog)
- Log with **contextual information**: UserId, RequestId, TraceId
- Use **appropriate log levels**: Debug, Information, Warning, Error, Fatal
- Include **timestamps in UTC** (ISO 8601 format)
- **Never log sensitive data**: Passwords, API keys, payment info, PII



## Documentation

### Code Comments
- Write **self-documenting code**—use clear names instead of comments
- Comment **why**, not **what**—explain business logic and non-obvious decisions
- Keep comments **up-to-date** with code changes
- Use XML documentation for public APIs:
```csharp
/// <summary>
/// Creates a new user with the provided email and password.
/// </summary>
/// <param name="email">The user's email address</param>
/// <param name="password">The user's password (min 12 characters)</param>
/// <returns>The newly created user ID</returns>
/// <exception cref="ArgumentNullException">Thrown when email or password is null</exception>
public async Task<Guid> CreateUserAsync(string email, string password)
```

## Testing Strategy

### Test Pyramid (Recommended Distribution)
- **Unit Tests**: 85% - Fast, isolated, test single methods
- **Integration Tests**: 20% - Test multiple components together (database, API)
- **End-to-End Tests**: 10% - Test complete user workflows (Playwright)

### Test Data Management
- Use **test fixtures** for consistent, reusable test data
- Clean up test data **after each test** (in-memory databases or transactions)
- Use **database containers** (TestContainers) for integration tests
- Mock external services (APIs, emails, payments)

### Performance Testing
- Load test critical endpoints (user registration, payment processing)
- Identify bottlenecks and optimize before production release

---

## Deployment & CI/CD

### Automated Checks (On Every PR)
- **Build**: Compile code, run all tests
- **Code Analysis**: SonarQube for code quality
- **Security Scanning**: Dependency scanning for vulnerabilities, SAST for code issues


## Performance Checklist for Copilot

### When Making Large Changes
1. Start with a **detailed plan** before modifications
2. Plan should include:
   - All functions/sections needing changes
   - Sequence of changes and dependencies
   - Estimate number of edits required
3. Format plan clearly: "Working with [filename]. Planned edits: [count]"

### When Making Code Changes
- Concentrate on **one conceptual change at a time**
- Explain **rationale** for each change
- Verify changes **align with project standards** and OWASP guidelines
- Keep edits to **one file at a time** when possible to prevent corruption

Copilot must NOT create new .md, .txt, or documentation files
unless the user explicitly requests documentation.


DO NOT RUN API INTEGRATION TEST AFTER EVERY CHANGE
