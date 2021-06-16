using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Jira.Clients;
using Inedo.Extensions.Jira.Credentials;
using Inedo.Extensions.Jira.Operations;
using Inedo.Web;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.Jira.SuggestionProviders
{

    public abstract class JiraSuggestionProvider : ISuggestionProvider
    {
        protected IComponentConfiguration ComponentConfiguration { get; private set; }
        protected UsernamePasswordCredentials Credentials { get; private set; }
        protected JiraSecureResource Resource { get; private set; }

        internal JiraClient Client { get; private set; }
        internal abstract Task<IEnumerable<string>> GetSuggestionsAsync();

        public Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
           var context = config.EditorContext as ICredentialResolutionContext;

            var credentialName = config[nameof(JiraSecureResource.CredentialName)];
            if (!string.IsNullOrEmpty(credentialName))
                this.Credentials = SecureCredentials.TryCreate(credentialName, context) as UsernamePasswordCredentials;

            var resourceName = config[nameof(JiraOperation.ResourceName)];
            if (!string.IsNullOrEmpty(resourceName))
                this.Resource = SecureResource.TryCreate(resourceName, context) as JiraSecureResource;
            else if (this.Credentials == null)
            {
                var rc = SecureCredentials.TryCreate(credentialName, context) as JiraLegacyCredentials;
                this.Resource = (JiraSecureResource)rc?.ToSecureResource();
                this.Credentials = (UsernamePasswordCredentials)rc?.ToSecureCredentials();
            }

            if (this.Credentials == null && this.Resource != null)
                this.Credentials = this.Resource.GetCredentials(context) as UsernamePasswordCredentials;

            if (this.Credentials != null)
                this.Client = JiraClient.Create(this.Resource.ServerUrl, this.Credentials);

            this.ComponentConfiguration = config;

            return this.GetSuggestionsAsync();
        }
    }
}
