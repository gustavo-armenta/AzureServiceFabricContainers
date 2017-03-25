# .NET Framework 4.6.1 applications in Service Fabric and Containers

Requisites:
* Visual Studio 2017 Community
* Service Fabric SDK
* Windows Server 2016 with Containers (or Windows 10 build >= 14372 with Docker for Windows containers)

In this sample, you learn how to build .net projects requiring different configuration values for each deployment instance. A deployment instance can be an environment (dev, test, prod), an azure region (SouthCentralUS), a combination (test-scus), or whatever custom thing you need.

In Console Application project, I added files App.Debug.config, App.Release.config, and App.test-scus.config. In Web Application project, I added file Web.test-scus.config. That's it! Run the build task
```
.\ci.ps1 -task Build -configuration Debug -version 0.1
.\ci.ps1 -task Build -configuration Release -version 0.1
.\ci.ps1 -task Build -configuration test-scus -version 0.1
```

The output is located in folders
```
FullFrameworkApps\ConsoleApp1\obj\ci\$configuration\Package
FullFrameworkApps\WebApplication1\obj\ci\$configuration\Package
```

To create container images, I added file Dockerfile on each project. That's it! Run the container task
```
.\ci.ps1 -task Container -configuration Debug -version 0.1
.\ci.ps1 -task Container -configuration Release -version 0.1
.\ci.ps1 -task Container -configuration test-scus -version 0.1
```

The output images are available in your machine
```
PS C:\git\AzureServiceFabricContainers\repo> docker images
REPOSITORY                                              TAG                 IMAGE ID            CREATED             SIZE
webapplication1-test-scus                               0.1                 f5e928ad0603        31 seconds ago      10.8 GB
consoleapp1-test-scus                                   0.1                 0ad531d70d8f        2 minutes ago       9.57 GB
webapplication1-release                                 0.1                 667cc31cb23a        2 minutes ago       10.8 GB
consoleapp1-release                                     0.1                 bd59ec0a4114        4 minutes ago       9.57 GB
webapplication1-debug                                   0.1                 87cd230d497b        4 minutes ago       10.9 GB
consoleapp1-debug                                       0.1                 16ede7fd1fee        6 minutes ago       9.56 GB
```

You need to push the images to a container registry by running commands
```
docker login myregistry-on.azurecr.io
docker tag webapplication1-southcentralus myregistry-on.azurecr.io/webapplication1-southcentralus
docker push myregistry-on.azurecr.io/webapplication1-southcentralus
```

I created FabricApps.sln with two GuestContainer projects.

In FabricApps/ConsoleApp1/ApplicationPackageRoot/ApplicationManifest.xml, I added lines
```
<Policies>
  <ContainerHostPolicies CodePackageRef="Code">
    <RepositoryCredentials AccountName="accountName" PasswordEncrypted="true" Password="EncryptedPassword" />
  </ContainerHostPolicies>
</Policies>
```

In FabricApps/ConsoleApp1/ApplicationPackageRoot/Guest1Pkg/ServiceManifest.xml, there is no change

In FabricApps/WebApplication1/ApplicationPackageRoot/ApplicationManifest.xml, I added lines
```
<Policies>
  <ContainerHostPolicies CodePackageRef="Code">
    <RepositoryCredentials AccountName="accountName" PasswordEncrypted="true" Password="EncryptedPassword" />
    <PortBinding ContainerPort="80" EndpointRef="HttpEndpoint" />
    <PortBinding ContainerPort="443" EndpointRef="HttpsEndpoint" />
  </ContainerHostPolicies>
  <EndpointBindingPolicy CertificateRef="SslCert" EndpointRef="HttpsEndpoint" />
</Policies>

<Principals>
  <Users>
    <User Name="NetworkServiceUser" AccountType="NetworkService" />
  </Users>
</Principals>
<Policies>
  <SecurityAccessPolicies>
    <SecurityAccessPolicy GrantRights="Read" PrincipalRef="NetworkServiceUser" ResourceRef="SecretsCert" ResourceType="Certificate" />
    <SecurityAccessPolicy GrantRights="Read" PrincipalRef="NetworkServiceUser" ResourceRef="SslCert" ResourceType="Certificate" />
  </SecurityAccessPolicies>
</Policies>
<Certificates>
  <SecretsCertificate Name="SecretsCert" X509FindType="FindByThumbprint" X509FindValue="0072E650750495B1D3BE7AF8748ECAEB750C6CB4" />
  <EndpointCertificate Name="SslCert" X509StoreName="My" X509FindValue="B738AE1C2E09343C0194056853AFCA54433C7688" />
</Certificates>
```

In FabricApps/WebApplication1/ApplicationPackageRoot/Guest1Pkg/ServiceManifest.xml, I added lines
```
<Endpoint Name="HttpEndpoint" Protocol="http" UriScheme="http" Port="80" Type="Input" />
<Endpoint Name="HttpsEndpoint" Protocol="https" UriScheme="https" Port="443" Type="Input" />
```

Run the task
```
.\ci.ps1 -task Fabric -configuration Debug -version 0.1
.\ci.ps1 -task Fabric -configuration Release -version 0.1
.\ci.ps1 -task Fabric -configuration test-scus -version 0.1
```

The output is located in folders
```
FabricApps\ConsoleApp1\pkg\$configuration
FabricApps\WebApplication1\pkg\$configuration
```

You might need to use different different container registries and certificates for each deployment instance. Make changes in this section of file ci.ps1
```
$v = New-Object -TypeName psobject -Property @{
    Repository = 'myregistry-on.azurecr.io'
    AccountName = 'accountName'
    Password = 'EncryptedPassword'
    SecretsCertThumbprint = 'Thumbprint'
    SslCertThumbprint = 'Thumbprint'
}
```

**Welcome to the cloud!**
