using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraIssueTypeNameSuggestionProvider : JiraSuggestionProvider
    {
        internal override async Task<IEnumerable<string>> GetSuggestionsAsync()
        {
            var empty = Enumerable.Empty<string>();

            if (this.Resource == null || this.Credentials == null)
                return empty;

            var project = await this.Client.FindProjectAsync(this.ComponentConfiguration["ProjectName"]);

            var types = from t in await this.Client.GetIssueTypesAsync(project?.Id)
                        select t.Name;

            return types;
        }
    }
}
