// Everything in the environment's resource group. Each resource mirrors one the AppHost declares (src/AppHost/AppHost.cs);
// the names of the host's settings are the ones the AppHost sets, so the host runs unchanged.
param location string
param openAiLocation string
param tags object
param keycloakAdminAddresses string
param tenants string
param smtpConnection string
param senderAddress string
param smtpUserName string
param chatDeploymentName string
param chatModelVersion string
param chatDeploymentSku string
param chatDeploymentCapacity int
param embeddingDeploymentName string
param embeddingModelVersion string
param embeddingDeploymentSku string
param embeddingDeploymentCapacity int
param postgresVersion string
param hostImageName string
param migrationsImageName string
param webImageName string

@secure()
param postgresPassword string
@secure()
param keycloakPassword string
@secure()
param keycloakUserAdminSecret string
@secure()
param keycloakStaffReaderSecret string
@secure()
param rabbitMqPassword string
@secure()
param smtpPassword string

var token = uniqueString(subscription().id, resourceGroup().id, location)
// Container Apps need an image before azd's first deploy replaces it.
var placeholderImage = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
var realm = 'fabric-pilot'
var postgresAdministrator = 'fabricpilot'
var rabbitMqUser = 'fabricpilot'

var roles = {
  acrPull: '7f951dda-4ed3-4680-a7ca-43fe172d538d'
  keyVaultSecretsUser: '4633458b-17de-408a-b874-0445c86b69e6'
  storageBlobDataContributor: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
}

// The virtual network (docs/adr/0055): the Container Apps environment in one subnet, the private endpoints of PostgreSQL,
// Key Vault and Storage in the other. None of the three answers from the internet; inside the network their usual names
// resolve to the endpoints, so connection strings and Key Vault references stay as they are.
resource network 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: 'vnet-${token}'
  location: location
  tags: tags
  properties: {
    addressSpace: { addressPrefixes: ['10.40.0.0/16'] }
    subnets: [
      {
        name: 'apps'
        properties: {
          addressPrefix: '10.40.0.0/23'
          delegations: [{ name: 'container-apps', properties: { serviceName: 'Microsoft.App/environments' } }]
        }
      }
      {
        name: 'endpoints'
        properties: { addressPrefix: '10.40.2.0/24' }
      }
    ]
  }
}

var appsSubnetId = network.properties.subnets[0].id
var endpointsSubnetId = network.properties.subnets[1].id

module postgresEndpoint 'private-endpoint.bicep' = {
  name: 'postgres-endpoint'
  params: {
    location: location
    tags: tags
    resourceId: postgres.id
    groupId: 'postgresqlServer'
    zoneName: 'privatelink.postgres.database.azure.com'
    networkId: network.id
    subnetId: endpointsSubnetId
  }
}

module vaultEndpoint 'private-endpoint.bicep' = {
  name: 'vault-endpoint'
  params: {
    location: location
    tags: tags
    resourceId: vault.id
    groupId: 'vault'
    zoneName: 'privatelink.vaultcore.azure.net'
    networkId: network.id
    subnetId: endpointsSubnetId
  }
}

module blobEndpoint 'private-endpoint.bicep' = {
  name: 'blob-endpoint'
  params: {
    location: location
    tags: tags
    resourceId: storage.id
    groupId: 'blob'
    zoneName: 'privatelink.blob.${az.environment().suffixes.storage}'
    networkId: network.id
    subnetId: endpointsSubnetId
  }
}

// One identity for every app: it pulls images, reads the Key Vault secrets and reaches the blobs without a key.
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${token}'
  location: location
  tags: tags
}

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${token}'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'cr${token}'
  location: location
  tags: tags
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false
  }
}

resource registryPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, roles.acrPull)
  scope: registry
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.acrPull)
  }
}

// Key Vault: every Aspire secret parameter the AppHost declares, under the parameter's name, and the connection strings
// that carry a password. The apps read them through the identity; nothing secret is an app setting in clear.
resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${token}'
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    softDeleteRetentionInDays: 7
    // Only through its private endpoint. The deployment writes the secrets through Azure Resource Manager, which the
    // network rules do not apply to; nobody reads them from outside (create-realm.sh reads azd's own copies).
    publicNetworkAccess: 'Disabled'
    networkAcls: { defaultAction: 'Deny', bypass: 'None' }
  }
}

resource vaultRead 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, identity.id, roles.keyVaultSecretsUser)
  scope: vault
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.keyVaultSecretsUser)
  }
}

resource openAiApiKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'azure-openai-api-key'
  properties: { value: openAi.listKeys().key1 }
}

resource keycloakPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'keycloak-password'
  properties: { value: keycloakPassword }
}

resource keycloakUserAdminSecretSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'keycloak-user-admin-secret'
  properties: { value: keycloakUserAdminSecret }
}

resource keycloakStaffReaderSecretSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'keycloak-staff-reader-secret'
  properties: { value: keycloakStaffReaderSecret }
}

// The SMTP server's password, only for one that requires sign-in: with a user name, the host signs in as that user.
var smtpSignsIn = !empty(smtpUserName)

resource smtpPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (smtpSignsIn) {
  parent: vault
  name: 'smtp-password'
  properties: { value: smtpPassword }
}

resource postgresPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'postgres-password'
  properties: { value: postgresPassword }
}

resource databaseConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'database-connection'
  properties: {
    value: 'Host=${postgres.properties.fullyQualifiedDomainName};Port=5432;Database=${database.name};Username=${postgresAdministrator};Password=${postgresPassword};SSL Mode=Require'
  }
}

resource rabbitMqPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'rabbitmq-password'
  properties: { value: rabbitMqPassword }
}

resource messagingConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'messaging-connection'
  properties: { value: 'amqp://${rabbitMqUser}:${rabbitMqPassword}@rabbitmq:5672' }
}

// PostgreSQL: the application's database and Keycloak's, on one Flexible Server. The vector extension is allowed; the
// MigrationRunner creates it (CREATE EXTENSION IF NOT EXISTS vector).
resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: 'psql-${token}'
  location: location
  tags: tags
  sku: { name: 'Standard_B1ms', tier: 'Burstable' }
  properties: {
    version: postgresVersion
    administratorLogin: postgresAdministrator
    administratorLoginPassword: postgresPassword
    authConfig: { activeDirectoryAuth: 'Disabled', passwordAuth: 'Enabled' }
    storage: { storageSizeGB: 32, autoGrow: 'Enabled' }
    backup: { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
    highAvailability: { mode: 'Disabled' }
    // Only through its private endpoint (postgresEndpoint); the server keeps its name, which resolves to it in the network.
    network: { publicNetworkAccess: 'Disabled' }
  }
}

resource postgresExtensions 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2024-08-01' = {
  parent: postgres
  name: 'azure.extensions'
  properties: { value: 'VECTOR', source: 'user-override' }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: 'database'
  properties: { charset: 'UTF8', collation: 'en_US.utf8' }
  dependsOn: [postgresExtensions]
}

resource keycloakDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgres
  name: 'keycloak'
  properties: { charset: 'UTF8', collation: 'en_US.utf8' }
  dependsOn: [database]
}

// Blob storage for documents and ticket attachments (the AppHost's "blobs"). No account key: the host reaches the
// endpoint with its identity (BlobServiceClients).
resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'st${token}'
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    // Only through its private endpoint (blobEndpoint); the host keeps the blob endpoint's name.
    publicNetworkAccess: 'Disabled'
    networkAcls: { defaultAction: 'Deny', bypass: 'None' }
  }
}

resource blobsForHost 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, identity.id, roles.storageBlobDataContributor)
  scope: storage
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.storageBlobDataContributor)
  }
}

// The model: an Azure OpenAI resource with the chat and embedding deployments the AppHost's parameters name. The host
// authenticates with its key (docs/adr/fabric/backend-llm-azure-openai.md), kept in Key Vault.
resource openAi 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: 'oai-${token}'
  location: openAiLocation
  tags: tags
  kind: 'OpenAI'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: 'oai-${token}'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

resource chatDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: openAi
  name: chatDeploymentName
  sku: { name: chatDeploymentSku, capacity: chatDeploymentCapacity }
  properties: {
    model: { format: 'OpenAI', name: 'gpt-5-mini', version: chatModelVersion }
  }
}

// One deployment at a time: the resource refuses concurrent changes.
resource embeddingDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: openAi
  name: embeddingDeploymentName
  sku: { name: embeddingDeploymentSku, capacity: embeddingDeploymentCapacity }
  properties: {
    model: { format: 'OpenAI', name: 'text-embedding-3-small', version: embeddingModelVersion }
  }
  dependsOn: [chatDeployment]
}

resource environment 'Microsoft.App/managedEnvironments@2025-01-01' = {
  name: 'cae-${token}'
  location: location
  tags: tags
  properties: {
    // In the network's apps subnet, with a public ingress for the web app and Keycloak; everything else is internal.
    vnetConfiguration: { infrastructureSubnetId: appsSubnetId, internal: false }
    workloadProfiles: [{ name: 'Consumption', workloadProfileType: 'Consumption' }]
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

var keycloakUrl = 'https://keycloak.${environment.properties.defaultDomain}'
var webUrl = 'https://web.${environment.properties.defaultDomain}'
var identities = { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
var registries = [{ server: registry.properties.loginServer, identity: identity.id }]

// The broker (the AppHost's "messaging"), reached inside the environment only. No volume: Wolverine keeps its outbox
// and inbox in PostgreSQL, so a restart loses no message that was not yet delivered to a queue.
resource rabbitMq 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'rabbitmq'
  location: location
  tags: tags
  identity: identities
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: false, targetPort: 5672, exposedPort: 5672, transport: 'tcp' }
      secrets: [{ name: 'rabbitmq-password', keyVaultUrl: rabbitMqPasswordSecret.properties.secretUri, identity: identity.id }]
    }
    template: {
      containers: [
        {
          name: 'rabbitmq'
          image: 'docker.io/library/rabbitmq:4.3'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'RABBITMQ_DEFAULT_USER', value: rabbitMqUser }
            { name: 'RABBITMQ_DEFAULT_PASS', secretRef: 'rabbitmq-password' }
          ]
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
  dependsOn: [vaultRead, vaultEndpoint]
}

// Keycloak in production mode, on its own database, behind the environment's TLS. Its realm is created once by hand
// (infra/README.md); nothing is imported on start (docs/adr/0052). The ingress reaches an nginx beside it
// (keycloak/proxy.conf), which keeps the admin console to the environment and the listed addresses (docs/adr/0055).
var keycloakAdminAllowed = join(map(filter(split(keycloakAdminAddresses, ','), address => !empty(trim(address))), address => 'allow ${trim(address)};'), '\n        ')
var keycloakProxyConfiguration = replace(loadTextContent('keycloak/proxy.conf'), '# KEYCLOAK_ADMIN_ADDRESSES', keycloakAdminAllowed)

resource keycloak 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'keycloak'
  location: location
  tags: tags
  identity: identities
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: true, targetPort: 8000, transport: 'auto', allowInsecure: false }
      secrets: [
        { name: 'keycloak-password', keyVaultUrl: keycloakPasswordSecret.properties.secretUri, identity: identity.id }
        { name: 'postgres-password', keyVaultUrl: postgresPasswordSecret.properties.secretUri, identity: identity.id }
      ]
    }
    template: {
      containers: [
        {
          name: 'keycloak'
          image: 'quay.io/keycloak/keycloak:26.6'
          args: ['start']
          resources: { cpu: json('1'), memory: '2Gi' }
          env: [
            { name: 'KC_BOOTSTRAP_ADMIN_USERNAME', value: 'admin' }
            { name: 'KC_BOOTSTRAP_ADMIN_PASSWORD', secretRef: 'keycloak-password' }
            { name: 'KC_DB', value: 'postgres' }
            {
              name: 'KC_DB_URL'
              value: 'jdbc:postgresql://${postgres.properties.fullyQualifiedDomainName}:5432/${keycloakDatabase.name}?sslmode=require'
            }
            { name: 'KC_DB_USERNAME', value: postgresAdministrator }
            { name: 'KC_DB_PASSWORD', secretRef: 'postgres-password' }
            { name: 'KC_HOSTNAME', value: keycloakUrl }
            { name: 'KC_HTTP_ENABLED', value: 'true' }
            { name: 'KC_PROXY_HEADERS', value: 'xforwarded' }
            { name: 'KC_HEALTH_ENABLED', value: 'true' }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: { path: '/health/started', port: 9000 }
              initialDelaySeconds: 30
              periodSeconds: 15
              failureThreshold: 10
            }
            {
              type: 'Readiness'
              httpGet: { path: '/health/ready', port: 9000 }
              periodSeconds: 15
            }
            {
              type: 'Liveness'
              httpGet: { path: '/health/live', port: 9000 }
              periodSeconds: 30
            }
          ]
        }
        {
          name: 'proxy'
          image: 'docker.io/nginxinc/nginx-unprivileged:1.31-alpine'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          // The configuration replaces the image's default server (port 8080, Keycloak's) before nginx starts.
          command: ['/bin/sh', '-c']
          args: ['printf \'%s\\n\' "$PROXY_CONFIGURATION" > /etc/nginx/conf.d/default.conf && exec nginx -g \'daemon off;\'']
          env: [{ name: 'PROXY_CONFIGURATION', value: keycloakProxyConfiguration }]
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
  dependsOn: [vaultRead, postgresEndpoint, vaultEndpoint]
}

var hostSecrets = concat([
  { name: 'database-connection', keyVaultUrl: databaseConnection.properties.secretUri, identity: identity.id }
  { name: 'messaging-connection', keyVaultUrl: messagingConnection.properties.secretUri, identity: identity.id }
  { name: 'azure-openai-api-key', keyVaultUrl: openAiApiKey.properties.secretUri, identity: identity.id }
  { name: 'keycloak-user-admin-secret', keyVaultUrl: keycloakUserAdminSecretSecret.properties.secretUri, identity: identity.id }
  { name: 'keycloak-staff-reader-secret', keyVaultUrl: keycloakStaffReaderSecretSecret.properties.secretUri, identity: identity.id }
], smtpSignsIn ? [
  { name: 'smtp-password', keyVaultUrl: smtpPasswordSecret!.properties.secretUri, identity: identity.id }
] : [])

// The settings the AppHost gives the host, by the same names (infra/check.sh compares them).
var hostSettings = [
  { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
  { name: 'ConnectionStrings__database', secretRef: 'database-connection' }
  { name: 'ConnectionStrings__messaging', secretRef: 'messaging-connection' }
  { name: 'ConnectionStrings__blobs', value: storage.properties.primaryEndpoints.blob }
  { name: 'ConnectionStrings__mail', value: smtpConnection }
  { name: 'Notifications__SenderAddress', value: senderAddress }
  { name: 'Authentication__Authority', value: '${keycloakUrl}/realms/${realm}' }
  { name: 'KeycloakAdmin__BaseAddress', value: keycloakUrl }
  { name: 'KeycloakAdmin__Realm', value: realm }
  { name: 'KeycloakAdmin__ClientId', value: 'fabric-pilot-user-admin' }
  { name: 'KeycloakAdmin__ClientSecret', secretRef: 'keycloak-user-admin-secret' }
  { name: 'KeycloakAdmin__StaffReaderClientId', value: 'fabric-pilot-staff-reader' }
  { name: 'KeycloakAdmin__StaffReaderClientSecret', secretRef: 'keycloak-staff-reader-secret' }
  { name: 'AzureOpenAI__Endpoint', value: openAi.properties.endpoint }
  { name: 'AzureOpenAI__Deployment', value: chatDeployment.name }
  { name: 'AzureOpenAI__ApiKey', secretRef: 'azure-openai-api-key' }
  { name: 'AzureOpenAI__EmbeddingDeployment', value: embeddingDeployment.name }
]

var tenantSettings = [for (tenant, index) in split(tenants, ','): { name: 'Tenancy__Tenants__${index}', value: trim(tenant) }]

// The host (the AppHost's "host"), reached only through the web app's /api.
resource host 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'host'
  location: location
  tags: union(tags, { 'azd-service-name': 'host' })
  identity: identities
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: false, targetPort: 8080, transport: 'auto', allowInsecure: false }
      registries: registries
      secrets: hostSecrets
    }
    template: {
      containers: [
        {
          name: 'host'
          image: empty(hostImageName) ? placeholderImage : hostImageName
          resources: { cpu: json('1'), memory: '2Gi' }
          // The SMTP sign-in only for a server that requires it; without it the host sends unauthenticated (SmtpSettings).
          env: concat(hostSettings, smtpSignsIn ? [
            { name: 'Notifications__SmtpUserName', value: smtpUserName }
            { name: 'Notifications__SmtpPassword', secretRef: 'smtp-password' }
          ] : [], tenantSettings)
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
  dependsOn: [registryPull, vaultRead, blobsForHost, rabbitMq, postgresEndpoint, vaultEndpoint, blobEndpoint]
}

// The MigrationRunner (the AppHost's "migrations"), a job azd starts after each deploy of it and waits for
// (infra/scripts/run-migrations.sh), before it deploys the host.
resource migrations 'Microsoft.App/jobs@2025-01-01' = {
  name: 'migrations'
  location: location
  tags: union(tags, { 'azd-service-name': 'migrations' })
  identity: identities
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 1800
      replicaRetryLimit: 0
      manualTriggerConfig: { parallelism: 1, replicaCompletionCount: 1 }
      registries: registries
      secrets: [{ name: 'database-connection', keyVaultUrl: databaseConnection.properties.secretUri, identity: identity.id }]
    }
    template: {
      containers: [
        {
          name: 'migrations'
          image: empty(migrationsImageName) ? placeholderImage : migrationsImageName
          resources: { cpu: json('0.5'), memory: '1Gi' }
          // The blob endpoint too, reached with the apps' identity: the runner moves attachments stored before their
          // names carried the tenant (LegacyAttachmentNames).
          env: [
            { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
            { name: 'ConnectionStrings__database', secretRef: 'database-connection' }
            { name: 'ConnectionStrings__blobs', value: storage.properties.primaryEndpoints.blob }
          ]
        }
      ]
    }
  }
  dependsOn: [registryPull, vaultRead, blobsForHost, postgresEndpoint, vaultEndpoint, blobEndpoint]
}

// The web app (the AppHost's "web"): the built SPA behind nginx, which forwards /api to the host as Vite does locally.
resource web 'Microsoft.App/containerApps@2025-01-01' = {
  name: 'web'
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  identity: identities
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: { external: true, targetPort: 8080, transport: 'auto', allowInsecure: false }
      registries: registries
    }
    template: {
      containers: [
        {
          name: 'web'
          image: empty(webImageName) ? placeholderImage : webImageName
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [{ name: 'HOST_URL', value: 'https://${host.properties.configuration.ingress.fqdn}' }]
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 1 }
    }
  }
  dependsOn: [registryPull]
}

output registryEndpoint string = registry.properties.loginServer
output environmentName string = environment.name
output keyVaultName string = vault.name
output openAiEndpoint string = openAi.properties.endpoint
output keycloakUrl string = keycloakUrl
output webUrl string = webUrl
output migrationsJobName string = migrations.name
