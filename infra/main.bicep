// The Kundenportal on Azure (README, "Where it runs"): one resource group per azd environment, holding everything the
// AppHost runs locally. infra/README.md says what to run and which values to set.
targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('The azd environment name; it names the resource group and seeds every resource name.')
param environmentName string

@minLength(1)
@description('The region for every resource but the model (openAiLocation).')
param location string

@description('The region of the Azure OpenAI resource: one that offers both model deployments below.')
param openAiLocation string = location

@description('The signed-in user azd runs as (AZURE_PRINCIPAL_ID): may read Key Vault secrets, for the realm setup.')
param principalId string = ''

@description('The tenants staff may write for, comma-separated (Tenancy:Tenants); each needs the group /tenants/<tenant>.')
param tenants string = 'demo'

@minLength(1)
@description('The SMTP server the notifications module sends through: smtps://host:port (TLS) or smtp://host:port (STARTTLS).')
param smtpConnection string

@minLength(1)
@description('The sender of the notifications module\'s e-mail (Notifications:SenderAddress).')
param senderAddress string

@description('The user the host signs in to the SMTP server as (AppHost parameter smtp-user-name); empty for a relay that needs no sign-in.')
param smtpUserName string = ''

@secure()
@description('That user\'s password (AppHost parameter smtp-password), kept in Key Vault; empty without a user name.')
param smtpPassword string = ''

@description('The chat deployment (AppHost parameter azure-openai-deployment).')
param chatDeploymentName string = 'gpt-5-mini'

@description('The chat model\'s version.')
param chatModelVersion string = '2025-08-07'

@description('The chat deployment\'s SKU.')
param chatDeploymentSku string = 'GlobalStandard'

@description('The chat deployment\'s capacity, in thousands of tokens per minute.')
param chatDeploymentCapacity int = 50

@description('The embedding deployment (AppHost parameter azure-openai-embedding-deployment). Its model fixes the vector size: 1536.')
param embeddingDeploymentName string = 'text-embedding-3-small'

@description('The embedding model\'s version.')
param embeddingModelVersion string = '1'

@description('The embedding deployment\'s SKU.')
param embeddingDeploymentSku string = 'GlobalStandard'

@description('The embedding deployment\'s capacity, in thousands of tokens per minute.')
param embeddingDeploymentCapacity int = 50

@description('The PostgreSQL major version.')
param postgresVersion string = '17'

@description('The image azd last deployed for the host; a placeholder until the first deploy.')
param hostImageName string = ''

@description('The image azd last deployed for the migrations job; a placeholder until the first deploy.')
param migrationsImageName string = ''

@description('The image azd last deployed for the web app; a placeholder until the first deploy.')
param webImageName string = ''

// Generated once by azd and kept in the environment's config (never in the repository); each lands in Key Vault.
@secure()
@metadata({ azd: { type: 'generate', config: { length: 32, noSpecial: true, minLower: 1, minUpper: 1, minNumeric: 1 } } })
@description('The PostgreSQL administrator\'s password.')
param postgresPassword string

@secure()
@metadata({ azd: { type: 'generate', config: { length: 32, noSpecial: true, minLower: 1, minUpper: 1, minNumeric: 1 } } })
@description('Keycloak\'s bootstrap administrator\'s password (AppHost parameter keycloak-password).')
param keycloakPassword string

@secure()
@metadata({ azd: { type: 'generate', config: { length: 32, noSpecial: true } } })
@description('The fabric-pilot-user-admin client\'s secret (AppHost parameter keycloak-user-admin-secret).')
param keycloakUserAdminSecret string

@secure()
@metadata({ azd: { type: 'generate', config: { length: 32, noSpecial: true } } })
@description('The fabric-pilot-staff-reader client\'s secret (AppHost parameter keycloak-staff-reader-secret).')
param keycloakStaffReaderSecret string

@secure()
@metadata({ azd: { type: 'generate', config: { length: 32, noSpecial: true } } })
@description('The broker\'s password.')
param rabbitMqPassword string

var tags = { 'azd-env-name': environmentName }

resource group 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  name: 'resources'
  scope: group
  params: {
    location: location
    openAiLocation: openAiLocation
    tags: tags
    principalId: principalId
    tenants: tenants
    smtpConnection: smtpConnection
    senderAddress: senderAddress
    smtpUserName: smtpUserName
    smtpPassword: smtpPassword
    chatDeploymentName: chatDeploymentName
    chatModelVersion: chatModelVersion
    chatDeploymentSku: chatDeploymentSku
    chatDeploymentCapacity: chatDeploymentCapacity
    embeddingDeploymentName: embeddingDeploymentName
    embeddingModelVersion: embeddingModelVersion
    embeddingDeploymentSku: embeddingDeploymentSku
    embeddingDeploymentCapacity: embeddingDeploymentCapacity
    postgresVersion: postgresVersion
    hostImageName: hostImageName
    migrationsImageName: migrationsImageName
    webImageName: webImageName
    postgresPassword: postgresPassword
    keycloakPassword: keycloakPassword
    keycloakUserAdminSecret: keycloakUserAdminSecret
    keycloakStaffReaderSecret: keycloakStaffReaderSecret
    rabbitMqPassword: rabbitMqPassword
  }
}

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = group.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = resources.outputs.registryEndpoint
output AZURE_CONTAINER_APPS_ENVIRONMENT_NAME string = resources.outputs.environmentName
output AZURE_KEY_VAULT_NAME string = resources.outputs.keyVaultName
output AZURE_OPENAI_ENDPOINT string = resources.outputs.openAiEndpoint
output KEYCLOAK_URL string = resources.outputs.keycloakUrl
output WEB_URL string = resources.outputs.webUrl
output MIGRATIONS_JOB_NAME string = resources.outputs.migrationsJobName
output TENANTS string = tenants
