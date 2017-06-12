using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
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
            this.env = env;
            this.location = loc;
            this.regions = new Dictionary<string, Region>();
            this.regions.Add("cn", Region.AsiaEast);
            this.regions.Add("my", Region.AsiaSouthEast);
            this.regions.Add("eau", Region.AustraliaEast);
            this.regions.Add("seau", Region.AustraliaSouthEast);
            this.regions.Add("sbr", Region.BrazilSouth);
            this.regions.Add("cca", Region.CanadaCentral);
            this.regions.Add("eca", Region.CanadaEast);
            this.regions.Add("ie", Region.EuropeNorth);
            this.regions.Add("nl", Region.EuropeWest);
            this.regions.Add("cin", Region.IndiaCentral);
            this.regions.Add("sin", Region.IndiaSouth);
            this.regions.Add("win", Region.IndiaWest);
            this.regions.Add("ejp", Region.JapanEast);
            this.regions.Add("wjp", Region.JapanWest);
            this.regions.Add("ckr", Region.KoreaCentral);
            this.regions.Add("skr", Region.KoreaSouth);
            this.regions.Add("sgb", Region.UKSouth);
            this.regions.Add("wgb", Region.UKWest);
            this.regions.Add("cus", Region.USCentral);
            this.regions.Add("eus", Region.USEast);
            this.regions.Add("e2us", Region.USEast2);
            this.regions.Add("ncus", Region.USNorthCentral);
            this.regions.Add("scus", Region.USSouthCentral);
            this.regions.Add("wus", Region.USWest);
            this.regions.Add("w2us", Region.USWest2);
            this.regions.Add("wcus", Region.USWestCentral);

            // China cloud
            this.regions.Add("echn", Region.ChinaEast);
            this.regions.Add("nchn", Region.ChinaNorth);

            this.config = "config";
            Region region = this.regions[location];
            if (region == Region.ChinaEast ||
                region == Region.ChinaNorth)
            {
                this.config = "china";
            }

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
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
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
