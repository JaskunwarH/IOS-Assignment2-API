// Azure App Service deployment for StockWise API
// This creates an App Service Plan and App Service to host the .NET 9.0 Web API

@description('Name of the environment (e.g., dev, staging, prod)')
param environmentName string = 'dev'

@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the app service')
param appServiceName string = 'stockwise-api-${environmentName}-${uniqueString(resourceGroup().id)}'

@description('Name of the app service plan')
param appServicePlanName string = 'asp-stockwise-${environmentName}'

@description('App Service Plan SKU')
@allowed([
  'F1'  // Free tier
  'B1'  // Basic tier
  'S1'  // Standard tier
  'P1v2' // Premium V2 tier
])
param appServicePlanSku string = 'F1'

// App Service Plan - hosting infrastructure
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true  // Required for Linux
  }
  tags: {
    environment: environmentName
    application: 'StockWise API'
  }
}

// App Service - the web application
resource appService 'Microsoft.Web/sites@2024-04-01' = {
  name: appServiceName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true  // Enforce HTTPS
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'  // .NET 9.0 runtime
      alwaysOn: appServicePlanSku != 'F1'  // Always On not available in Free tier
      ftpsState: 'Disabled'  // Disable FTP for security
      minTlsVersion: '1.2'  // Minimum TLS version
      http20Enabled: true
      cors: {
        allowedOrigins: [
          '*'  // Allow all origins (matching your CORS policy)
        ]
        supportCredentials: false
      }
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
  }
  tags: {
    environment: environmentName
    application: 'StockWise API'
  }
}

// Outputs
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServiceName string = appService.name
output resourceGroupName string = resourceGroup().name
