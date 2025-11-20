#!/bin/bash

# Script to configure Azure App Service settings for StockWise API
# This sets up the required API keys and configuration values

set -e

RESOURCE_GROUP="rg-stockwise-api"
APP_NAME="stockwise-api-dev-czyqpwdtyduyq"

echo "🔧 Configuring Azure App Service settings for StockWise API"
echo ""

# Prompt for AlphaVantage API Key
read -p "Enter your AlphaVantage API Key (get one free at https://www.alphavantage.co/support/#api-key): " ALPHA_VANTAGE_KEY

# Prompt for GitHub Token (optional)
read -p "Enter your GitHub Token for AI features (optional, press Enter to skip): " GITHUB_TOKEN

echo ""
echo "📝 Updating App Service configuration..."

# Set AlphaVantage Key
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --settings AlphaVantageKey="$ALPHA_VANTAGE_KEY" \
  --output none

echo "✅ AlphaVantageKey configured"

# Set GitHub Token if provided
if [ ! -z "$GITHUB_TOKEN" ]; then
  az webapp config appsettings set \
    --resource-group $RESOURCE_GROUP \
    --name $APP_NAME \
    --settings GitHubToken="$GITHUB_TOKEN" \
    --output none
  echo "✅ GitHubToken configured"
else
  echo "ℹ️  GitHubToken skipped (AI summaries will use fallback text)"
fi

echo ""
echo "🔄 Restarting App Service to apply settings..."
az webapp restart \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --output none

echo "✅ App Service restarted"
echo ""
echo "⏳ Waiting for application to start..."
sleep 10

echo ""
echo "🧪 Testing the API..."
RESPONSE=$(curl -s "https://$APP_NAME.azurewebsites.net/api/stock/summary?symbol=AAPL")

if echo "$RESPONSE" | grep -q "Symbol"; then
  echo "✅ API is working correctly!"
  echo ""
  echo "📊 Sample response:"
  echo "$RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$RESPONSE"
else
  echo "⚠️  API returned: $RESPONSE"
  echo ""
  echo "Check the logs with:"
  echo "az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP"
fi

echo ""
echo "🎉 Configuration complete!"
echo ""
echo "Your API is available at:"
echo "https://$APP_NAME.azurewebsites.net/api/stock/summary?symbol=AAPL"
echo ""
