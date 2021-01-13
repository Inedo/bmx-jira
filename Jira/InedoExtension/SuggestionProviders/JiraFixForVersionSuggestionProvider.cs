using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensions.Jira.Clients;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraFixForVersionSuggestionProvider : JiraSuggestionProvider
    {
        private protected override string Empty => "$ReleaseNumber";

        private protected async override Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config, JiraClient client)
        {
            string projectName = config["ProjectName"];
            if (string.IsNullOrEmpty(projectName))
                return this.GetEmpty();

            var proj = (await client.GetProjectsAsync()).FirstOrDefault(p => string.Equals(projectName, p.Name, StringComparison.OrdinalIgnoreCase));
            if (proj == null)
                return this.GetEmpty();

            var versions = from v in await client.GetProjectVersionsAsync(proj.Key)
                           select v.Name;

            return new[] { this.Empty }.Concat(versions);
        }
    }
}
