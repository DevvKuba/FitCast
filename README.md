# ClientDashboard Application
FitCast is a predictive fitness and business intelligence platform that unifies training data, client management, and financial insights for personal trainers while supporting their clients
The platform was built with .NET 9 and Angular 19.

## Deployed Environment
https://fitcast.uk/

## Local Development Setup

### Prerequisites
- .NET 9 SDK
- Docker Desktop
- SQL Server (via Docker)
- Node.js 18+ and npm (for Angular frontend)
- Twilio account (for SMS notifications)
- Hevy API access (for workout synchronization)

### Configuration Steps

#### 1. Copy Example Configuration Files

```powershell
# Backend configuration
Copy-Item ClientDashboard_API/appsettings.Development.json.example ClientDashboard_API/appsettings.Development.json
Copy-Item ClientDashboard_API/Properties/launchSettings.json.example ClientDashboard_API/Properties/launchSettings.json
Copy-Item ProjectTests/ClientDashboard_API_Tests/Properties/launchSettings.json.example ProjectTests/ClientDashboard_API_Tests/Properties/launchSettings.json

# Docker configuration
Copy-Item docker-compose.yml.example docker-compose.yml
```

#### 2. Generate Required Secrets

**API Encryption Key (32 bytes):**
```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)
Write-Output "API Encryption Key: $key"
```

**JWT Secret (32 bytes minimum):**
```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)
Write-Output "JWT Secret: $secret"
```

#### 3. Update Configuration Files

Edit the following files with your credentials:

- **`appsettings.Development.json`**
  - Database connection string password
  - JWT Secret, Issuer, and Audience
  - API Encryption Key
  - Email configuration

- **`launchSettings.json`** (both API and Tests)
  - Twilio `AUTH_TOKEN`
  - Twilio `ACCOUNT_SID`
  - `SENDER_PHONE_NUMBER`
  - Hevy `API_KEY` (if using workout sync)

- **`docker-compose.yml`**
  - SQL Server `MSSQL_SA_PASSWORD`

#### 4. Start Docker Containers

```powershell
docker-compose up -d
```

Wait ~10 seconds for SQL Server to fully initialize.

#### 5. Run Database Migrations

```powershell
cd ClientDashboard_API
dotnet ef database update
```

#### 6. Run the Backend API

```powershell
cd ClientDashboard_API
dotnet run
```

The API will be available at: `https://localhost:7217`

#### 7. Run the Frontend (Angular)

```powershell
cd ClientDashboard_GUI
npm install
npm start
```

The frontend will be available at: `http://localhost:4200`

## Environment Variables Required

### Twilio (SMS Notifications)
- `ACCOUNT_SID` - Your Twilio Account SID
- `AUTH_TOKEN` - Your Twilio Auth Token
- `SENDER_PHONE_NUMBER` - Your Twilio phone number (E.164 format: +1234567890)

### Hevy API (Workout Sync)
- `API_KEY` - Your Hevy API key
- `API_URL` - Hevy API endpoint URL

## Security Notes

- **Never commit actual credentials to version control!**
- All sensitive configuration files are gitignored
- Use the `.example` files as templates
- For production, use Azure App Service configuration or Azure Key Vault

## Running Tests

```powershell
cd ProjectTests/ClientDashboard_API_Tests
dotnet test
```