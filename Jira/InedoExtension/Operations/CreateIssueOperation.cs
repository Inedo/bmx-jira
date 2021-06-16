using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Jira.Clients;
using Inedo.Extensions.Jira.Credentials;
using Inedo.Extensions.Jira.SuggestionProviders;
using Inedo.Web;

namespace Inedo.Extensions.Jira.Operations
{
    [DisplayName("Create Jira Issue")]
    [Description("Creates an issue in Jira.")]
    [Tag("issue-tracking")]
    [ScriptAlias("Create-Issue")]
    [Example(@"
# create issue for the HDARS project notifying QA that testing is required
Create-Issue(
    ResourceName: Jira7Local,
    Project: HDARS,
    Title: QA Testing Required for $ApplicationName,
    Description: This issue was created by BuildMaster on $Date,
    Type: ReadyForQA,
    JiraIssueId => $JiraIssueId
);

Log-Information ""Issue '$JiraIssueId' was created in JIRA."";
")]
    public sealed class CreateIssueOperation : JiraOperation
    {
        [DisplayName("From Jira resource")]
        [SuggestableValue(typeof(SecureResourceSuggestionProvider<JiraSecureResource>))]
        [Required]
        public override string ResourceName { get; set; }
        [Required]
        [ScriptAlias("Project")]
        [DisplayName("Project name")]
        [SuggestableValue(typeof(JiraProjectNameSuggestionProvider))]
        public string ProjectName { get; set; }
        [Required]
        [ScriptAlias("Title")]
        [DisplayName("Title")]
        public string Title { get; set; }
        [Required]
        [ScriptAlias("Type")]
        [DisplayName("Type")]
        [SuggestableValue(typeof(JiraIssueTypeNameSuggestionProvider))]
        public string Type { get; set; }
        [ScriptAlias("Description")]
        [DisplayName("Description")]
        public string Description { get; set; }
        [ScriptAlias("FixFor")]
        [DisplayName("Fix for version")]
        [PlaceholderText("$ReleaseNumber")]
        [SuggestableValue(typeof(JiraFixForVersionSuggestionProvider))]
        public string FixForVersion { get; set; }

        [Output]
        [ScriptAlias("JiraIssueId")]
        [DisplayName("Set issue ID to a variable")]
        [Description("The JIRA issue ID can be output into a runtime variable.")]
        [PlaceholderText("e.g. $JiraIssueId")]
        public string JiraIssueId { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation("Creating JIRA issue...");
            var resource = this.GetResourceAndCredentials(context);

            var client = JiraClient.Create(resource.resource.ServerUrl, resource.secureCredentials, this);

            var project = await this.ResolveProjectAsync(client, this.ProjectName);
            if (project == null)
                return;

            var type = await this.ResolveIssueTypeAsync(client, project, this.Type);
            if (type == null)
                return;

            var fixForVersion = this.FixForVersion ?? context.ExpandVariables("$ReleaseNumber").AsString();

            var issue = await client.CreateIssueAsync(
                new JiraContext(project, fixForVersion, null),
                this.Title,
                this.Description,
                type.Id
            );

            this.JiraIssueId = issue.Id;

            this.LogInformation($"JIRA issue '{issue.Id}' created.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Create Jira Issue for project ", config[nameof(this.ProjectName)])
            );
        }
    }
}
