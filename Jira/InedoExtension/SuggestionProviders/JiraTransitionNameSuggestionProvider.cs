using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Extensions.Jira.Clients;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraTransitionNameSuggestionProvider : JiraSuggestionProvider
    {
        internal override async Task<IEnumerable<string>> GetSuggestionsAsync()
        {
            var empty = Enumerable.Empty<string>();

            if (this.Resource == null || this.Credentials == null)
                return empty;
            
            var project = await this.Client.FindProjectAsync(this.ComponentConfiguration["ProjectName"]);

            var transitions = await this.Client.GetTransitionsAsync(new JiraContext(project, null, null));

            var names = from t in transitions
                        select t.Name;

            return names;
        }
    }
}
