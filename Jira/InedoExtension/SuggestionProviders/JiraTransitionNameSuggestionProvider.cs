using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Extensions.Jira.Clients;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraTransitionNameSuggestionProvider : JiraSuggestionProvider
    {
        private protected override async Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config, JiraClient client)
        {
            var project = await client.FindProjectAsync(config["ProjectName"]);
            var transitions = await client.GetTransitionsAsync(new JiraContext(project, null, null));

            return from t in transitions
                   select t.Name;
        }
    }
}
