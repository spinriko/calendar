#!/usr/bin/env bash
set -euo pipefail

# Example Azure deployment script for App Service using SQLite
# Replace the variables below with your desired resource names and region.

RESOURCE_GROUP="rg-daypilot-demo"
LOCATION="eastus"
PLAN_NAME="daypilot-plan"
APP_NAME="daypilot-sqlite-
$(date +%s)"
SKU="F1" # Free tier; change to B1 for paid

# Path to the zip produced by `dotnet publish` as described in README
ZIP_PATH="./app.zip"

echo "Resource group: $RESOURCE_GROUP"
echo "Location: $LOCATION"
echo "App name: $APP_NAME"

if [ ! -f "$ZIP_PATH" ]; then
  echo "Error: $ZIP_PATH not found. Run dotnet publish and create the zip as described in README." >&2
  exit 2
fi

echo "Creating resource group..."
az group create -n "$RESOURCE_GROUP" -l "$LOCATION"

echo "Creating App Service plan ($PLAN_NAME)..."
az appservice plan create -g "$RESOURCE_GROUP" -n "$PLAN_NAME" --is-linux --sku "$SKU"

echo "Creating Web App ($APP_NAME)..."
az webapp create -g "$RESOURCE_GROUP" -p "$PLAN_NAME" -n "$APP_NAME" --runtime "DOTNET|10.0"

echo "Setting SQLite connection string (App setting: ConnectionStrings:SchedulerDbContext)
and ensuring the app uses a file inside wwwroot/App_Data..."
az webapp connection-string set -g "$RESOURCE_GROUP" -n "$APP_NAME" --settings SchedulerDbContext="Data Source=/home/site/wwwroot/App_Data/scheduler.db" --connection-string-type Custom

echo "Deploying zip to App Service..."
az webapp deploy -g "$RESOURCE_GROUP" -n "$APP_NAME" --src-path "$ZIP_PATH" --type zip

echo "Deployment complete. App URL: https://$APP_NAME.azurewebsites.net"

echo "Notes:
- The script creates an App Service with a unique name based on epoch time. Change $APP_NAME variable as needed.
- Do not scale the app out to multiple instances when using SQLite.
"
