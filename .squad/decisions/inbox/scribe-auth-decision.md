### 2026-04-13: Auth solution selected — ASP.NET Core Identity

**Status:** APPROVED | **Owner:** Brand (Backend Dev)

**Decision:** Use ASP.NET Core Identity with PostgreSQL for user authentication.

**Selected by:** Josh Hills after reviewing options analysis from Mikey.

**Rejected options:** 
- Keycloak (overkill)
- Auth0 (vendor lock-in on password hashes)
- Microsoft Entra External ID (complexity disproportionate to need)

**Rationale:** In-framework, cookie auth natural for YARP/vanilla JS, full GDPR data ownership, shared DB with future route saving, no vendor lock-in.

**Architecture:** PostgreSQL Aspire container resource → EF Core Identity stores → cookie auth middleware → YARP transparent forwarding.

**Endpoints:** 
- POST /api/auth/register
- POST /api/auth/login
- POST /api/auth/logout
- GET /api/auth/me
