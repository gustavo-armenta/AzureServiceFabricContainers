Login-AzureRmAccount

. .\common.ps1
$ResourceGroupName = "test-scus"
$ResourceGroupLocation = "southcentralus"
Sql-AddKeyVaultSecrets -ResourceGroupName $ResourceGroupName -ResourceGroupLocation $ResourceGroupLocation
Fabric-AddKeyVaultSecrets -ResourceGroupName $ResourceGroupName -ResourceGroupLocation $ResourceGroupLocation

Get-ChildItem -path cert:\CurrentUser\My |fl