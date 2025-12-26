# IdentityService Tests

This directory contains comprehensive unit and integration tests for the IdentityService API.

## Structure

- **Unit Tests** (`Unit/`): Tests for individual services with mocked dependencies using Moq
  - `Services/ServiceTests.cs`: Tests for TokenService, AuthenticationService, ClientService, ScopeService

- **Integration Tests** (`Integration/`): End-to-end tests using WebApplicationFactory
  - `Auth/AuthControllerTests.cs`: Tests for authentication endpoints (register, login, refresh, 2FA, etc.)
  - `Admin/AdminControllerTests.cs`: Tests for admin endpoints (clients, scopes, users, audit logs)

- **Fixtures** (`Fixtures/`): Test utilities and helpers
  - `IdentityServiceWebApplicationFactory.cs`: Factory for creating test application instance
  - `IntegrationTestBase.cs`: Base class for integration tests with helper methods
  - `TestDataBuilder.cs`: Builder for creating test data (users, clients, scopes, etc.)

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run specific test class
```bash
dotnet test --filter FullyQualifiedName~AuthControllerTests
```

### Run with coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Run with verbose output
```bash
dotnet test --verbosity detailed
```

## Test Coverage

The test suite covers:

### Authentication Endpoints
- ✅ User registration (valid/invalid data, duplicates)
- ✅ Login (valid credentials, invalid password, nonexistent user)
- ✅ Token refresh (valid/expired/invalid tokens)
- ✅ Logout (with/without token)
- ✅ Token revocation
- ✅ 2FA request and verification
- ✅ TOTP setup and verification

### Admin Endpoints
- ✅ Client management (create, read, list)
- ✅ Scope management (create, read, list)
- ✅ User management (read, list)
- ✅ Audit log retrieval (with pagination)
- ✅ Authorization enforcement

### Service Layer
- ✅ TokenService: Token generation and validation
- ✅ AuthenticationService: User registration, login, password validation
- ✅ ClientService: Client CRUD operations and validation
- ✅ ScopeService: Scope CRUD operations

## Key Features

### WebApplicationFactory
- Creates in-memory database for testing
- Isolates each test with unique database instance
- Configures identical services as production

### Test Helpers
- `GenerateAccessToken()`: Creates valid JWT tokens for testing
- `AddAuthorizationHeader()`: Adds authorization to HTTP requests
- `ClearDatabaseAsync()`: Resets database state between tests
- `TestDataBuilder`: Creates test entities (users, clients, scopes, tokens)

### Edge Cases Covered
- Missing or invalid request fields
- Duplicate usernames/emails
- Expired refresh tokens
- Unauthorized access (missing auth headers)
- Invalid credentials
- Database constraints

## CI/CD Integration

Tests run automatically on:
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`

See `.github/workflows/tests.yml` for CI/CD configuration.

## Dependencies

- **xUnit**: Test framework
- **Moq**: Mocking library for unit tests
- **Microsoft.AspNetCore.Mvc.Testing**: WebApplicationFactory for integration tests
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database for testing
- **coverlet**: Code coverage measurement

## Best Practices

1. **Test Isolation**: Each test is independent and doesn't rely on others
2. **Arrange-Act-Assert**: Tests follow AAA pattern
3. **Descriptive Names**: Test names clearly describe what is being tested
4. **Database Cleanup**: Integration tests clean up database state
5. **Mock External Dependencies**: Unit tests mock repositories and services
6. **Positive and Negative Cases**: Tests cover both success and error scenarios

## Adding New Tests

When adding new endpoints or services:

1. Create unit tests with mocked dependencies
2. Create integration tests using WebApplicationFactory
3. Test both positive and negative cases
4. Update this README with new coverage information
5. Ensure CI/CD passes before merging

## Troubleshooting

### Tests fail with "database already exists"
Solution: Delete any existing test databases in the temp directory

### Port conflicts in integration tests
Solution: WebApplicationFactory automatically assigns random ports

### Authentication failures in integration tests
Solution: Use `AddAuthorizationHeader()` to add valid JWT tokens

### Timeout errors
Solution: Increase timeout in WebApplicationFactory if needed

## Performance

- Unit tests: ~2-3 seconds total
- Integration tests: ~5-8 seconds total
- Full test suite: ~10-15 seconds
