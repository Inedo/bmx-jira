using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensions.Jira.Clients;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraIssueTypeNameSuggestionProvider : JiraSuggestionProvider
    {
        private protected override async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config, JiraClient client)
        {
            var project = await client.FindProjectAsync(config["ProjectName"]);

            return from t in await client.GetIssueTypesAsync(project?.Id)
                   select t.Name;
        }
    }
}
