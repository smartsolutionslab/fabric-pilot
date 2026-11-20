// One resource reached from the virtual network only (docs/adr/0055): its private endpoint in the endpoints subnet, and
// the private DNS zone that resolves the resource's usual name to that endpoint inside the network. Callers keep the
// name they use today; only where it resolves changes.
param location string
param tags object

@description('The resource the endpoint leads to.')
param resourceId string

@description('The resource\'s sub-resource (Private Link group): postgresqlServer, vault or blob.')
param groupId string

@description('The private DNS zone of that group, e.g. privatelink.vaultcore.azure.net.')
param zoneName string

@description('The virtual network whose DNS resolves the zone.')
param networkId string

@description('The subnet the endpoint takes its address from.')
param subnetId string

resource zone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: zoneName
  location: 'global'
  tags: tags
}

resource link 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: zone
  name: 'network'
  location: 'global'
  tags: tags
  properties: {
    registrationEnabled: false
    virtualNetwork: { id: networkId }
  }
}

resource endpoint 'Microsoft.Network/privateEndpoints@2024-05-01' = {
  name: 'pe-${groupId}-${uniqueString(resourceId)}'
  location: location
  tags: tags
  properties: {
    subnet: { id: subnetId }
    privateLinkServiceConnections: [
      {
        name: groupId
        properties: {
          privateLinkServiceId: resourceId
          groupIds: [groupId]
        }
      }
    ]
  }
}

resource zoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = {
  parent: endpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [{ name: groupId, properties: { privateDnsZoneId: zone.id } }]
  }
  dependsOn: [link]
}
