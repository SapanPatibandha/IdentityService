# Identity Service Postman Collection

This folder contains Postman collection files for testing the Identity Service API.

## How to Import

1. Open Postman
2. Click **Import** button (top left)
3. Select **File** tab
4. Choose the `.postman_collection.json` file from this folder
5. Click **Import**

## How to Export

To export your Postman collection:

1. In Postman, click the **...** (three dots) next to your collection name
2. Select **Export**
3. Choose **Collection v2.1** format
4. Save the file in this folder

## Environment Variables

If you're using environment variables in Postman, create a corresponding `.postman_environment.json` file in this folder.

### Common Variables

- `base_url` - API base URL (e.g., `http://localhost:8080`)
- `api_version` - API version (e.g., `v1`)
- `token` - JWT token for authenticated requests
- `client_id` - OAuth2 client ID

## API Endpoints

The collection should cover the following endpoints:

### Authentication
- `POST /api/v1/auth/register` - Register new user
- `POST /api/v1/auth/login` - Login user
- `POST /api/v1/auth/refresh` - Refresh access token
- `POST /api/v1/auth/logout` - Logout user
- `POST /api/v1/auth/request-2fa` - Request 2FA verification
- `POST /api/v1/auth/verify-2fa` - Verify 2FA code

### Admin Operations
- `GET /api/v1/admin/users` - List all users
- `GET /api/v1/admin/users/{id}` - Get user details
- `GET /api/v1/admin/clients` - List all clients
- `POST /api/v1/admin/clients` - Create new client
- `GET /api/v1/admin/scopes` - List all scopes
- `POST /api/v1/admin/scopes` - Create new scope
- `GET /api/v1/admin/audit-logs` - List audit logs

## Testing with Docker

When testing with Docker deployment:
- Set `base_url` to `http://localhost:8080`
- Ensure containers are running: `docker-compose up -d`

## Testing Local Development

When testing local development:
- Set `base_url` to `http://localhost:5238`
- Run the API: `dotnet run` in `src/IdentityService.Api`
