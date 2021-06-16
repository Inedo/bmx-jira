using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraProjectNameSuggestionProvider : JiraSuggestionProvider
    {
        internal override async Task<IEnumerable<string>> GetSuggestionsAsync()
        {
            var empty = new[] { "$ApplicationName" };


            if (this.Resource == null || this.Credentials == null)
                return empty;
            var proj = from p in await this.Client.GetProjectsAsync()
                       select p.Name;

            return empty.Concat(proj);
        }
    }
}
