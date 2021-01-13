using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Operations;
using Inedo.Extensions.Jira.Clients;
using Inedo.Extensions.Jira.SuggestionProviders;
using Inedo.Web;

namespace Inedo.Extensions.Jira.Operations
{
    [DisplayName("Transition Jira Issues")]
    [Description("Transitions issues in JIRA.")]
    [Tag("issue-tracking")]
    [ScriptAlias("Transition-Issues")]
    [Example(@"
# closes issues for the HDARS project for the current release
Transition-Issues(
    Credentials: Jira7Local,
    Project: HDARS,
    From: QA-InProgress,
    To: Closed
);
")]
    public sealed class TransitionIssuesOperation : JiraOperation
    {
        [ScriptAlias("Credentials")]
        [DisplayName("Credentials")]
        public override string CredentialName { get; set; }
        [Required]
        [ScriptAlias("Project")]
        [DisplayName("Project name")]
        [SuggestableValue(typeof(JiraProjectNameSuggestionProvider))]
        public string ProjectName { get; set; }

        [ScriptAlias("From")]
        [DisplayName("From")]
        [PlaceholderText("Any status")]
        public string FromStatus { get; set; }
        [Required]
        [ScriptAlias("To")]
        [DisplayName("To")]
        [SuggestableValue(typeof(JiraTransitionNameSuggestionProvider))]
        public string ToStatus { get; set; }
        [Required]
        [ScriptAlias("FixFor")]
        [DisplayName("With fix for version")]
        [DefaultValue("$ReleaseNumber")]
        [SuggestableValue(typeof(JiraFixForVersionSuggestionProvider))]
        public string FixForVersion { get; set; } = "$ReleaseNumber";
        [ScriptAlias("Id")]
        [DisplayName("Specific issue ID")]
        [PlaceholderText("Any")]
        [Description("If an issue ID is supplied, other filters will be ignored.")]
        public string IssueId { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
        {
            this.LogInformation($"Transitioning JIRA issue status {(this.IssueId != null ? $"of issue ID '{this.IssueId}' " : " ")}from '{(string.IsNullOrEmpty(this.FromStatus) ? "<any status>" : this.FromStatus)}' to '{this.ToStatus}'...");

            var client = JiraClient.Create(this.ServerUrl, this.UserName, AH.Unprotect(this.Password), this);

            var project = await this.ResolveProjectAsync(client, this.ProjectName);
            if (project == null)
                return;

            var fixForVersion = this.FixForVersion;

            var jiraContext = new JiraContext(project, fixForVersion, null);

            if (this.IssueId != null)
            {
                await client.TransitionIssueAsync(jiraContext, this.IssueId, this.ToStatus);
            }
            else
            {
                await client.TransitionIssuesInStatusAsync(jiraContext, this.FromStatus, this.ToStatus);
            }

            this.LogInformation("Issue(s) transitioned.");
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription("Transition Jira Issues for project ", config[nameof(this.ProjectName)])
            );
        }
    }
}
