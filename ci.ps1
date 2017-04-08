Param(
  [string]$task,
  [string]$configuration,
  [string]$version
)

$msbuild='C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\amd64\msbuild.exe'
$xmlTransformLibrary='C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\Microsoft\VisualStudio\v15.0\Web\Microsoft.Web.XmlTransform.dll'

Write-Host ""
Write-Host "================================================================================================"
Write-Host "task=$task"
Write-Host "configuration=$configuration"
Write-Host "version=$version"
Write-Host "msbuild=$msbuild"
Write-Host "xmlTransformLibrary=$xmlTransformLibrary"
Write-Host "================================================================================================"
Write-Host ""

function Main($task, $configuration, $version)
{
    if ($task -eq 'Encrypt')
    {
        EncryptTask $configuration $version
    }
    elseif ($task -eq 'Decrypt')
    {
        DecryptTask $configuration $version
    }
    elseif ($task -eq 'Build')
    {
        BuildTask $configuration $version
    }
    elseif ($task -eq 'Container')
    {
        ContainerTask $configuration $version
    }
    elseif ($task -eq 'Fabric')
    {
        FabricTask $configuration $version
    }
}

function EncryptTask($configuration, $version)
{
    $root = $pwd
    Push-Location "$root\packages\SecureText.1.0.0"
    dotnet SecureText.dll Encrypt "$root\FullFrameworkApps"
    Pop-Location
    Write-Host "Encrypt completed"
}

function DecryptTask($configuration, $version)
{
    $root = $pwd
    Push-Location "$root\packages\SecureText.1.0.0"
    dotnet SecureText.dll Decrypt "$root\FullFrameworkApps"
    Pop-Location
    Write-Host "Decrypt completed"
}

function BuildTask($configuration, $version)
{
    $c = 'Release'
    if($configuration -eq 'Debug')
    {
        $c = 'Debug'
    }

    & $msbuild "$pwd\FullFrameworkApps\FullFrameworkApps.sln" /nologo /v:minimal /t:Rebuild /p:Configuration=$c

    BuildConsoleApp "$pwd\FullFrameworkApps\ConsoleApp1\ConsoleApp1.csproj" $configuration
    BuildWebApp "$pwd\FullFrameworkApps\WebApplication1\WebApplication1.csproj" $configuration

    $root = $pwd
    $packagePath = "$env:SystemDrive\ci\$configuration"
    Push-Location "$root\packages\SecureText.1.0.0"
    dotnet SecureText.dll Decrypt "$packagePath"
    Pop-Location
    Get-ChildItem $packagePath -Recurse | Where{$_.Name -Match ".*(.securetext)$"} | Remove-Item
    Write-Host "Decrypt completed"
}

function ContainerTask($configuration, $version)
{
    BuildContainer "$pwd\FullFrameworkApps\ConsoleApp1\ConsoleApp1.csproj" $configuration $version
    BuildContainer "$pwd\FullFrameworkApps\WebApplication1\WebApplication1.csproj" $configuration $version
}

function FabricTask($configuration, $version)
{
    BuildFabric "$pwd\FabricApps\ConsoleApp1\ConsoleApp1.sfproj" $configuration $version
    BuildFabric "$pwd\FabricApps\WebApplication1\WebApplication1.sfproj" $configuration $version
}

function BuildConsoleApp($project, $configuration)
{
    [xml]$xml = Get-Content $project;
    $namespace = @{m = 'http://schemas.microsoft.com/developer/msbuild/2003'}
    $assemblyName = Select-Xml -Xml $xml -Namespace $namespace -XPath '//m:AssemblyName' | %{$_.Node.'#text'}
    $projectPath = Split-Path $project -Parent
    $packagePath = GetPackagePath $configuration $assemblyName
    if (Test-Path "$packagePath")
    {
        Remove-Item "$packagePath" -Recurse
    }

    Copy-Item "$projectPath\bin\Release" "$packagePath" -Recurse
    XmlDocTransform "$projectPath\App.config" "$projectPath\App.$configuration.config" "$packagePath\$assemblyName.exe.config"
    Write-Host "  $assemblyName -> $packagePath"
}

function BuildWebApp($project, $configuration)
{
    [xml]$xml = Get-Content $project;
    $namespace = @{m = 'http://schemas.microsoft.com/developer/msbuild/2003'}
    $assemblyName = Select-Xml -Xml $xml -Namespace $namespace -XPath '//m:AssemblyName' | %{$_.Node.'#text'}
    $projectPath = Split-Path $project -Parent
    $packagePath = GetPackagePath $configuration $assemblyName
    if (Test-Path "$projectPath\obj\Release\Package")
    {
        Remove-Item "$projectPath\obj\Release\Package" -Recurse
    }

    if (Test-Path "$packagePath")
    {
        Remove-Item "$packagePath" -Recurse
    }

    & $msbuild $project /nologo /v:minimal /t:Package /p:Configuration=Release `
        /p:PackageAsSingleFile=False /p:PackageLocation=$packagePath /p:PackageTraceLevel=Warning /p:DefaultDeployIisAppPath="Default Web Site" `
        /p:ProjectConfigTransformFileName=Web.$configuration.config

    Write-Host "  $assemblyName -> $packagePath"
}

function BuildContainer($project, $configuration, $version)
{
    [xml]$xml = Get-Content $project;
    $namespace = @{m = 'http://schemas.microsoft.com/developer/msbuild/2003'}
    $assemblyName = Select-Xml -Xml $xml -Namespace $namespace -XPath '//m:AssemblyName' | %{$_.Node.'#text'}
    $projectPath = Split-Path $project -Parent
    $packagePath = GetPackagePath $configuration $assemblyName
    $packagePath = Split-Path $packagePath -Parent

    # The docker daemon requires build 14393 or later of Windows Server 2016 or Windows 10
    $docker = "$env:ProgramFiles\Docker\docker.exe"
    if (!(Test-Path $docker)) {
        $docker = "$env:ProgramFiles\Docker\Docker\Resources\bin\docker.exe"
        if (!(Test-Path $docker)) {
            Write-Host "Skipping task because docker is not installed"
            return
        }
    }

    Copy-Item "$projectPath\Dockerfile" -Destination "$packagePath"
    Copy-Item tools -Destination "$packagePath" -Recurse
    $container = GetContainer $assemblyName $configuration $version
    try
    {
        & $docker build --no-cache -t $container "$packagePath"
        Write-Host "  $assemblyName -> $container"
        if ($LASTEXITCODE -ne 0) 
        {
            Write-Warning "Failed to create container $container"
            return
        }
    } catch {
        Write-Error $_.Exception.Message
    }
}

function BuildFabric($project, $configuration, $version)
{
    & $msbuild $project /nologo /v:minimal /t:Package /p:Configuration=$configuration
    $container = GetContainer $assemblyName $configuration $version
    $v = New-Object -TypeName psobject -Property @{
        Repository = 'myregistry-on.azurecr.io'
        AccountName = 'accountName'
        Password = 'EncryptedPassword'
        SecretsCertThumbprint = 'Thumbprint'
        SslCertThumbprint = 'Thumbprint'
    }

    $imageName = "{0}/{1}" -f $v.Repository, $container
    $namespaces = @{}
    $namespaces.Add("f", "http://schemas.microsoft.com/2011/01/fabric")
    $dir = Split-Path $project
    $manifest = Join-Path $dir "pkg\$configuration\ApplicationManifest.xml"
    PokeXml $manifest "/f:ApplicationManifest/@ApplicationTypeVersion" $version $namespaces
    PokeXml $manifest "/f:ApplicationManifest/f:ServiceManifestImport/f:ServiceManifestRef/@ServiceManifestVersion" $version $namespaces
    PokeXml $manifest "/f:ApplicationManifest/f:ServiceManifestImport/f:Policies/f:ContainerHostPolicies/f:RepositoryCredentials/@AccountName" $v.AccountName $namespaces
    PokeXml $manifest "/f:ApplicationManifest/f:ServiceManifestImport/f:Policies/f:ContainerHostPolicies/f:RepositoryCredentials/@Password" $v.Password $namespaces
    PokeXml $manifest "/f:ApplicationManifest/f:Certificates/f:SecretsCertificate/@X509FindValue" $v.SecretsCertThumbprint $namespaces
    PokeXml $manifest "/f:ApplicationManifest/f:Certificates/f:EndpointCertificate/@X509FindValue" $v.SslCertThumbprint $namespaces
    $manifest = Join-Path $dir "pkg\$configuration\Guest1Pkg\ServiceManifest.xml"
    PokeXml $manifest "/f:ServiceManifest/@Version" $version $namespaces
    PokeXml $manifest "/f:ServiceManifest/f:CodePackage/@Version" $version $namespaces
    PokeXml $manifest "/f:ServiceManifest/f:CodePackage/f:EntryPoint/f:ContainerHost/f:ImageName" $imageName $namespaces
    PokeXml $manifest "/f:ServiceManifest/f:ConfigPackage/@Version" $version $namespaces
}

function GetPackagePath($configuration, $assemblyName)
{
    $packagePath = "$env:SystemDrive\ci\$configuration\$assemblyName\Package"
    return $packagePath
}

function GetContainer($name, $configuration, $version) {
    $container = "{0}-{1}:{2}" -f $name, $configuration, $version
    $container = $container.ToLower()
    return $container  
}

function XmlDocTransform($xml, $xdt, $destination)
{
    if (!$xml -or !(Test-Path -path $xml -PathType Leaf))
    {
        throw "File not found. $xml";
    }

    if (!$xdt -or !(Test-Path -path $xdt -PathType Leaf))
    {
        throw "File not found. $xdt";
    }

    Add-Type -LiteralPath $xmlTransformLibrary

    $xmldoc = New-Object Microsoft.Web.XmlTransform.XmlTransformableDocument;
    $xmldoc.PreserveWhitespace = $true
    $xmldoc.Load($xml);

    $transf = New-Object Microsoft.Web.XmlTransform.XmlTransformation($xdt);
    if ($transf.Apply($xmldoc) -eq $false)
    {
        throw "Transformation failed."
    }

    $xmlFilename = Split-Path -Path $xml -Leaf
    $xdtFilename = Split-Path -Path $xdt -Leaf
    Write-Host "  Transformed $xmlFileName using $xdtFilename into $destination."
    $xmldoc.Save($destination);
}

function PokeXml($filePath, $xpath, $value, $namespaces = @{}) {
    [xml] $fileXml = Get-Content $filePath

    if ($namespaces -ne $null -and $namespaces.Count -gt 0) {
        $ns = New-Object Xml.XmlNamespaceManager $fileXml.NameTable
        $namespaces.GetEnumerator() | %{ $ns.AddNamespace($_.Key,$_.Value) }
        $node = $fileXml.SelectSingleNode($xpath,$ns)
    } else {
        $node = $fileXml.SelectSingleNode($xpath)
    }

    if ($node -eq $null) {
        return
    }

    if ($node.NodeType -eq "Element") {
        $node.InnerText = $value
    } else {
        $node.Value = $value
    }

    $fileXml.Save($filePath)
}

Main $task $configuration $version