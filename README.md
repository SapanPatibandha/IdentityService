# Identity Service

A comprehensive OAuth2/OpenID Connect-compliant identity authentication service built with .NET 10, PostgreSQL, and clean architecture patterns.

## Features

✅ **OAuth2 Authorization Framework**
- Support for multiple grant types (Authorization Code, Client Credentials, Refresh Token)
- JWT token generation with RSA asymmetric key cryptography
- Token refresh and revocation capabilities
- Scope-based access control with pattern: `api:featureName:action`

✅ **Authentication Methods**
- Username/password authentication with bcrypt hashing
- Email verification workflow
- Two-Factor Authentication (2FA):
  - TOTP (Time-based One-Time Password) via authenticator apps
  - Email-based verification codes
- Account lockout after failed attempts

✅ **Multi-Tenant Support**
- OAuth2 client management
- Scope definition and assignment
- Rate limiting and security policies per client

✅ **Audit & Compliance**
- Comprehensive audit logging for all authentication events
- IP tracking and user agent logging
- Success/failure tracking for security monitoring

✅ **Admin Management**
- Create and manage OAuth2 clients
- Define and manage scopes
- View detailed audit logs
- User management endpoints

✅ **Deployment Ready**
- Docker & Docker Compose support
- Environment-based configuration
- PostgreSQL database with migrations
- Structured logging with Serilog

---

## Quick Start - Docker

```bash
docker-compose up -d
```

Service will be available at `http://localhost:8080`

---

## Architecture

Clean Architecture with 4 layers:
- **Core**: Domain entities and interfaces
- **Application**: Business logic and DTOs
- **Infrastructure**: Database, repositories, external services
- **API**: REST endpoints and controllers

---

## API Quick Reference

### Register User
```bash
POST /api/v1/auth/register
{ "username": "user", "email": "user@example.com", "password": "SecurePass123!" }
```

### Login
```bash
POST /api/v1/auth/login
{ "username": "user", "password": "SecurePass123!" }
```

### Create OAuth2 Client (Admin)
```bash
POST /api/v1/admin/clients
Authorization: Bearer {token}
{ "name": "My App", "clientType": "Public" }
```

### Create Scope (Admin)
```bash
POST /api/v1/admin/scopes
Authorization: Bearer {token}
{ "name": "api:users:read", "displayName": "Read Users" }
```

### View Audit Logs (Admin)
```bash
GET /api/v1/admin/audit-logs
Authorization: Bearer {token}
```

---

## Configuration

### Environment Variables
```bash
ConnectionStrings__DefaultConnection=Host=postgres;Database=identity_service;...
Jwt__Issuer=identityservice
Jwt__Audience=identityservice-api
ASPNETCORE_ENVIRONMENT=Production
```

---

## Scope Naming Convention

Pattern: `api:featureName:action`

Examples:
- `api:users:read` - Read user data
- `api:users:write` - Modify users  
- `api:orders:read` - View orders
- `api:products:delete` - Delete products

---

## Security Features

- 🔐 Bcrypt password hashing
- 🔑 RSA-256 JWT signing
- 🔄 Refresh token rotation
- 🛡️ Account lockout (5 attempts)
- 📧 Email verification required
- 🔏 2FA (TOTP + Email)
- 📊 Comprehensive audit logging
- ⚡ Rate limiting support

---

## Database

PostgreSQL with EF Core migrations. Tables:
- Users
- Clients
- Scopes
- RefreshTokens
- TwoFactorVerifications
- AuditLogs

Migrations auto-apply on startup.

---

## Local Development

```bash
# Build
dotnet build

# Run migrations
dotnet-ef database update --project src/IdentityService.Infrastructure

# Run API
cd src/IdentityService.Api && dotnet run
```

API at `https://localhost:5001`

---

## Integration Examples

**C#**
```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
```

**JavaScript**
```javascript
fetch('/api/v1/admin/users', {
  headers: { 'Authorization': `Bearer ${token}` }
});
```

**cURL**
```bash
curl -H "Authorization: Bearer TOKEN" http://localhost:8080/api/v1/admin/users
```

---

## Deployment

### Docker Compose
```bash
docker-compose up -d
```

### Kubernetes
[Deployment manifests coming soon]

### Cloud Platforms
- ☁️ AWS (ECS, RDS)
- ☁️ Azure (App Service, Database)
- ☁️ Google Cloud (Cloud Run, Cloud SQL)

---

## Monitoring & Logs

Logs are written to:
- Console (development)
- File: `logs/identityservice-{date}.txt`
- Structure: JSON format for log aggregation

---

## Troubleshooting

**Database connection failed**
- Ensure PostgreSQL is running
- Verify connection string
- Check firewall rules (port 5432)

**JWT validation failed**
- Check token hasn't expired (1 hour default)
- Verify JWT settings in appsettings.json
- Ensure correct issuer/audience

**2FA setup issues**
- Verify email service is configured
- Check TOTP algorithm support

---

## Support & Documentation

- API Documentation: See `/openapi/v1.json` for Swagger
- Issues: Open GitHub issue
- Contributing: Pull requests welcome

---

## Roadmap

- Admin dashboard UI
- SMS 2FA
- SAML/LDAP
- OpenID Connect
- Advanced analytics
- Kubernetes manifests

---

Made with ❤️ for secure authentication