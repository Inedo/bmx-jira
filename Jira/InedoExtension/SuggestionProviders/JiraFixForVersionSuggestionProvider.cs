using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inedo.Extensions.Jira.SuggestionProviders
{
    public sealed class JiraFixForVersionSuggestionProvider : JiraSuggestionProvider
    {
        internal override async Task<IEnumerable<string>> GetSuggestionsAsync()
        {
            var empty = new[] { "$ReleaseNumber" };

            string projectName = this.ComponentConfiguration["ProjectName"];
            if (string.IsNullOrEmpty(projectName))
                return empty;

            if (this.Resource == null || this.Credentials == null)
                return empty;

            var proj = (await this.Client.GetProjectsAsync()).FirstOrDefault(p => string.Equals(projectName, p.Name, StringComparison.OrdinalIgnoreCase));
            if (proj == null)
                return empty;

            var versions = from v in await this.Client.GetProjectVersionsAsync(proj.Key)
                           select v.Name;

            return empty.Concat(versions);
        }
    }
}
