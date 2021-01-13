using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensions.Jira.Clients;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraProjectNameSuggestionProvider : JiraSuggestionProvider
    {
        private protected override string Empty => "$ApplicationName";

        private protected override async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config, JiraClient client)
        {
            var proj = from p in await client.GetProjectsAsync()
                       select p.Name;

            return new[] { this.Empty }.Concat(proj);
        }
    }
}
