Login-AzureRmAccount -EnvironmentName AzureChinaCloud

. .\common.ps1
$ResourceGroupName = "test-cnn"
$ResourceGroupLocation = "chinanorth"
Sql-AddKeyVaultSecrets -ResourceGroupName $ResourceGroupName -ResourceGroupLocation $ResourceGroupLocation
Fabric-AddKeyVaultSecrets -ResourceGroupName $ResourceGroupName -ResourceGroupLocation $ResourceGroupLocation

Get-ChildItem -path cert:\CurrentUser\My |fl