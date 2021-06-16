using System.ComponentModel;
using Inedo.Documentation;
using Inedo.Extensibility.SecureResources;
using Inedo.Serialization;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.Jira.Credentials
{
    public class JiraSecureResource : SecureResource<UsernamePasswordCredentials>
    {

        [Required]
        [Persistent]
        [DisplayName("JIRA server URL")]
        public string ServerUrl { get; set; }

        public override RichDescription GetDescription() => new RichDescription(this.ServerUrl);
    }
}
