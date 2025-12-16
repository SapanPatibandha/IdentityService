# Installation Guide - Identity Service

Complete guide to install and deploy the Identity Service on various platforms.

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Local Development Setup](#local-development-setup)
3. [Docker Installation](#docker-installation)
4. [Production Deployment](#production-deployment)
5. [Configuration](#configuration)
6. [Testing](#testing)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### For Local Development
- **.NET 10 SDK** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com)
- **PostgreSQL 12+** - Download from [postgresql.org](https://www.postgresql.org)
- **Git** - For version control
- **Visual Studio Code** or **Visual Studio 2022** (optional but recommended)

### For Docker Deployment
- **Docker Desktop** - Download from [docker.com](https://www.docker.com/products/docker-desktop)
- **Docker Compose** - Included with Docker Desktop

### For Production (Cloud)
- AWS, Azure, or Google Cloud account (if deploying to cloud)
- Domain name (for HTTPS)
- SSL/TLS certificate

---

## Local Development Setup

### Step 1: Clone Repository
```bash
git clone https://github.com/yourusername/IdentityService.git
cd IdentityService
```

### Step 2: Install PostgreSQL

#### Windows
1. Download installer from [postgresql.org](https://www.postgresql.org/download/windows/)
2. Run installer and follow setup wizard
3. Remember the postgres password you set
4. Default port: 5432

#### macOS
```bash
brew install postgresql@15
brew services start postgresql@15
```

#### Linux (Ubuntu/Debian)
```bash
sudo apt-get update
sudo apt-get install postgresql postgresql-contrib
sudo service postgresql start
```

### Step 3: Create Database
```bash
# Connect to PostgreSQL
psql -U postgres

# In psql terminal:
CREATE DATABASE identity_service;
\q
```

### Step 4: Configure Connection String

Edit `src/IdentityService.Api/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=identity_service;Username=postgres;Password=YOUR_POSTGRES_PASSWORD"
  },
  "Jwt": {
    "Issuer": "identityservice",
    "Audience": "identityservice-api",
    "ExpiryMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Step 5: Restore NuGet Packages
```bash
dotnet restore
```

### Step 6: Apply Database Migrations
```bash
cd src/IdentityService.Api
dotnet ef database update --project ../IdentityService.Infrastructure
```

### Step 7: Run the Service
```bash
dotnet run
```

The API will start at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### Step 8: Verify Installation
```bash
curl https://localhost:5001/openapi/v1.json
```

---

## Docker Installation

### Quick Start (Recommended)

```bash
# Clone repository
git clone https://github.com/yourusername/IdentityService.git
cd IdentityService

# Start services
docker-compose up -d

# Check logs
docker-compose logs -f identityservice

# Verify service
curl http://localhost:8080/health
```

Services will be available at:
- **API**: http://localhost:8080
- **PostgreSQL**: localhost:5432

### Build Custom Docker Image

```bash
# Build image
docker build -t identity-service:latest .

# Run container
docker run -d \
  -p 8080:80 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=identity_service;..." \
  identity-service:latest
```

### Docker Compose with Custom Settings

Create `docker-compose.override.yml`:
```yaml
version: '3.9'
services:
  identityservice:
    environment:
      Jwt__ExpiryMinutes: "120"
      # Add more custom env vars
```

---

## Production Deployment

### AWS Deployment (ECS + RDS)

#### 1. Create RDS PostgreSQL Instance
```bash
aws rds create-db-instance \
  --db-instance-identifier identity-service-db \
  --db-instance-class db.t3.micro \
  --engine postgres \
  --master-username postgres \
  --allocated-storage 20
```

#### 2. Push Docker Image to ECR
```bash
# Create ECR repository
aws ecr create-repository --repository-name identity-service

# Tag and push image
docker tag identity-service:latest YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/identity-service:latest
docker push YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/identity-service:latest
```

#### 3. Create ECS Cluster & Task Definition
```bash
# Create cluster
aws ecs create-cluster --cluster-name identity-service

# Create task definition (JSON)
aws ecs register-task-definition --cli-input-json file://task-definition.json

# Run task
aws ecs run-task --cluster identity-service --task-definition identity-service:1
```

### Azure Deployment (App Service + Azure Database)

#### 1. Create Azure Database for PostgreSQL
```bash
az postgres server create \
  --resource-group identity-service \
  --name identity-service-db \
  --admin-user dbadmin \
  --admin-password YourSecurePassword123!
```

#### 2. Push to Azure Container Registry
```bash
# Create registry
az acr create --resource-group identity-service --name identityserviceacr --sku Basic

# Push image
az acr build --registry identityserviceacr --image identity-service:latest .
```

#### 3. Deploy App Service
```bash
# Create App Service plan
az appservice plan create --name identity-service-plan --resource-group identity-service --sku B2

# Create web app
az webapp create --resource-group identity-service --plan identity-service-plan --name identity-service-app --deployment-container-image-name identityserviceacr.azurecr.io/identity-service:latest
```

### Google Cloud Deployment (Cloud Run)

```bash
# Build and push image
gcloud builds submit --tag gcr.io/PROJECT_ID/identity-service

# Deploy to Cloud Run
gcloud run deploy identity-service \
  --image gcr.io/PROJECT_ID/identity-service:latest \
  --platform managed \
  --region us-central1 \
  --set-env-vars ConnectionStrings__DefaultConnection="..."
```

---

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=identity_service;Username=postgres;Password=xxx

# JWT Settings
Jwt__Issuer=identityservice
Jwt__Audience=identityservice-api
Jwt__ExpiryMinutes=60

# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80;https://+:443

# Optional: Email Service
EmailService__Provider=sendgrid
EmailService__ApiKey=xxx

# Optional: Logging
Serilog__MinimumLevel=Information
```

### Securing Configuration

#### Using .env file (Development Only)
```bash
cp .env.example .env
# Edit .env with your values
docker-compose --env-file .env up
```

#### Using AWS Secrets Manager (Production)
```bash
aws secretsmanager create-secret \
  --name identity-service-db \
  --secret-string '{"username":"postgres","password":"xxx"}'
```

#### Using Azure Key Vault (Production)
```bash
az keyvault secret set --vault-name identity-service-kv \
  --name DbPassword --value "xxx"
```

---

## Testing

### Unit Tests
```bash
dotnet test
```

### Integration Tests
```bash
dotnet test --filter Category=Integration
```

### API Testing with Postman
1. Import `postman-collection.json` in Postman
2. Set variables:
   - `base_url`: http://localhost:8080
   - `token`: (generated after login)
3. Run requests

### Manual API Testing
```bash
# Register user
curl -X POST http://localhost:8080/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"test@example.com","password":"SecurePass123!"}'

# Login
curl -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"SecurePass123!"}'
```

---

## Database Migrations

### Create Migration
```bash
cd src/IdentityService.Api
dotnet ef migrations add MigrationName --project ../IdentityService.Infrastructure
```

### Apply Migrations
```bash
dotnet ef database update --project ../IdentityService.Infrastructure
```

### Rollback Migration
```bash
dotnet ef database update PreviousMigrationName --project ../IdentityService.Infrastructure
```

### Remove Latest Migration
```bash
dotnet ef migrations remove --project ../IdentityService.Infrastructure
```

---

## Troubleshooting

### Port Already in Use
```bash
# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F

# macOS/Linux
lsof -i :8080
kill -9 <PID>
```

### Database Connection Failed
```bash
# Test PostgreSQL connection
psql -h localhost -U postgres -d identity_service

# Verify connection string
echo $ConnectionStrings__DefaultConnection
```

### Docker Issues
```bash
# Check service logs
docker-compose logs identityservice

# Restart services
docker-compose restart

# Clean up
docker-compose down -v
docker system prune -a
```

### JWT Token Errors
- Verify token hasn't expired
- Check issuer/audience in appsettings.json
- Ensure RSA keys are properly generated

### 2FA Not Working
- Verify email service is configured
- Check TOTP secret format (Base32)
- Test authenticator app compatibility

---

## Health Checks

### Service Health
```bash
curl http://localhost:8080/health
```

### Database Health
```bash
curl http://localhost:8080/health/db
```

### JWT Validation
```bash
curl -H "Authorization: Bearer <TOKEN>" http://localhost:8080/api/v1/auth/verify
```

---

## Monitoring & Logs

### View Logs Locally
```bash
tail -f logs/identityservice-*.txt
```

### Docker Logs
```bash
docker-compose logs -f identityservice
```

### Setup Log Aggregation (ELK Stack)
```yaml
# Add to docker-compose.yml
elasticsearch:
  image: docker.elastic.co/elasticsearch/elasticsearch:8.0.0

kibana:
  image: docker.elastic.co/kibana/kibana:8.0.0
  depends_on:
    - elasticsearch
```

---

## Scaling

### Horizontal Scaling (Multiple Instances)
```bash
docker-compose up -d --scale identityservice=3
```

### Load Balancing (Nginx)
```nginx
upstream identity_service {
  server identityservice:80;
  server identityservice-2:80;
  server identityservice-3:80;
}

server {
  listen 8080;
  location / {
    proxy_pass http://identity_service;
  }
}
```

---

## SSL/TLS Certificates

### Self-Signed (Development)
```bash
dotnet dev-certs https --trust
```

### Let's Encrypt (Production)
```bash
# Using Certbot
certbot certonly --standalone -d your-domain.com

# Use certificate in docker-compose
volumes:
  - /etc/letsencrypt/live/your-domain.com:/app/certs:ro
```

---

## Backup & Recovery

### PostgreSQL Backup
```bash
docker-compose exec postgres pg_dump -U postgres identity_service > backup.sql
```

### PostgreSQL Restore
```bash
docker-compose exec -T postgres psql -U postgres identity_service < backup.sql
```

---

## Performance Tuning

### Database Optimization
```sql
-- Create indexes
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_refresh_tokens_token ON refresh_tokens(token);
CREATE INDEX idx_audit_logs_created_at ON audit_logs(created_at);
```

### Connection Pooling
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=identity_service;...;Maximum Pool Size=20;"
  }
}
```

---

## Next Steps

1. ✅ Install service
2. ✅ Configure database
3. ✅ Run migrations
4. ⬜ Create first OAuth2 client
5. ⬜ Register test user
6. ⬜ Test authentication flows
7. ⬜ Integrate with your application
8. ⬜ Set up monitoring

---

## Support

For issues during installation:
- Check [Troubleshooting](#troubleshooting) section
- Review logs in `logs/` directory
- Open GitHub issue with logs attached

---

**Last Updated**: December 2025
