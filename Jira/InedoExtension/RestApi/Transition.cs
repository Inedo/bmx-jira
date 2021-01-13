using Newtonsoft.Json.Linq;

namespace Inedo.Extensions.Jira
{
    internal sealed class Transition
    {
        public Transition(JObject transition)
        {
            this.Id = (string)transition.Property("id");
            this.Name = (string)transition.Property("name");
        }

        public string Id { get; }
        public string Name { get; }
    }
}
