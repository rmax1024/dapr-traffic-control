[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string] $ResourcePrefix,

    [Parameter(Mandatory=$true)]
    [string] $Location = "eastus"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 4.0
$here = Split-Path -Parent $PSCommandPath

if (!(Get-AzContext)) {
    Write-Host "Login to Azure PowerShell before continuing"
    Connect-AzAccount
}

Get-AzContext | Format-List | Out-String | Write-Host
Read-Host "Press <RETURN> to confirm deployment into the above Azure subscription, or <CTRL-C> to cancel"
$tenantId = (Get-AzContext).Tenant.Id

# Setup a service principal with a short-lived password
$spName = "$ResourcePrefix-adventures-in-dapr-sp"
$servicePrincipal = Get-AzADServicePrincipal -DisplayName $spName
if ((!$servicePrincipal) -or ([string]::IsNullOrEmpty($env:AZURE_CLIENT_SECRET)) -or (([datetime]$env:AZURE_CLIENT_SECRET_EXPIRATION) -lt ([datetime]::Now))) {
    $servicePrincipal = Get-AzADServicePrincipal -DisplayName $spName
    $endDate = ([datetime]::Now.AddDays(5))
    if (!$servicePrincipal) {
        $servicePrincipal = New-AzADServicePrincipal -DisplayName $spName -EndDate $endDate
        $env:AZURE_CLIENT_SECRET = $servicePrincipal.PasswordCredentials.SecretText
    }
    else {
        
        $newSpCred = $servicePrincipal | New-AzADServicePrincipalCredential -EndDate $endDate
        $env:AZURE_CLIENT_SECRET = $newSpCred.SecretText
    }

    $env:AZURE_CLIENT_SECRET_EXPIRATION = $endDate
    $env:AZURE_CLIENT_ID = $servicePrincipal.appId
    $env:AZURE_TENANT_ID = $tenantId
    $env:AZURE_CLIENT_OBJECTID = $servicePrincipal.id
}


$timestamp = Get-Date -f yyyyMMddTHHmmssZ
$armParams = @{
    location = $Location
    prefix = $ResourcePrefix
    keyVaultAccessObjectId = $env:AZURE_CLIENT_OBJECTID
}
$res = New-AzResourceGroupDeployment -Name "deploy-aind-ep01-$timestamp" `
                                     -ResourceGroupName mrusaev `
                                     -TemplateFile $here/main.bicep `
                                     -TemplateParameterObject $armParams `
                                     -Location $armParams.location `
                                     -Verbose

Write-Host "`nARM provisioning completed successfully"

$tenantFqdn = Get-AzTenant | ? { $_.Id -eq (Get-AzContext).Tenant.Id } | Select -ExpandProperty Domains | Select -First 1
Write-Host "`nPortal Link: https://portal.azure.com/#@$tenantFqdn/resource/subscriptions/$((Get-AzContext).Subscription.Id)/resourceGroups/mrusaev/overview"
Write-Host "`nService Bus Connection String: $($res.Outputs.servicebus_connection_string.Value)"

Write-Host "`nSet the following environment variables in the console(s) used to launch the services:"
Write-Host "`$env:AZURE_CLIENT_ID = `"$($env:AZURE_CLIENT_ID)`""
Write-Host "`$env:AZURE_CLIENT_SECRET = `"$($env:AZURE_CLIENT_SECRET)`""
Write-Host "`$env:AZURE_TENANT_ID = `"$($env:AZURE_TENANT_ID)`""