# Quick Start Guide - Identity Service

## 🚀 Getting Started (5 minutes)

### Prerequisites
- .NET 10 SDK installed
- Docker Desktop installed
- PostgreSQL 16 (or use Docker)

### Option 1: Quick Start with Docker (Recommended)

```bash
# Navigate to project directory
cd IdentityService

# Start PostgreSQL container
docker compose up postgres -d --wait

# Start the API (from src/IdentityService.Api directory)
cd src/IdentityService.Api
dotnet run

# Access the admin dashboard
# Open browser: http://localhost:5000
```

The API will be available at: `http://localhost:5000/api/v1`

### Option 2: Local Development (Without Docker)

```bash
# Ensure PostgreSQL is running locally on port 5432
# Update appsettings.json connection string if needed

# From project root
dotnet build

# Run migrations
dotnet-ef database update --project src/IdentityService.Infrastructure --startup-project src/IdentityService.Api

# Start API
cd src/IdentityService.Api
dotnet run

# Access dashboard: http://localhost:5000
```

---

## 📊 Admin Dashboard

Access the admin dashboard at: **http://localhost:5000**

The dashboard provides:
- **Dashboard**: Overview with stats and quick actions
- **Users**: Create and manage users
- **OAuth2 Clients**: Register OAuth2 clients for your applications
- **Scopes**: Define permission scopes (e.g., `api:users:read`)
- **Audit Logs**: View all security events

### Dashboard Features

1. **Create Users**
   - Register new users
   - Set email and password
   - Add first/last names

2. **Create OAuth2 Clients**
   - Register application clients
   - Configure redirect URIs
   - Set token lifetimes
   - Choose client type (Confidential/Public)

3. **Define Scopes**
   - Pattern: `api:featureName:action`
   - Examples: `api:users:read`, `api:posts:write`
   - Assign scopes to clients

4. **View Audit Logs**
   - Track all authentication events
   - Monitor failed login attempts
   - View client operations
   - See IP addresses and User-Agent info

---

## 🔌 API Endpoints

### Base URL
```
http://localhost:5000/api/v1
```

### Authentication Endpoints

#### Register User
```bash
POST /auth/register
Content-Type: application/json

{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

#### Login
```bash
POST /auth/login
Content-Type: application/json

{
  "username": "john_doe",
  "password": "SecurePassword123!"
}

Response:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "abc123...",
  "expiresIn": 3600,
  "twoFactorRequired": false
}
```

#### Refresh Token
```bash
POST /auth/refresh
Content-Type: application/json

{
  "refreshToken": "abc123..."
}
```

#### Setup 2FA (TOTP)
```bash
POST /auth/setup-totp
Authorization: Bearer {accessToken}

Response:
{
  "secret": "JBSWY3DPEBLW64TMMQ======",
  "qrCode": "otpauth://totp/IdentityService:john_doe?...",
  "manualEntry": "JBSWY3DPEBLW64TMMQ======"
}
```

#### Verify 2FA
```bash
POST /auth/verify-2fa
Content-Type: application/json

{
  "code": "123456"
}
```

#### Logout
```bash
POST /auth/logout
Authorization: Bearer {accessToken}
```

### Admin Endpoints (Requires Authorization)

#### Create OAuth2 Client
```bash
POST /admin/clients
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "name": "My Web App",
  "description": "Production web application",
  "clientType": "Confidential",
  "redirectUris": [
    "http://localhost:3000/callback",
    "https://myapp.com/callback"
  ],
  "allowedOrigins": [
    "http://localhost:3000",
    "https://myapp.com"
  ]
}
```

#### Create Scope
```bash
POST /admin/scopes
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "name": "api:users:read",
  "displayName": "Read Users",
  "description": "Allows reading user data"
}
```

#### Get Audit Logs
```bash
GET /admin/audit-logs?pageNumber=1&pageSize=20
Authorization: Bearer {adminToken}
```

#### List Users
```bash
GET /admin/users
Authorization: Bearer {adminToken}
```

---

## 🔐 OAuth2 Flows

### Authorization Code Flow (For Web Apps & SPAs)

```
1. Redirect user to: GET /oauth/authorize?client_id=xyz&redirect_uri=...&scope=api:users:read&response_type=code

2. User logs in and grants permission

3. Redirect back to: http://your-app/callback?code=abc123

4. Exchange code for token:
   POST /auth/login
   { code: "abc123", client_id: "xyz" }

5. Use access token:
   GET /api/resource
   Authorization: Bearer {accessToken}
```

### Client Credentials Flow (Service-to-Service)

```
POST /auth/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&
client_id=xyz&
client_secret=secret123&
scope=api:admin:read

Response:
{
  "access_token": "eyJhbGc...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

---

## 🧪 Testing with cURL

### Test User Registration
```bash
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Test123!@#",
    "firstName": "Test",
    "lastName": "User"
  }'
```

### Test Login
```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "password": "Test123!@#"
  }'
```

### Test Creating a Scope
```bash
curl -X POST http://localhost:5000/api/v1/admin/scopes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {YOUR_TOKEN}" \
  -d '{
    "name": "api:products:read",
    "displayName": "Read Products",
    "description": "Permission to read product data"
  }'
```

---

## 🔑 Scope Naming Convention

Use the pattern: `api:featureName:action`

### Examples
- `api:users:read` - Read user data
- `api:users:write` - Create/update users
- `api:products:read` - Read products
- `api:products:write` - Manage products
- `api:admin:read` - Read admin data
- `api:admin:write` - Admin operations
- `api:billing:read` - View billing info
- `api:billing:write` - Manage billing

---

## 📝 Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=identity_service;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "identityservice",
    "Audience": "identityservice-api",
    "ExpiryMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Environment Variables (Docker)

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=identity_service;Username=postgres;Password=postgres
Jwt__Issuer=identityservice
Jwt__Audience=identityservice-api
Jwt__ExpiryMinutes=60
```

---

## 🐛 Troubleshooting

### PostgreSQL Connection Failed
```bash
# Check if PostgreSQL is running
docker-compose ps

# If not running, start it
docker-compose up postgres -d --wait

# Check connection string in appsettings.json
```

### Port Already in Use (5000)
```bash
# Kill process using port 5000
# Windows
netstat -ano | findstr :5000
taskkill /PID {PID} /F

# Linux/Mac
lsof -ti:5000 | xargs kill -9
```

### Database Errors
```bash
# Reset database (deletes all data!)
dotnet-ef database drop --project src/IdentityService.Infrastructure --startup-project src/IdentityService.Api

# Reapply migrations
dotnet-ef database update --project src/IdentityService.Infrastructure --startup-project src/IdentityService.Api
```

### CORS Issues
The API is configured to allow all origins in development. If you need to restrict origins, update `Program.cs` CORS policy.

---

## 📚 Next Steps

1. **Create your first OAuth2 client** in the admin dashboard
2. **Define scopes** for your applications
3. **Register users** for testing
4. **Integrate with your applications** using the OAuth2 flows
5. **Monitor audit logs** for security events

---

## 📖 Complete Documentation

- See [README.md](README.md) for feature overview
- See [INSTALLATION.md](INSTALLATION.md) for deployment guides
- See [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md) for architecture details

---

## 🆘 Getting Help

- Check logs in `logs/` directory
- Review API response errors
- Check audit logs in dashboard for what went wrong
- Verify your OAuth2 client configuration
- Ensure scopes are properly assigned to clients

---

**Version**: 1.0.0  
**Last Updated**: December 16, 2025
