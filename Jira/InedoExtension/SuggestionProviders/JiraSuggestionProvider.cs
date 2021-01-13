using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Jira.Clients;
using Inedo.Extensions.Jira.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public abstract class JiraSuggestionProvider : ISuggestionProvider
    {
        private protected JiraSuggestionProvider()
        {
        }

        private protected virtual string Empty => null;

        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            if (config == null)
                return this.GetEmpty();

            string resourceName = config["ResourceName"];
            if (string.IsNullOrEmpty(resourceName))
                return this.GetEmpty();

            int? projectId = AH.ParseInt(AH.CoalesceString(config["ProjectId"], config["ApplicationId"]));

            if (SecureResource.TryCreate(resourceName, new ResourceResolutionContext(projectId)) is not JiraSecureResource resource)
                return this.GetEmpty();

            if (resource.GetCredentials(new CredentialResolutionContext(projectId, null)) is not Extensions.Credentials.UsernamePasswordCredentials credentials)
                return this.GetEmpty();

            using var client = JiraClient.Create(resource.ServerUrl, credentials.UserName, AH.Unprotect(credentials.Password));
            return (await this.GetSuggestionsAsync(config, client)).ToList();
        }

        private protected abstract Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config, JiraClient client);
        private protected IEnumerable<string> GetEmpty() => this.Empty == null ? Enumerable.Empty<string>() : new[] { this.Empty };
    }
}
