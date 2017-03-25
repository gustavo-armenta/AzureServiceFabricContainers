namespace AzureResourceManagerTemplates
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ARMDeployment deployment = new ARMDeployment("test", "scus");
            deployment.Deploy("01_KeyVault", "keyVault");
            //deployment.Deploy("02a_VirtualNetwork", "vnet");
            //update sslCertificateThumbprint, clientCertificateThumbprints, vaultCertificates in parameters file before running
            //deployment.Deploy("03_Fabric", "fabric");
            //deployment.Deploy("04_ContainerRegistry", "registry");
            //Invoke-ServiceFabricEncryptText -CertStore -CertThumbprint "<thumbprint>" -Text "mysecret" -StoreLocation CurrentUser -StoreName My
            //deployment.Deploy("05_ServiceBus", "servicebus");
        }
    }
}
