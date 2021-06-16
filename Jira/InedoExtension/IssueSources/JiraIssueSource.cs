using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.IssueSources;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Jira.Clients;
using Inedo.Extensions.Jira.Credentials;
using Inedo.Extensions.Jira.SuggestionProviders;
using Inedo.Serialization;
using Inedo.Web;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.Jira.IssueSources
{
    [DisplayName("JIRA Issue Source")]
    [Description("Issue source for JIRA.")]
    public sealed class JiraIssueSource : IssueSource<JiraSecureResource>, IMissingPersistentPropertyHandler
    {
        [Persistent]
        [DisplayName("Project name")]
        [SuggestableValue(typeof(JiraProjectNameSuggestionProvider))]
        public string ProjectName { get; set; }
        [Persistent]
        [DisplayName("Fix for version")]
        [SuggestableValue(typeof(JiraFixForVersionSuggestionProvider))]
        public string FixForVersion { get; set; }
        [Persistent]
        [DisplayName("Custom JQL")]
        [PlaceholderText("Use above fields")]
        [FieldEditMode(FieldEditMode.Multiline)]
        [Description("Custom JQL will ignore the project name and 'fix for' versions if supplied. "
            +"See the <a href=\"https://confluence.atlassian.com/jirasoftwarecloud/advanced-searching-764478330.html\">JIRA Advanced searching documentation</a> "
            +"for more information.")]
        public string CustomJql { get; set; }


        void IMissingPersistentPropertyHandler.OnDeserializedMissingProperties(IReadOnlyDictionary<string, string> missingProperties)
        {
            if (string.IsNullOrEmpty(this.ResourceName) && missingProperties.TryGetValue("CredentialName", out var credName))
                this.ResourceName = credName;
            if (string.IsNullOrEmpty(this.ResourceName) && missingProperties.TryGetValue("Credentials", out var credsName))
                this.ResourceName = credsName;

        }

        private (JiraSecureResource resource, UsernamePasswordCredentials secureCredentials) GetResourceAndCredentials(IIssueSourceEnumerationContext context)
        {
            JiraSecureResource resource = null;
            SecureCredentials credentials = null;

            var resolutionConext = new CredentialResolutionContext(context.ProjectId, null);

            if (!string.IsNullOrEmpty(this.ResourceName))
            {
                resource = SecureResource.TryCreate(this.ResourceName, resolutionConext) as JiraSecureResource;
                if (resource == null)
                {
                    var legacy = ResourceCredentials.TryCreate<JiraLegacyCredentials>(this.ResourceName);
                    resource = legacy?.ToSecureResource() as JiraSecureResource;
                    credentials = legacy?.ToSecureCredentials();
                }
                else
                {
                    credentials = resource.GetCredentials(resolutionConext);
                }
            }

            return (resource, credentials as UsernamePasswordCredentials);
        }

        public async override Task<IEnumerable<IIssueTrackerIssue>> EnumerateIssuesAsync(IIssueSourceEnumerationContext context)
        {
            var resource = GetResourceAndCredentials(context);

            if (resource.resource == null || resource.secureCredentials == null)
                throw new InvalidOperationException("Credentials must be supplied to enumerate JIRA issues.");
           
            if (string.IsNullOrEmpty(this.ProjectName) && string.IsNullOrEmpty(this.FixForVersion) && string.IsNullOrEmpty(this.CustomJql))
                throw new InvalidOperationException("Cannot enumerate JIRA issues unless either a project name, fix version, or custom JQL is specified.");
            if (resource.secureCredentials.Password == null)
                throw new InvalidOperationException("A credential password is required to enumerate JIRA issues.");

            var client = JiraClient.Create(resource.resource.ServerUrl, resource.secureCredentials, context.Log);

            var project = await client.FindProjectAsync(this.ProjectName);

            var issueContext = new JiraContext(project, this.FixForVersion, this.CustomJql);

            var issues = await client.EnumerateIssuesAsync(issueContext).ConfigureAwait(false);
            return issues;
        }

        public override RichDescription GetDescription()
        {
            if (!string.IsNullOrEmpty(this.CustomJql))
                return new RichDescription("Get Issues from JIRA Using Custom JQL");
            else
                return new RichDescription(
                    "Get Issues from ", new Hilite(this.ProjectName), " in JIRA for version ", new Hilite(this.FixForVersion)
                );
        }
    }
}
