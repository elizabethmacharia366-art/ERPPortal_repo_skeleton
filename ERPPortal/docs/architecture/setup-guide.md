# ERPPortal — Local Development Setup Guide

Follow these steps in order to get the full stack running from a clean machine.
This reflects the exact setup built during Phase 1.

## Prerequisites

- .NET 8 SDK (`dotnet --version` should show `8.0.x`)
- Node.js and npm
- Docker
- `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef --version 8.0.10`

If .NET was installed via snap, you may need these in your shell profile
(`~/.bashrc` and `~/.bash_profile`) for tools to resolve correctly:
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
export DOTNET_ROOT=/snap/dotnet-sdk/<version-folder>   # check with: dotnet --list-sdks
```

## 1. Start infrastructure containers

```bash
# SQL Server
docker run -e "ACCEPT_EULA=Y" -e MSSQL_SA_PASSWORD='YourStrong!Passw0rd' \
  -p 1433:1433 --name erpportal-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest

# Keycloak (with a persistent volume — required, or all realm/user data
# resets every time the container restarts)
docker run -d --name erpportal-keycloak -p 8080:8080 \
  -e KEYCLOAK_ADMIN=admin -e KEYCLOAK_ADMIN_PASSWORD=admin \
  -v erpportal-keycloak-data:/opt/keycloak/data \
  quay.io/keycloak/keycloak:24.0 start-dev
```

SQL Server takes ~10-20 seconds to accept connections after starting.
Keycloak takes ~30-40 seconds on a cold start. Check status with `docker ps`.

## 2. Configure Keycloak (one-time, or after a volume reset)

Using the admin API is faster and more reliable than the web UI:

```bash
ADMIN_TOKEN=$(curl -s -X POST http://localhost:8080/realms/master/protocol/openid-connect/token \
  -d "client_id=admin-cli" -d "username=admin" -d "password=admin" \
  -d "grant_type=password" | grep -o '"access_token":"[^"]*' | cut -d'"' -f4)

# Create realm
curl -s -X POST http://localhost:8080/admin/realms \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"realm": "erpportal", "enabled": true}'

# Create client (direct access grants enabled for local testing)
curl -s -X POST http://localhost:8080/admin/realms/erpportal/clients \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{
    "clientId": "erpportal-api", "enabled": true, "publicClient": false,
    "protocol": "openid-connect", "standardFlowEnabled": true,
    "directAccessGrantsEnabled": true,
    "rootUrl": "http://localhost:5173",
    "redirectUris": ["http://localhost:5173/*"],
    "webOrigins": ["http://localhost:5173"]
  }'

# Get the client secret (needed for appsettings.Development.json and testing)
CLIENT_UUID=$(curl -s -H "Authorization: Bearer $ADMIN_TOKEN" \
  "http://localhost:8080/admin/realms/erpportal/clients?clientId=erpportal-api" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)[0]['id'])")
curl -s -H "Authorization: Bearer $ADMIN_TOKEN" \
  "http://localhost:8080/admin/realms/erpportal/clients/$CLIENT_UUID/client-secret"

# Create a test user with firstName/lastName set (required — Keycloak 24's
# "Verify Profile" required action blocks login without these) and
# requiredActions cleared
curl -s -X POST http://localhost:8080/admin/realms/erpportal/users \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{
    "username": "testuser", "email": "testuser@erpportal.local",
    "firstName": "Test", "lastName": "User",
    "enabled": true, "emailVerified": true, "requiredActions": []
  }'

USER_UUID=$(curl -s -H "Authorization: Bearer $ADMIN_TOKEN" \
  "http://localhost:8080/admin/realms/erpportal/users?username=testuser" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)[0]['id'])")
curl -s -X PUT "http://localhost:8080/admin/realms/erpportal/users/$USER_UUID/reset-password" \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"type": "password", "value": "TestUser123!", "temporary": false}'
```

**Note:** admin tokens expire in ~60 seconds. If any step returns `401`, re-run
the `ADMIN_TOKEN=...` line and retry immediately.

## 3. Configure the API

Put the resulting client secret into `backend/ERPPortal.API/appsettings.Development.json`:
```json
"Keycloak": {
  "Authority": "http://localhost:8080/realms/erpportal",
  "Audience": "account"
}
```
(The client secret itself isn't needed by the API for token *validation* —
only for flows where the API calls Keycloak on its own behalf.)

## 4. Apply database migrations

```bash
cd backend/ERPPortal.API
dotnet ef database update --project ../ERPPortal.Infrastructure --startup-project .
```

## 5. Run the API

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

Verify:
```bash
curl http://localhost:5000/health          # expect: Healthy
curl -i http://localhost:5000/api/v1/me    # expect: 401 Unauthorized (no token)
```

## 6. Run the frontend

```bash
cd frontend
npm install
npm run dev
```
Visit `http://localhost:5173`.

## 7. Get a token and test the full auth flow

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/realms/erpportal/protocol/openid-connect/token \
  -d "client_id=erpportal-api" -d "client_secret=<your-client-secret>" \
  -d "grant_type=password" -d "username=testuser" -d "password=TestUser123!" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")

curl http://localhost:5000/api/v1/me -H "Authorization: Bearer $TOKEN"
# expect: {"username":"testuser","email":"testuser@erpportal.local"}
```

## Troubleshooting

- **`dotnet ef` not found:** see the PATH/DOTNET_ROOT exports under Prerequisites.
- **Keycloak realm/user data vanished:** the container is likely running
  without the `-v erpportal-keycloak-data:/opt/keycloak/data` volume — check
  with `docker inspect erpportal-keycloak` and recreate with the volume if missing.
- **"Account is not fully set up" on login:** check the user has no pending
  `requiredActions` (`GET /admin/realms/erpportal/users?username=...`) and has
  `firstName`/`lastName` set.
- **`NU1202` package errors:** you likely omitted `--version` on a
  `dotnet add package` command and got a version built for a newer .NET target.
  Always pin EF Core–family packages to match `net8.0`.
