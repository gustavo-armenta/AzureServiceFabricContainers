namespace AzureResourceManagerTemplates
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ARMDeployment deployment = new ARMDeployment("test", "scus");
            //deployment.Deploy("01_KeyVault", "keyVault");
            //deployment.Deploy("02a_VirtualNetwork", "vnet");
            deployment.Deploy("03_AppInsights", "app-insights");
            //deployment.Deploy("04_SQL", "sql");
            //deployment.Deploy("04_SQLAlwaysOn", "sql");
            //deployment.Deploy("05_ServiceBus", "servicebus");
            //deployment.Deploy("20_ContainerRegistry", "registry");
            //update sslCertificateThumbprint, clientCertificateThumbprints, vaultCertificates in parameters file before running
            //deployment.Deploy("21_Fabric", "fabric");
            //Invoke-ServiceFabricEncryptText -CertStore -CertThumbprint "<thumbprint>" -Text "mysecret" -StoreLocation CurrentUser -StoreName My
        }
    }
}
