# Architecture Decision Records

## ADR-001: Clean Architecture with CQRS Pattern
**Status:** Accepted  
**Context:** Need separation of concerns across Domain, Application, Infrastructure, and API layers with scalable read/write operations  
**Decision:** Implement Clean Architecture with MediatR-based CQRS, EF Core for PostgreSQL persistence, and FluentValidation for input validation  
**Consequences:** Business logic is fully testable in isolation, commands and queries scale independently, and infrastructure can be swapped without touching domain

## ADR-002: SignalR with JWT Authentication
**Status:** Accepted  
**Context:** Need real-time collaboration (task updates, @mention notifications) with secure stateless authentication  
**Decision:** Use ASP.NET Core SignalR hub with `[Authorize]` attribute and JWT Bearer tokens (`Microsoft.AspNetCore.Authentication.JwtBearer`) plus BCrypt password hashing  
**Consequences:** Instant project-scoped updates via group messaging, scalable across instances, token expiry and refresh management required

## ADR-003: Template-Driven Domain Engine
**Status:** Accepted  
**Context:** The platform must serve multiple industry verticals (IT, Healthcare, Construction, Infrastructure, Public Safety, Economic Development, Technology) without forking the codebase or adding domain-specific branching in command/query handlers  
**Decision:** Define each vertical as a JSON seed template (`Infrastructure/Seed/Templates/*.json`) that configures workflow states, work item type labels, custom fields, notification rules, and asset catalogs. Templates are loaded at startup by `DomainTemplateSeeder` and applied via data-driven resolution at runtime. The `DomainAssetConfig` entity maps each domain to its asset types with defaults for depreciation, maintenance, and compliance  
**Consequences:** Adding a new industry vertical requires one JSON file and one migration — zero changes to core handlers. All domains share the same pipeline, which reduces testing surface and eliminates regression risk from domain-specific code paths. The trade-off is that truly exotic domain logic that cannot be expressed as configuration would require a strategy pattern extension point

## ADR-004: ISO 8601 Date/Time Serialization
**Status:** Accepted  
**Context:** Multi-timezone teams and long-lived projects require deterministic date handling across frontend, backend, and database. Locale-dependent formatting and implicit timezone conversions cause silent data corruption over months  
**Decision:** Implement a custom `Iso8601DateTimeConverter` (backend, `System.Text.Json`) and a matching `Iso8601Interceptor` (frontend, Angular `HttpInterceptor`). All dates cross the wire as UTC with `Z` suffix (`yyyy-MM-ddTHH:mm:ss.fffZ`). The backend converter accepts ISO 8601 variants and date-only strings for flexibility; it always writes strict UTC  
**Consequences:** No timezone drift regardless of server locale, client locale, or database timezone setting. Date-only inputs from HTML date pickers are preserved and handled correctly. All audit timestamps are unambiguously UTC

## ADR-005: Per-Tenant PII Field-Level Encryption
**Status:** Accepted  
**Context:** Multi-tenant SaaS handling healthcare, government, and enterprise data must protect personally identifiable information at rest, with tenant isolation extending to encryption keys  
**Decision:** `IEncryptionService` provides `Encrypt`/`Decrypt` with per-tenant key derivation using AES-256. Ciphertext is prefixed with `ENC:` making decryption self-describing. When encryption is not configured, the service acts as a pass-through — graceful degradation over hard failure  
**Consequences:** Sensitive fields are encrypted transparently at the application layer. A database breach does not expose PII in plaintext. Per-tenant key derivation ensures one tenant's key cannot decrypt another tenant's data. The `ENC:` prefix allows safe migration of existing plaintext data without a flag day

## ADR-006: Append-Only Audit Trail via EF Core Interceptor
**Status:** Accepted  
**Context:** Compliance, debugging, and operational transparency require an immutable record of every entity change with the acting user, timestamp, and before/after state  
**Decision:** Implement `AuditInterceptor` as an EF Core `SaveChangesInterceptor` that captures all Added/Modified/Deleted entities, serializes old and new values to JSON, and writes to an append-only `AuditLog` table. The interceptor skips its own entity type (`AuditLog`) and high-frequency read models (`MentionNotification`) to prevent recursion and noise  
**Consequences:** Complete change history for all business entities with no application-layer code changes required when new entities are added. Audit records are never updated or deleted. The trade-off is increased write amplification — every business write generates an additional audit insert
