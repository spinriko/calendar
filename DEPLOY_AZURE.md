# Deploy to Azure App Service

## Prerequisites
- Azure CLI installed: `winget install Microsoft.AzureCLI`
- Azure account with active subscription

## Quick Deployment Steps

### 1. Login to Azure
```powershell
az login
```

### 2. Create Resource Group
```powershell
az group create --name pto-track-rg --location eastus
```

### 3. Create App Service Plan (Free Tier)
```powershell
az appservice plan create --name pto-track-plan --resource-group pto-track-rg --sku F1 --is-linux
```

### 4. Create Web App
```powershell
az webapp create --name pto-track-app --resource-group pto-track-rg --plan pto-track-plan --runtime "DOTNET|9.0"
```

### 5. Deploy SQL Server Container to Render.com (Free)

#### A. Create Render.com Account
1. Go to https://render.com and sign up (free)
2. Verify your email

#### B. Deploy SQL Server Container
1. Go to Render Dashboard → New → Web Service
2. Choose "Deploy an existing image from a registry"
3. Image URL: `mcr.microsoft.com/mssql/server:2022-latest`
4. Configure:
   - **Name**: `pto-track-sql`
   - **Region**: Choose closest to your Azure region
   - **Instance Type**: Free
   - **Environment Variables**:
     - `ACCEPT_EULA=Y`
     - `SA_PASSWORD=YourSecurePassword123!` (use a strong password)
     - `MSSQL_PID=Developer`
   - **Port**: 1433
5. Click "Create Web Service"
6. Wait for deployment (5-10 minutes)
7. Copy the external URL (e.g., `pto-track-sql.onrender.com`)

**Important**: Render free tier limitations:
- Service spins down after 15 min of inactivity
- First request after spin-down takes 30-60 seconds
- 750 hours/month free usage

#### C. Get Connection String
Your SQL Server will be available at:
```
Server=pto-track-sql.onrender.com,1433;Database=PtoTrack;User ID=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;
```

### 6. Configure Azure App Service Connection String
```powershell
$connString = "Server=pto-track-sql.onrender.com,1433;Database=PtoTrack;User ID=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;"

az webapp config connection-string set --name pto-track-app --resource-group pto-track-rg --connection-string-type SQLServer --settings PtoTrackDbContext="$connString"
```

### 7. Deploy Application
```powershell
# From project root
dotnet publish ./pto.track/pto.track.csproj -c Release -o ./publish

# Create zip
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip -Force

# Deploy
az webapp deployment source config-zip --name pto-track-app --resource-group pto-track-rg --src ./deploy.zip
```

### 8. Set Environment
```powershell
az webapp config appsettings set --name pto-track-app --resource-group pto-track-rg --settings ASPNETCORE_ENVIRONMENT=Production
```

## Access Your App
```
https://pto-track-app.azurewebsites.net
```

## Clean Up (When Done)
```powershell
az group delete --name pto-track-rg --yes
```

## Notes
- Replace `pto-track-app` with a unique name (must be globally unique)
- Replace SQL password with a secure password in both Render and Azure
- **F1 (Free) tier limitations**: 60 min/day compute time, 1GB disk, 1GB RAM
- **Render free tier**: SQL Server spins down after 15 min inactivity, 750 hrs/month
- Database migrations run automatically on first start
- Check logs: `az webapp log tail --name pto-track-app --resource-group pto-track-rg`
- **Warning**: First request after SQL Server spin-down takes 30-60 seconds
- Both services are free but have usage limitations

## Alternative: SQLite (Simpler, No Render Needed)
If cold starts and spin-down delays are problematic, consider using SQLite instead:
1. Add package: `dotnet add ./pto.track/pto.track.csproj package Microsoft.EntityFrameworkCore.Sqlite`
2. Update `ServiceCollectionExtensions.cs` to use `UseSqlite` instead of `UseSqlServer`
3. Skip Render.com setup entirely
