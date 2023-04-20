
@description('The target Azure location for all resources')
param location string
@description('A string that will be prepended to all resource names')
param prefix string
@description('The ObjectId of the service principal that will be granted Key Vault access')
param keyVaultAccessObjectId string

var rgName = 'mrusaev'
var serviceBusNamespace = '${prefix}-aind-namespace'
var keyVaultName = '${prefix}aindkv'
var serviceBusConnectionStringSecretName = 'ServiceBus-ConnectionString'

var storageAccountName = '${prefix}storage'
var entryCamQueueName = 'entrycam'
var exitCamQueueName = 'exitcam'
var storageAccountAccessKeySecretName = 'StorageQueue-AccessKey'

module components 'components.bicep' = {
  name: 'aind-components-deploy'
  scope: resourceGroup(rgName)
  params: {
    location: location
    serviceBusNamespace: serviceBusNamespace
    keyVaultName: keyVaultName
    keyVaultAccessObjectId: keyVaultAccessObjectId
    serviceBusConnectionStringSecretName: serviceBusConnectionStringSecretName
    storageAccountName: storageAccountName
    entryCamQueueName: entryCamQueueName
    exitCamQueueName: exitCamQueueName
    storageAccountAccessKeySecretName: storageAccountAccessKeySecretName
  }
}

output servicebus_connection_string string = components.outputs.servicebus_connection_string