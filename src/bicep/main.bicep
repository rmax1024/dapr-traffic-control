
@description('The target Azure location for all resources')
param location string
@description('A string that will be prepended to all resource names')
param prefix string

var rgName = 'mrusaev'
var serviceBusNamespace = '${prefix}-aind-namespace'

module components 'components.bicep' = {
  name: 'aind-components-deploy'
  scope: resourceGroup(rgName)
  params: {
    location: location
    serviceBusNamespace: serviceBusNamespace
  }
}

output servicebus_connection_string string = components.outputs.servicebus_connection_string