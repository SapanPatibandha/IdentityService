# Identity Service API Reference

**Base URL**: `http://localhost:5000/api/v1` (Development)

**Authentication**: Bearer Token (JWT)

---

## 📚 Table of Contents
1. [Authentication Endpoints](#authentication-endpoints)
2. [Admin Endpoints](#admin-endpoints)
3. [Response Formats](#response-formats)
4. [Error Handling](#error-handling)
5. [Examples](#examples)

---

## 🔐 Authentication Endpoints

### POST /auth/register
Register a new user account.

**Request:**
```json
{
  "username": "john_doe",
  "email": "john@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**Response (201):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "john_doe",
  "email": "john@example.com",
  "isEmailVerified": false,
  "createdAt": "2025-12-16T10:30:00Z"
}
```

**Error Cases:**
- `400` - Invalid input (missing fields, weak password)
- `409` - User already exists

---

### POST /auth/login
Authenticate user with username/password.

**Request:**
```json
{
  "username": "john_doe",
  "password": "SecurePassword123!"
}
```

**Response (200):**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abcdef123456",
  "expiresIn": 3600,
  "twoFactorRequired": false,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "john_doe",
    "email": "john@example.com"
  }
}
```

**Error Cases:**
- `401` - Invalid credentials
- `403` - Account locked (5 failed attempts)
- `429` - Too many login attempts

**Notes:**
- If `twoFactorRequired` is true, user must complete 2FA before getting access token
- Account locks for 15 minutes after 5 failed attempts
- Tokens expire after configured time (default 1 hour)

---

### POST /auth/verify-2fa
Verify two-factor authentication code.

**Request:**
```json
{
  "code": "123456",
  "method": "totp"
}
```

**Response (200):**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "abcdef123456",
  "expiresIn": 3600
}
```

**Error Cases:**
- `400` - Invalid code
- `401` - Code expired (valid for 30 seconds)

---

### POST /auth/refresh
Refresh expired access token using refresh token.

**Request:**
```json
{
  "refreshToken": "abcdef123456"
}
```

**Response (200):**
```json
{
  "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "newRefreshToken789",
  "expiresIn": 3600,
  "rotated": true
}
```

**Error Cases:**
- `400` - Invalid token
- `401` - Token expired/revoked

**Notes:**
- Refresh tokens are rotated (old token replaced with new one)
- Keep track of new refresh token for next refresh

---

### POST /auth/setup-totp
Setup Time-based One-Time Password (TOTP) 2FA.

**Request:**
```
POST /auth/setup-totp
Authorization: Bearer {accessToken}
```

**Response (200):**
```json
{
  "secret": "JBSWY3DPEBLW64TMMQ======",
  "qrCode": "otpauth://totp/IdentityService:john_doe?secret=JBSWY3...",
  "manualEntry": "JBSWY3DPEBLW64TMMQ======",
  "expiresAt": "2025-12-16T11:30:00Z"
}
```

**How to Use:**
1. Scan QR code with authenticator app (Google Authenticator, Authy, etc.)
2. Or manually enter the secret key
3. Get 6-digit code from app
4. Use code in `/auth/verify-2fa` endpoint

**Error Cases:**
- `401` - Unauthorized (not logged in)
- `400` - TOTP already enabled

---

### POST /auth/logout
Logout and revoke refresh token.

**Request:**
```
POST /auth/logout
Authorization: Bearer {accessToken}

{
  "refreshToken": "abcdef123456"
}
```

**Response (200):**
```json
{
  "message": "Logged out successfully"
}
```

**Notes:**
- Refresh token becomes invalid after logout
- Can revoke all user tokens by passing null

---

## 👑 Admin Endpoints

All admin endpoints require `Authorization: Bearer {adminToken}` header.

---

### POST /admin/clients
Create a new OAuth2 client.

**Request:**
```json
{
  "name": "Web App",
  "description": "My web application",
  "clientType": "Confidential",
  "redirectUris": [
    "http://localhost:3000/callback",
    "https://myapp.com/callback"
  ],
  "allowedOrigins": [
    "http://localhost:3000",
    "https://myapp.com"
  ],
  "accessTokenLifetime": 3600,
  "refreshTokenLifetime": 2592000,
  "allowRefreshTokenRotation": true
}
```

**Response (201):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "clientId": "webapp_prod_123",
  "clientSecret": "$2a$11$...",
  "name": "Web App",
  "clientType": "Confidential",
  "isActive": true,
  "redirectUris": ["http://localhost:3000/callback"],
  "createdAt": "2025-12-16T10:30:00Z"
}
```

**Important:** Save the `clientSecret` - it won't be shown again!

---

### GET /admin/clients/{id}
Get OAuth2 client details.

**Response (200):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "clientId": "webapp_prod_123",
  "name": "Web App",
  "description": "My web application",
  "clientType": "Confidential",
  "isActive": true,
  "redirectUris": ["http://localhost:3000/callback"],
  "allowedOrigins": ["http://localhost:3000"],
  "accessTokenLifetime": 3600,
  "refreshTokenLifetime": 2592000,
  "scopes": [
    {
      "id": "scope123",
      "name": "api:users:read",
      "displayName": "Read Users"
    }
  ],
  "createdAt": "2025-12-16T10:30:00Z",
  "updatedAt": "2025-12-16T10:30:00Z"
}
```

---

### POST /admin/scopes
Create a new permission scope.

**Request:**
```json
{
  "name": "api:users:read",
  "displayName": "Read Users",
  "description": "Permission to read user data"
}
```

**Response (201):**
```json
{
  "id": "scope123",
  "name": "api:users:read",
  "displayName": "Read Users",
  "description": "Permission to read user data",
  "isActive": true,
  "createdAt": "2025-12-16T10:30:00Z",
  "updatedAt": "2025-12-16T10:30:00Z"
}
```

**Naming Convention:**
- Pattern: `api:featureName:action`
- Examples: `api:users:read`, `api:posts:write`, `api:admin:delete`

---

### GET /admin/scopes
List all available scopes.

**Query Parameters:**
- `pageNumber` (int, optional, default: 1)
- `pageSize` (int, optional, default: 20)

**Response (200):**
```json
[
  {
    "id": "scope123",
    "name": "api:users:read",
    "displayName": "Read Users",
    "description": "Permission to read user data",
    "isActive": true,
    "createdAt": "2025-12-16T10:30:00Z"
  },
  {
    "id": "scope124",
    "name": "api:users:write",
    "displayName": "Write Users",
    "isActive": true,
    "createdAt": "2025-12-16T10:31:00Z"
  }
]
```

---

### GET /admin/scopes/{id}
Get specific scope details.

**Response (200):**
```json
{
  "id": "scope123",
  "name": "api:users:read",
  "displayName": "Read Users",
  "description": "Permission to read user data",
  "isActive": true,
  "createdAt": "2025-12-16T10:30:00Z",
  "updatedAt": "2025-12-16T10:30:00Z"
}
```

---

### GET /admin/audit-logs
Get all audit logs with pagination.

**Query Parameters:**
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 20)
- `userId` (Guid, optional) - Filter by user
- `action` (string, optional) - Filter by action

**Response (200):**
```json
{
  "items": [
    {
      "id": "log123",
      "userId": "550e8400-e29b-41d4-a716-446655440000",
      "clientId": null,
      "action": "LOGIN_SUCCESS",
      "resource": "User",
      "description": "User successfully logged in",
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "success": true,
      "createdAt": "2025-12-16T10:30:00Z"
    },
    {
      "id": "log124",
      "userId": "550e8400-e29b-41d4-a716-446655440000",
      "clientId": null,
      "action": "LOGIN_FAILED",
      "resource": "User",
      "description": "Invalid password",
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "success": false,
      "errorMessage": "Invalid credentials",
      "createdAt": "2025-12-16T10:25:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 100
}
```

**Common Actions:**
- `LOGIN_SUCCESS` - User logged in
- `LOGIN_FAILED` - Failed login attempt
- `REGISTRATION` - New user registered
- `TOKEN_REFRESH` - Token refreshed
- `2FA_SETUP` - 2FA enabled
- `2FA_VERIFIED` - 2FA code verified
- `LOGOUT` - User logged out

---

### GET /admin/audit-logs/user/{userId}
Get audit logs for a specific user.

**Response (200):**
```json
[
  {
    "id": "log123",
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "action": "LOGIN_SUCCESS",
    "resource": "User",
    "success": true,
    "ipAddress": "192.168.1.100",
    "createdAt": "2025-12-16T10:30:00Z"
  }
]
```

---

### GET /admin/users
Get all registered users.

**Query Parameters:**
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 20)

**Response (200):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "john_doe",
    "email": "john@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "isEmailVerified": true,
    "twoFactorEnabled": true,
    "isLocked": false,
    "failedLoginAttempts": 0,
    "createdAt": "2025-12-16T10:30:00Z",
    "lastLoginAt": "2025-12-16T10:35:00Z"
  }
]
```

---

## 📋 Response Formats

### Success Response
```json
{
  "data": { /* response data */ },
  "success": true,
  "message": "Operation successful"
}
```

### Error Response
```json
{
  "success": false,
  "error": "Error message here",
  "statusCode": 400,
  "timestamp": "2025-12-16T10:30:00Z"
}
```

---

## ⚠️ Error Handling

### HTTP Status Codes

| Code | Meaning | Example |
|------|---------|---------|
| 200 | OK | Request successful |
| 201 | Created | Resource created |
| 400 | Bad Request | Invalid input |
| 401 | Unauthorized | Missing/invalid token |
| 403 | Forbidden | Access denied |
| 409 | Conflict | User already exists |
| 429 | Too Many Requests | Rate limited |
| 500 | Server Error | Internal error |

### Common Error Responses

**Invalid Credentials:**
```json
{
  "success": false,
  "error": "Invalid username or password",
  "statusCode": 401
}
```

**User Already Exists:**
```json
{
  "success": false,
  "error": "User with this email already exists",
  "statusCode": 409
}
```

**Validation Error:**
```json
{
  "success": false,
  "error": "Username must be at least 3 characters",
  "statusCode": 400
}
```

**Account Locked:**
```json
{
  "success": false,
  "error": "Account locked due to too many failed login attempts",
  "statusCode": 403
}
```

---

## 💡 Examples

### Complete User Registration & Login Flow

```bash
# 1. Register user
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "alice",
    "email": "alice@example.com",
    "password": "Secure123!@#",
    "firstName": "Alice",
    "lastName": "Smith"
  }'

# 2. Login
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "alice",
    "password": "Secure123!@#"
  }'

# Response includes accessToken and refreshToken
# Save these tokens!
```

### Using Access Token

```bash
curl -X GET http://localhost:5000/api/v1/admin/users \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

### Refresh Expired Token

```bash
curl -X POST http://localhost:5000/api/v1/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN"
  }'
```

### Create OAuth2 Client

```bash
curl -X POST http://localhost:5000/api/v1/admin/clients \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Mobile App",
    "description": "iOS and Android app",
    "clientType": "Public",
    "redirectUris": ["myapp://callback"],
    "allowedOrigins": []
  }'
```

### Create Scopes

```bash
curl -X POST http://localhost:5000/api/v1/admin/scopes \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "api:transactions:read",
    "displayName": "View Transactions",
    "description": "Read-only access to transaction history"
  }'
```

### View Audit Logs

```bash
curl -X GET "http://localhost:5000/api/v1/admin/audit-logs?pageSize=50" \
  -H "Authorization: Bearer ADMIN_TOKEN"
```

---

## 🔑 Token Structure

Access tokens are JWT tokens signed with RSA-256.

**Token Claims:**
```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "username": "john_doe",
  "email": "john@example.com",
  "scope": ["api:users:read", "api:users:write"],
  "iat": 1702733400,
  "exp": 1702737000
}
```

---

**Version**: 1.0.0  
**Last Updated**: December 16, 2025
