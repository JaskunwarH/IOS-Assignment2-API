# StockWise API - Azure Deployment Guide

Your StockWise API has been successfully deployed to Azure! 🎉

## 🌐 Deployed Application

**App Service URL:** https://stockwise-api-dev-czyqpwdtyduyq.azurewebsites.net

**API Endpoint:** `https://stockwise-api-dev-czyqpwdtyduyq.azurewebsites.net/api/stock/summary?symbol=AAPL`

**Azure Portal:** [View Resource Group](https://portal.azure.com/#@/resource/subscriptions/ec9219ed-b70d-4f11-be63-de5117151822/resourceGroups/rg-stockwise-api)

## ⚙️ Configuration Required

Your API needs API keys to function properly. Run the configuration script:

```bash
./configure-app-settings.sh
```

This script will prompt you for:

1. **AlphaVantage API Key** (Required)
   - Get a free key at: https://www.alphavantage.co/support/#api-key
   - Used to fetch real-time stock data

2. **GitHub Token** (Optional)
   - Used for AI-powered stock summaries
   - If not provided, the API will use basic text summaries

## 🚀 Deployment Files

The deployment created the following files:

- `infra/main.bicep` - Azure infrastructure definition
- `infra/main.parameters.json` - Infrastructure parameters
- `deploy.sh` - Full deployment script (infrastructure + code)
- `configure-app-settings.sh` - Configure API keys after deployment

## 📦 Azure Resources Created

- **Resource Group:** `rg-stockwise-api` (East US)
- **App Service Plan:** `asp-stockwise-dev` (F1 - Free tier)
- **App Service:** `stockwise-api-dev-czyqpwdtyduyq`

## 🔧 Manual Configuration (Alternative)

If you prefer to configure settings manually via Azure Portal:

1. Go to the [Azure Portal](https://portal.azure.com)
2. Navigate to your App Service: `stockwise-api-dev-czyqpwdtyduyq`
3. Click on **Configuration** → **Application settings**
4. Add these settings:
   - `AlphaVantageKey`: Your AlphaVantage API key
   - `GitHubToken`: Your GitHub token (optional)
5. Click **Save** and restart the app

Or use Azure CLI:

```bash
# Set AlphaVantage Key
az webapp config appsettings set \
  --resource-group rg-stockwise-api \
  --name stockwise-api-dev-czyqpwdtyduyq \
  --settings AlphaVantageKey="YOUR_KEY_HERE"

# Set GitHub Token (optional)
az webapp config appsettings set \
  --resource-group rg-stockwise-api \
  --name stockwise-api-dev-czyqpwdtyduyq \
  --settings GitHubToken="YOUR_TOKEN_HERE"

# Restart the app
az webapp restart \
  --resource-group rg-stockwise-api \
  --name stockwise-api-dev-czyqpwdtyduyq
```

## 🧪 Testing the API

Once configured, test your API:

```bash
# Test with AAPL
curl "https://stockwise-api-dev-czyqpwdtyduyq.azurewebsites.net/api/stock/summary?symbol=AAPL"

# Test with other symbols
curl "https://stockwise-api-dev-czyqpwdtyduyq.azurewebsites.net/api/stock/summary?symbol=MSFT"
curl "https://stockwise-api-dev-czyqpwdtyduyq.azurewebsites.net/api/stock/summary?symbol=GOOGL"
```

Expected response:
```json
{
  "Symbol": "AAPL",
  "Price": "228.52",
  "Change": "-0.02",
  "PercentChange": "-0.0087%",
  "aiSummary": "Stock AAPL is currently priced at $228.52, with a change of -0.02 (-0.0087%)."
}
```

## 📊 Monitoring & Logs

View live logs:
```bash
az webapp log tail \
  --name stockwise-api-dev-czyqpwdtyduyq \
  --resource-group rg-stockwise-api
```

View in Azure Portal:
1. Go to your App Service in the portal
2. Click on **Log stream** for live logs
3. Click on **Monitoring** → **Metrics** for performance data

## 🔄 Redeployment

To redeploy after making code changes:

```bash
# Full redeployment (infrastructure + code)
./deploy.sh

# Code only (faster)
dotnet publish StockWiseAPI.csproj -c Release -o ./publish
cd publish && zip -r ../app.zip . && cd ..
az webapp deploy \
  --resource-group rg-stockwise-api \
  --name stockwise-api-dev-czyqpwdtyduyq \
  --src-path app.zip \
  --type zip
rm -rf publish app.zip
```

## 📱 Connecting Your iOS App

Update your iOS app to use the deployed URL:

```swift
let apiBaseURL = "https://stockwise-api-dev-czyqpwdtyduyq.azurewebsites.net"
```

The API has CORS enabled to accept requests from any origin.

## 💰 Cost

Your current deployment uses:
- **App Service Plan:** F1 (Free tier) - $0/month
- **App Service:** Included in free plan

**Note:** The free tier has limitations:
- 60 minutes of CPU time per day
- 1 GB storage
- No custom domains
- No auto-scaling

To upgrade to a paid tier with better performance, update `infra/main.parameters.json`:

```json
{
  "appServicePlanSku": {
    "value": "B1"  // Basic tier ~$13/month
  }
}
```

Then redeploy with `./deploy.sh`.

## 🗑️ Cleanup

To delete all Azure resources:

```bash
az group delete --name rg-stockwise-api --yes --no-wait
```

## 🔒 Security Notes

- HTTPS is enforced (HTTP redirects to HTTPS)
- TLS 1.2 minimum
- FTP is disabled
- API keys are stored in App Service configuration (encrypted at rest)
- Consider using Azure Key Vault for production deployments

## 📚 Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [AlphaVantage API Documentation](https://www.alphavantage.co/documentation/)
- [Azure Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/)
