using System;
using Inedo.Extensibility.IssueSources;
using Newtonsoft.Json.Linq;

namespace Inedo.Extensions.Jira.RestApi
{
    internal sealed class Issue : IIssueTrackerIssue
    {
        public Issue(JObject issue, string hostUrl)
        {
            if (issue == null)
                throw new ArgumentNullException(nameof(issue));
            if (hostUrl == null)
                throw new ArgumentNullException(nameof(hostUrl));

            var fields = (JObject)issue["fields"];
            var status = (JObject)fields["status"];
            var reporter = (JObject)fields["reporter"];
            var type = (JObject)fields["issuetype"];

            this.Id = issue["key"].ToString();
            this.Description = (string)fields.Property("description");
            this.Status = (string)status.Property("name");
            this.IsClosed = fields.Property("resolution") != null;
            this.SubmittedDate = (DateTime)fields.Property("created");
            this.Submitter = (string)reporter.Property("name");
            this.Title = (string)fields.Property("summary");
            this.Type = (string)type.Property("name");
            this.Url = hostUrl + "/browse/" + this.Id;
        }

        public string Id { get; }
        public string Description { get; }
        public bool IsClosed { get; }
        public string Status { get; }
        public DateTime SubmittedDate { get; }
        public string Submitter { get; }
        public string Title { get; }
        public string Type { get; }
        public string Url { get; }
    }
}
