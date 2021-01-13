using System.ComponentModel;
using System.Security;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Jira.Clients;
using Inedo.Extensions.Jira.Credentials;

namespace Inedo.Extensions.Jira.Operations
{
    public abstract class JiraOperation : ExecuteOperation, IHasCredentials<JiraCredentials>
    {
        private protected JiraOperation()
        {
        }

        public abstract string CredentialName { get; set; }

        [Category("Connection")]
        [ScriptAlias("Server")]
        [PlaceholderText("Use server URL from secure resource")]
        public string ServerUrl { get; set; }

        [Browsable(false)]
        [Category("Connection")]
        [ScriptAlias("UserName")]
        [PlaceholderText("Use user name from credential")]
        public string UserName { get; set; }
        [Browsable(false)]
        [Category("Connection")]
        [ScriptAlias("Password")]
        [PlaceholderText("Use password from credential")]
        public SecureString Password { get; set; }

        internal async Task<JiraProject> ResolveProjectAsync(JiraClient client, string name)
        {
            this.LogDebug($"Resolving project name '{name}'...");
            var project = await client.FindProjectAsync(name);

            if (project == null)
            {
                this.LogError($"Project '{name}' was not found.");
                return null;
            }

            this.LogDebug($"Project resolved to key='{project.Key}', id='{project.Id}'.");

            return project;
        }

        internal async Task<JiraIssueType> ResolveIssueTypeAsync(JiraClient client, JiraProject project, string issueTypeName)
        {
            this.LogDebug($"Resolving issue type name '{issueTypeName}' for project '{project.Name}'...");
            var issueType = await client.FindIssueTypeAsync(project.Id, issueTypeName);

            if (issueType == null)
            {
                this.LogError($"Issue type '{issueTypeName}' was not found in project '{project.Name}'.");
                return null;
            }

            this.LogDebug($"Issue type ID resolved to '{issueType.Id}'.");

            return issueType;
        }
    }
}
