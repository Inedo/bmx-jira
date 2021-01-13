using Newtonsoft.Json.Linq;

namespace Inedo.Extensions.Jira.RestApi
{
    internal sealed class Permission
    {
        public Permission(JObject permission)
        {
            this.Key = (string)permission.Property("key");
            this.HasPermission = (bool?)permission.Property("havePermission") ?? false;
        }

        public string Key { get; }
        public bool HasPermission { get; }
    }
}
