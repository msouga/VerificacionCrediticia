using 'main.bicep'

param location = 'westus'
param envSuffix = 'dev'

// Estos valores se pasan por linea de comandos o se editan aqui antes del deploy.
// NUNCA commitear secretos reales.
param sqlConnectionString = ''
param storageConnectionString = ''
param contentUnderstandingApiKey = ''
param equifaxClientId = ''
param equifaxClientSecret = ''
param equifaxBillTo = '011549B001'
param equifaxShipTo = '011549B001S0001'
