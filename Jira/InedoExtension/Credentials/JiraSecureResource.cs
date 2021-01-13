using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility.SecureResources;
using Inedo.Extensions.Credentials;
using Inedo.Serialization;

namespace Inedo.Extensions.Jira.Credentials
{
    [DisplayName("JIRA")]
    [Description("JIRA server with user-based authentication.")]
    public sealed class JiraSecureResource : SecureResource<UsernamePasswordCredentials>
    {
        [Required]
        [Persistent]
        [DisplayName("JIRA server URL")]
        public string ServerUrl { get; set; }

        public override RichDescription GetDescription() => new RichDescription("JIRA at ", new Hilite(this.ServerUrl));
    }
}
