using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensions.Jira.Clients;
using Inedo.Extensions.Jira.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraTransitionNameSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var empty = Enumerable.Empty<string>();

            if (config == null)
                return empty;

            string credentialName = config["CredentialName"];
            if (string.IsNullOrEmpty(credentialName))
                return empty;

            var credential = JiraCredentials.TryCreate(credentialName, config);
            if (credential == null)
                return empty;

            var client = JiraClient.Create(credential.ServerUrl, credential.UserName, AH.Unprotect(credential.Password));
            var project = await client.FindProjectAsync(config["ProjectName"]);

            var transitions = await client.GetTransitionsAsync(new JiraContext(project, null, null));

            var names = from t in transitions
                        select t.Name;

            return names;
        }
    }
}
