using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Resource.Fluent;
using Microsoft.Azure.Management.Resource.Fluent.Authentication;
using Microsoft.Azure.Management.Resource.Fluent.Core;
using Microsoft.Azure.Management.Resource.Fluent.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace AzureResourceManagerTemplates
{
    public class ARMDeployment
    {
        private string config;
        private string env;
        private string location;
        private Dictionary<string, Region> regions;
        private IAzure azure;

        public ARMDeployment(string env, string loc)
        {
            this.config = "config";
            this.env = env;
            this.location = loc;
            this.regions = new Dictionary<string, Region>();
            this.regions.Add("scus", Region.USSouthCentral);
            this.Authenticate();
        }

        private void Authenticate()
        {
            string passwordsString = File.ReadAllText(Path.Combine(config, "passwords.json"));
            JObject json = JObject.Parse(passwordsString);
            var user = new ServicePrincipalLoginInformation();
            user.ClientId = json["clientId"].Value<string>();
            user.ClientSecret = json["clientSecret"].Value<string>();
            string tenantId = json["tenantId"].Value<string>();
            var env = json["azureEnvironment"].ToObject<AzureEnvironment>();
            var credentials = new AzureCredentials(user, tenantId, env);
            azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.BASIC)
                .Authenticate(credentials)
                .WithDefaultSubscription();
        }

        public void Deploy(string app, string deploymentName)
        {
            string resourceGroupName = string.Format("{0}-{1}", env, location);
            string service = string.Format("{0}-{1}", resourceGroupName, deploymentName);
            string templateString = File.ReadAllText(Path.Combine(config, app, "template.json"));
            string parametersString = File.ReadAllText(Path.Combine(config, app, string.Format("parameters-{0}.json", resourceGroupName)));
            JObject parametersJson = JObject.Parse(parametersString);

            azure.ResourceGroups.Define(resourceGroupName)
                .WithRegion(regions[location])
                .Create();

            Console.WriteLine("Deploying {0}", service);
            azure.Deployments.Define(service)
                .WithExistingResourceGroup(resourceGroupName)
                .WithTemplate(templateString)
                .WithParameters(parametersJson["parameters"].ToString())
                .WithMode(DeploymentMode.Incremental)
                .Create();
        }
    }
}
