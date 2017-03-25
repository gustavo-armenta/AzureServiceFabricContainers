Login-AzureRmAccount

. .\common.ps1
$ResourceGroupName = "test-scus"
$ResourceGroupLocation = "southcentralus"
Fabric-AddKeyVaultSecrets -ResourceGroupName $ResourceGroupName -ResourceGroupLocation $ResourceGroupLocation

Get-ChildItem -path cert:\CurrentUser\My |fl