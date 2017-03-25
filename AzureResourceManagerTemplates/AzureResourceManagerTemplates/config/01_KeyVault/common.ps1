function global:Fabric-AddKeyVaultSecrets
{
	param (
		[parameter(Position = 0, Mandatory = $true)]
		[string] $ResourceGroupName,
		[parameter(Position = 1, Mandatory = $true)]
		[string] $ResourceGroupLocation
	)
	
	$vaultName = "$ResourceGroupName-keyvault"
	$fabricName = "$ResourceGroupName-fabric"
	$passwords = Get-Content "$fabricName.passwords.json" | ConvertFrom-Json
	
	$secretName = "$fabricName-vm"
	$securePassword = ConvertTo-SecureString -String $passwords.vm -Force -AsPlainText
	CreatePassword $vaultName $secretName $securePassword

	$secretName = "$fabricName-primarycertificate"
	$dnsName = "*.$ResourceGroupLocation.cloudapp.azure.com"
	$pfxFileName = "star.$ResourceGroupLocation.cloudapp.azure.com.primary.pfx"
	$pfxFilePath = Join-Path $pwd $pfxFileName
	$password = $passwords.sslCertificate
	$securePassword = ConvertTo-SecureString -String $password -Force -AsPlainText
	CreateSslCertificate $vaultName $secretName $dnsName $pfxFilePath $password $securePassword

	$secretName = "$fabricName-secondarycertificate"
	$dnsName = "*.$ResourceGroupLocation.cloudapp.azure.com"
	$pfxFileName = "star.$ResourceGroupLocation.cloudapp.azure.com.secondary.pfx"
	$pfxFilePath = Join-Path $pwd $pfxFileName
	$password = $passwords.sslCertificate
	$securePassword = ConvertTo-SecureString -String $password -Force -AsPlainText
	CreateSslCertificate $vaultName $secretName $dnsName $pfxFilePath $password $securePassword

	$secretName = "$fabricName-admincertificate"
	$dnsName = "admin.fabric.azure.com"
	$pfxFileName = "$dnsName.pfx"
	$pfxFilePath = Join-Path $pwd $pfxFileName
	$password = $passwords.adminCertificate
	$securePassword = ConvertTo-SecureString -String $password -Force -AsPlainText
	CreateSslCertificate $vaultName $secretName $dnsName $pfxFilePath $password $securePassword

	$secretName = "$fabricName-encryptioncertificate"
	$subject = "encryption.fabric.azure.com"
	$pfxFileName = "$subject.pfx"
	$pfxFilePath = Join-Path $pwd $pfxFileName
	$password = $passwords.encryptionCertificate
	$securePassword = ConvertTo-SecureString -String $password -Force -AsPlainText
	CreateEncryptionCertificate $vaultName $secretName $subject $pfxFilePath $password $securePassword
}

function CreatePassword($vaultName, $secretName, $securePassword)
{
	$found = Get-AzureKeyVaultSecret -VaultName $vaultName -Name $secretName -ErrorAction SilentlyContinue
	if ($found)
	{
		Write-Warning "Secret already exists in keyvault: $secretName"
	}
	else
	{
		Write-Host "Creating secret: $secretName"
		$secret = Set-AzureKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $securePassword
	}
}

function CreateSslCertificate($vaultName, $secretName, $dnsName, $pfxFilePath, $password, $securePassword)
{
	if (Test-Path $pfxFilePath)
	{
		Write-Warning "Certificate already exists: $pfxFilePath"
	}
	else
	{
		Write-Host "Creating certificate: $pfxFilePath"
		New-SelfSignedCertificate -CertStoreLocation Cert:\CurrentUser\My -DnsName $dnsName -Provider 'Microsoft Enhanced Cryptographic Provider v1.0' | Export-PfxCertificate -FilePath $pfxFilePath -Password $securePassword | Out-Null
	}

	$found = Get-AzureKeyVaultSecret -VaultName $vaultName -Name $secretName -ErrorAction SilentlyContinue
	if ($found)
	{
		Write-Warning "Secret already exists in keyvault: $secretName"
	}
	else
	{
		Write-Host "Creating secret: $secretName"
		$bytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
		$base64 = [System.Convert]::ToBase64String($bytes)
		$jsonBlob = @{
			data = $base64
			dataType = 'pfx'
			password = $password
		} | ConvertTo-Json
		$contentbytes = [System.Text.Encoding]::UTF8.GetBytes($jsonBlob)
		$content = [System.Convert]::ToBase64String($contentbytes)
		$secretValue = ConvertTo-SecureString -String $content -AsPlainText -Force
		$secret = Set-AzureKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secretValue
	}
}

function CreateEncryptionCertificate($vaultName, $secretName, $subject, $pfxFilePath, $password, $securePassword)
{
	if (Test-Path $pfxFilePath)
	{
		Write-Warning "Certificate already exists: $pfxFilePath"
	}
	else
	{
		Write-Host "Creating certificate: $pfxFilePath"
		New-SelfSignedCertificate -CertStoreLocation Cert:\CurrentUser\My -Type DocumentEncryptionCert -KeyUsage DataEncipherment -Subject $subject -Provider 'Microsoft Enhanced Cryptographic Provider v1.0' | Export-PfxCertificate -FilePath $pfxFilePath -Password $securePassword | Out-Null
	}

	$found = Get-AzureKeyVaultSecret -VaultName $vaultName -Name $secretName -ErrorAction SilentlyContinue
	if ($found)
	{
		Write-Warning "Secret already exists in keyvault: $secretName"
	}
	else
	{
		Write-Host "Creating secret: $secretName"
		$bytes = [System.IO.File]::ReadAllBytes($pfxFilePath)
		$base64 = [System.Convert]::ToBase64String($bytes)
		$jsonBlob = @{
			data = $base64
			dataType = 'pfx'
			password = $password
		} | ConvertTo-Json
		$contentbytes = [System.Text.Encoding]::UTF8.GetBytes($jsonBlob)
		$content = [System.Convert]::ToBase64String($contentbytes)
		$secretValue = ConvertTo-SecureString -String $content -AsPlainText -Force
		$secret = Set-AzureKeyVaultSecret -VaultName $vaultName -Name $secretName -SecretValue $secretValue
	}
}