using System.Collections.Generic;
using System.ComponentModel;
using System.Security;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.Operations;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Jira.Clients;
using Inedo.Extensions.Jira.Credentials;
using Inedo.Serialization;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.Jira.Operations
{
    public abstract class JiraOperation : ExecuteOperation, IMissingPersistentPropertyHandler
    {
        private protected JiraOperation()
        {
        }

        public abstract string ResourceName { get; set; }

        [Category("Connection")]
        [ScriptAlias("Server")]
        [PlaceholderText("Use server URL from resource")]
        public string ServerUrl { get; set; }

        [Category("Connection")]
        [ScriptAlias("UserName")]
        [PlaceholderText("Use user name from resource's credentials")]
        public string UserName { get; set; }

        [Category("Connection")]
        [ScriptAlias("Password")]
        [PlaceholderText("Use password from resource's credentials")]
        public SecureString Password { get; set; }

        void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IReadOnlyDictionary<string, string> missingProperties)
        {
            if (string.IsNullOrEmpty(this.ResourceName) && missingProperties.TryGetValue("CredentialName", out var credName))
                this.ResourceName = credName;
            if (string.IsNullOrEmpty(this.ResourceName) && missingProperties.TryGetValue("Credentials", out var credsName))
                this.ResourceName = credsName;
        }

        protected (JiraSecureResource resource, SecureCredentials secureCredentials) GetResourceAndCredentials(IOperationExecutionContext context)
        {
            JiraSecureResource resource = null;
            SecureCredentials credentials = null;

            if (!string.IsNullOrEmpty(this.ResourceName))
            {
                resource = SecureResource.TryCreate(this.ResourceName, context as IResourceResolutionContext) as JiraSecureResource;
                if (resource == null)
                {
                    var legacy = ResourceCredentials.TryCreate<JiraLegacyCredentials>(this.ResourceName);
                    resource = legacy?.ToSecureResource() as JiraSecureResource;
                    credentials = legacy?.ToSecureCredentials();
                }
                else
                {
                    credentials = resource.GetCredentials((context as ICredentialResolutionContext));
                }
            }
            if (!string.IsNullOrEmpty(this.UserName) && this.Password != null)
            {
                credentials = new UsernamePasswordCredentials
                {
                    UserName = this.UserName,
                    Password = this.Password
                };
            }

            return (new JiraSecureResource()
            {
                ServerUrl = AH.CoalesceString(this.ServerUrl, resource?.ServerUrl)
            }, credentials);
        }

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
