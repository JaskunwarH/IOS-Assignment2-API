#!/bin/bash

# Deployment script for StockWise API to Azure App Service
# This script deploys the infrastructure and application code to Azure

set -e

# Configuration
RESOURCE_GROUP_NAME="rg-stockwise-api"
LOCATION="eastus"
DEPLOYMENT_NAME="stockwise-deployment-$(date +%Y%m%d-%H%M%S)"

echo "🚀 Starting deployment of StockWise API to Azure..."
echo ""

# Create resource group if it doesn't exist
echo "📦 Creating resource group: $RESOURCE_GROUP_NAME in $LOCATION..."
az group create \
  --name $RESOURCE_GROUP_NAME \
  --location $LOCATION \
  --output none

echo "✅ Resource group ready"
echo ""

# Validate the Bicep deployment
echo "🔍 Validating Bicep template..."
az deployment group validate \
  --resource-group $RESOURCE_GROUP_NAME \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json \
  --output none

echo "✅ Template validation successful"
echo ""

# Preview changes with what-if
echo "📋 Previewing deployment changes..."
az deployment group what-if \
  --resource-group $RESOURCE_GROUP_NAME \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json \
  --name $DEPLOYMENT_NAME

echo ""
read -p "❓ Do you want to proceed with the deployment? (y/n) " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Deployment cancelled"
    exit 1
fi

# Deploy infrastructure
echo "🏗️  Deploying infrastructure..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP_NAME \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json \
  --name $DEPLOYMENT_NAME \
  --output json)

APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceUrl.value')

echo "✅ Infrastructure deployed successfully"
echo "   App Service Name: $APP_SERVICE_NAME"
echo "   App Service URL: $APP_SERVICE_URL"
echo ""

# Build and publish the .NET application
echo "🔨 Building .NET application..."
dotnet publish StockWiseAPI.csproj -c Release -o ./publish

echo "✅ Build successful"
echo ""

# Deploy application code to App Service
echo "📤 Deploying application code to Azure App Service..."
cd publish
zip -r ../app.zip . > /dev/null
cd ..

az webapp deploy \
  --resource-group $RESOURCE_GROUP_NAME \
  --name $APP_SERVICE_NAME \
  --src-path app.zip \
  --type zip \
  --output none

# Clean up
rm -rf publish
rm app.zip

echo "✅ Application deployed successfully"
echo ""

# Wait for app to start
echo "⏳ Waiting for application to start..."
sleep 10

# Test the endpoint
echo "🧪 Testing the API endpoint..."
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$APP_SERVICE_URL/api/stock" || echo "000")

if [ "$HTTP_STATUS" -eq "200" ] || [ "$HTTP_STATUS" -eq "404" ]; then
    echo "✅ API is responding (HTTP $HTTP_STATUS)"
else
    echo "⚠️  API returned HTTP $HTTP_STATUS (it may need a few more moments to start)"
fi

echo ""
echo "🎉 Deployment complete!"
echo ""
echo "📍 Resource Information:"
echo "   Resource Group: $RESOURCE_GROUP_NAME"
echo "   App Service: $APP_SERVICE_NAME"
echo "   URL: $APP_SERVICE_URL"
echo ""
echo "🔗 Azure Portal:"
echo "   https://portal.azure.com/#@/resource/subscriptions/$(az account show --query id -o tsv)/resourceGroups/$RESOURCE_GROUP_NAME"
echo ""
echo "📊 View logs:"
echo "   az webapp log tail --name $APP_SERVICE_NAME --resource-group $RESOURCE_GROUP_NAME"
echo ""
