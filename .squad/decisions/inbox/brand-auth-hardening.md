# Auth Hardening: Lockout, Token Lifespan, Rate Limiting

**Status:** Implemented  
**Date:** 2026-04-15  
**Author:** Brand (Backend Dev)  
**Context:** Pre-production improvements flagged in Mikey's auth implementation review

## What Was Implemented

### 1. Account Lockout on Failed Login Attempts
- **Configuration:** 5 failed attempts → 15 minute lockout
- **Location:** `Program.cs` (AddIdentity options block) + `AuthEndpoints.cs` (PasswordSignInAsync)
- **Settings:**
  - `MaxFailedAccessAttempts = 5`
  - `DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`
  - `AllowedForNewUsers = true`
  - `lockoutOnFailure: true` in PasswordSignInAsync

### 2. Password Reset Token Lifespan
- **Configuration:** 24-hour token expiry
- **Location:** `Program.cs` (Configure<DataProtectionTokenProviderOptions>)
- **Setting:** `TokenLifespan = TimeSpan.FromHours(24)`
- **Applies to:** Tokens generated via `UserManager.GeneratePasswordResetTokenAsync`

### 3. Rate Limiting on Forgot-Password Endpoint
- **Policy:** Fixed-window rate limiter (3 requests per 15 minutes)
- **Location:** `Program.cs` (AddRateLimiter) + `AuthEndpoints.cs` (.RequireRateLimiting("forgot-password"))
- **Behavior:** Returns HTTP 429 (Too Many Requests) when limit exceeded
- **Test Environment:** Disabled in tests to avoid cross-test interference

### 4. Integration Tests for Password Reset Flow
- **Tests Added:** 7 new integration tests
  - **Forgot-password (4 tests):**
    - Known email → 200 (anti-enumeration by design)
    - Unknown email → 200 (anti-enumeration)
    - Empty email → 200 (handled gracefully)
    - Null body → 200 (handled gracefully)
  - **Reset-password (3 tests):**
    - Invalid token → 400 (rejected)
    - Missing required fields → 400
    - Unknown email → 400
- **Location:** `AuthEndpointTests.cs`
- **Test Infrastructure:** Added FakeEmailSender to replace SmtpEmailSender in tests (avoids SMTP connection)

## Files Modified

1. **src/Waymarked.Api/Program.cs**
   - Added lockout configuration in AddIdentity options
   - Added DataProtectionTokenProviderOptions configuration
   - Added rate limiting services + middleware (conditional on non-Test environment)

2. **src/Waymarked.Api/AuthEndpoints.cs**
   - Changed `lockoutOnFailure: false` → `lockoutOnFailure: true`
   - Applied `.RequireRateLimiting("forgot-password")` to forgot-password endpoint

3. **src/Waymarked.Api.Tests/AuthEndpointTests.cs**
   - Added 7 integration tests for forgot-password and reset-password

4. **src/Waymarked.Api.Tests/AuthWebApplicationFactory.cs**
   - Added FakeEmailSender to stub out SMTP in tests
   - Configured SmtpSettings with valid FrontendBaseUrl for tests
   - Disabled lockout in tests (AllowedForNewUsers = false, MaxFailedAccessAttempts = int.MaxValue)

## Test Results

- **Total tests:** 45 (38 pre-existing + 7 new)
- **Passing:** 42 (35 pre-existing + 7 new)
- **Failing:** 3 (pre-existing, unrelated to this work)
- **New tests:** All 7 passing

## Trade-offs & Decisions

### Lockout Configuration
- **5 attempts, 15 minutes:** Balances security (prevents brute force) with UX (not too aggressive for legitimate users who mistype passwords)
- **AllowedForNewUsers: true:** New registrations are subject to lockout from their first login (consistent security policy)

### Rate Limiting
- **3 requests per 15 minutes:** Allows 1-2 legitimate retry attempts for users who mistype their email, while blocking automated enumeration attacks
- **Disabled in Test environment:** Rate limiter state is shared across all tests in the test suite, causing cross-test interference. Disabling ensures tests are isolated.

### Token Lifespan
- **24 hours:** Standard industry practice for password reset tokens. Long enough for users to complete the flow at their convenience, short enough to minimize exposure if a token is compromised.

## Dependencies

- **Microsoft.AspNetCore.RateLimiting:** Added via using directive (part of ASP.NET Core 10)
- **System.Threading.RateLimiting:** Added via using directive (part of .NET 10)
- **No new NuGet packages required**

## Notes

- Rate limiting middleware placement: AFTER `UseAuthentication()`, BEFORE `UseAuthorization()`
- Forgot-password endpoint always returns 200 (even for unknown emails) to prevent user enumeration attacks
- Reset-password endpoint returns 400 for validation failures (token validation happens server-side, so enumeration isn't a concern)
