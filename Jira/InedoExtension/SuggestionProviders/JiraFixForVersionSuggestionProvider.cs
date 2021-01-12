using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensions.Jira.Clients;
using Inedo.Extensions.Jira.Credentials;
using Inedo.Web;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraFixForVersionSuggestionProvider : ISuggestionProvider
    {
        public async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var empty = new[] { "$ReleaseNumber" };

            if (config == null)
                return empty;

            string credentialName = config["CredentialName"];
            if (string.IsNullOrEmpty(credentialName))
                return empty;

            string projectName = config["ProjectName"];
            if (string.IsNullOrEmpty(projectName))
                return empty;

            var credential = JiraCredentials.TryCreate(credentialName, config);
            if (credential == null)
                return empty;

            var client = JiraClient.Create(credential.ServerUrl, credential.UserName, AH.Unprotect(credential.Password));
            var proj = (await client.GetProjectsAsync()).FirstOrDefault(p => string.Equals(projectName, p.Name, StringComparison.OrdinalIgnoreCase));
            if (proj == null)
                return empty;

            var versions = from v in await client.GetProjectVersionsAsync(proj.Key)
                           select v.Name;

            return empty.Concat(versions);
        }
    }
}
