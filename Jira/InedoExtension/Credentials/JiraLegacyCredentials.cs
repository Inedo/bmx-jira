using System.ComponentModel;
using System.Security;
using Inedo.Documentation;
using Inedo.Extensibility;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.SecureResources;
using Inedo.Serialization;
using Inedo.Web;
using Inedo.Web.Plans;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.Jira.Credentials
{
    [ScriptAlias(JiraLegacyCredentials.TypeName)]
    [DisplayName("JIRA")]
    [Description("(Legacy) Credentials for JIRA.")]
    [PersistFrom("Inedo.BuildMasterExtensions.Jira.Credentials.JiraCredentials,Jira")]
    [PersistFrom("Inedo.OtterExtensions.Jira.Credentials.JiraCredentials,Jira")]
    [PersistFrom("Inedo.Extensions.Jira.Credentials.JiraCredentials,Jira")]
    public sealed class JiraLegacyCredentials : ResourceCredentials
    {
        public const string TypeName = "Jira";

        [Required]
        [Persistent]
        [DisplayName("JIRA server URL")]
        public string ServerUrl { get; set; }

        [Persistent]
        [DisplayName("User name")]
        [PlaceholderText("Anonymous")]
        public string UserName { get; set; }

        [Persistent(Encrypted = true)]
        [DisplayName("Password")]
        [FieldEditMode(FieldEditMode.Password)]
        public SecureString Password { get; set; }

        public override RichDescription GetDescription() => new RichDescription(this.UserName, " @ ", this.ServerUrl);

        public override SecureResource ToSecureResource() => new JiraSecureResource { ServerUrl = this.ServerUrl };

        public override SecureCredentials ToSecureCredentials()
        {
            if (this.UserName != null)
                return new UsernamePasswordCredentials { UserName = this.UserName, Password = this.Password };
            else
                return null;
        }
    }
}
