using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Inedo.Extensibility.IssueSources;
using Inedo.Extensions.Jira.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Inedo.Extensions.Jira.RestApi
{
    internal sealed class RestApiClient
    {
        private readonly string host;
        private readonly string apiBaseUrl;

        public RestApiClient(string host)
        {
            this.host = host.TrimEnd('/');
            this.apiBaseUrl = this.host + "/rest/api/2/";
        }

        public string UserName { get; set; }
        public string Password { get; set; }

        public async Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            var results = (JObject)await this.InvokeAsync("GET", "mypermissions");
            var permissions = (JObject)results["permissions"];
            return permissions.Properties().Select(p => new Permission(p.Value<JObject>()));
        }

        public Task AddCommentAsync(string issueKey, string comment) => this.InvokeAsync("POST", $"issue/{issueKey}/comment", data: new JObject { ["body"] = comment });

        public async Task<IEnumerable<JiraProject>> GetProjectsAsync()
        {
            var results = (IEnumerable<object>)await this.InvokeAsync("GET", "project");
            return results.Select(p => new JiraProject((Dictionary<string, object>)p));
        }

        public async Task<IEnumerable<ProjectVersion>> GetVersionsAsync(string projectKey)
        {
            var results = (IEnumerable<object>)await this.InvokeAsync(
                "GET",
                $"project/{projectKey}/versions"
            );

            return results.Select(v => ProjectVersion.Parse((Dictionary<string, object>)v)).Where(v => v != null);
        }

        public Task TransitionIssueAsync(string issueKey, string transitionId)
        {
            return this.InvokeAsync(
                "POST",
                $"issue/{issueKey}/transitions",
                data: new JObject
                {
                    ["transition"] = new JObject { ["id"] = transitionId }
                }
            );
        }

        public async Task<IEnumerable<Transition>> GetTransitionsAsync(string issueKey)
        {
            var results = (JObject)await this.InvokeAsync(
                "GET",
                $"issue/{issueKey}/transitions"
            );

            var transitions = ((JArray)results["transitions"]).OfType<JObject>();
            return transitions.Select(t => new Transition(t));
        }

        public Task ReleaseVersionAsync(string projectKey, string versionId)
        {
            return this.InvokeAsync(
                "PUT",
                $"version/{versionId}",
                data: new JObject
                {
                    ["released"] = true,
                    ["releaseDate"] = DateTime.Now
                }
            );
        }

        public Task CreateVersionAsync(string projectKey, string versionNumber)
        {
            return this.InvokeAsync(
                "POST",
                "version",
                data: new JObject
                {
                    ["name"] = versionNumber,
                    ["project"] = projectKey
                }
            );
        }

        public async Task<Issue> CreateIssueAsync(string projectKey, string summary, string description, string issueTypeId, string fixForVersion)
        {
            var fixVersions = new JArray();
            if (!string.IsNullOrEmpty(fixForVersion))
            {
                var version = (await this.GetVersionsAsync(projectKey)).FirstOrDefault(v => v.Name == fixForVersion);
                if (version != null)
                    fixVersions.Add(new JObject { ["id"] = version.Id });
            }

            var result = (JObject)await this.InvokeAsync(
                "POST",
                "issue",
                data: new JObject
                {
                    ["fields"] = new JObject
                    {
                        ["project"] = new JObject
                        {
                            ["key"] = projectKey
                        },
                        ["summary"] = summary,
                        ["description"] = description,
                        ["issuetype"] = new JObject
                        {
                            ["id"] = issueTypeId
                        },
                        ["fixVersions"] = fixVersions
                    }
                }
            );

            return await this.GetIssueAsync(result["key"].ToString());
        }

        public async Task<Issue> GetIssueAsync(string issueKey)
        {
            var result = (JObject)await this.InvokeAsync(
                "GET",
                $"issue/{issueKey}"
            );

            return new Issue(result, this.host);
        }

        public async Task<IEnumerable<JiraIssueType>> GetIssueTypes(string projectId)
        {
            QueryString query = null;
            if (projectId != null)
                query = new QueryString { Jql = $"project='{projectId}'" };

            var result = (IEnumerable<object>)await this.InvokeAsync("GET", "issuetype", query);
            return result.Select(t => new JiraIssueType((Dictionary<string, object>)t));
        }

        public async Task<IEnumerable<Issue>> GetIssuesAsync(string projectKey, string versionName)
        {
            var result = (JObject)await this.InvokeAsync(
                "GET",
                "search",
                new QueryString { Jql = $"project='{projectKey}' and fixVersion='{versionName}'" }
            );

            var issues = ((JArray)result["issues"]).OfType<JObject>();
            return issues.Select(i => new Issue(i, this.host));
        }

        public async Task<IEnumerable<IIssueTrackerIssue>> GetIssuesAsync(string jql)
        {
            var result = (JObject)await this.InvokeAsync("GET", "search", new QueryString { Jql = jql }).ConfigureAwait(false);
            var issues = ((JArray)result["issues"]).OfType<JObject>();

            return from i in issues
                   select new Issue(i, this.host);
        }

        private async Task<JToken> InvokeAsync(string method, string relativeUrl, QueryString query = null, JObject data = null)
        {
            var url = this.apiBaseUrl + relativeUrl + query?.ToString();

            var request = WebRequest.CreateHttp(url);
            request.UserAgent = "InedoJiraExtension/" + typeof(RestApiClient).Assembly.GetName().Version.ToString();
            request.ContentType = "application/json";
            request.Method = method;
            if (data != null)
            {
                using var requestStream = await request.GetRequestStreamAsync();
                using var writer = new JsonTextWriter(new StreamWriter(requestStream, InedoLib.UTF8Encoding));
                data.WriteTo(writer);
            }

            if (!string.IsNullOrEmpty(this.UserName))
                request.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.UserName + ":" + this.Password));

            try
            {
                using var response = await request.GetResponseAsync();
                using var responseStream = response.GetResponseStream();
                using var reader = new JsonTextReader(new StreamReader(responseStream));
                return JObject.Load(reader);
            }
            catch (WebException ex) when (ex.Response != null)
            {
                bool rethrow = true;
                using var responseStream = ex.Response.GetResponseStream();
                using var reader = new JsonTextReader(new StreamReader(responseStream));
                string message = null;
                try
                {
                    var obj = JObject.Load(reader);
                    var messages = ((JArray)obj["errorMessages"]).Select(m => m.Value<string>());
                    var errors = (JObject)obj["errors"];

                    message = "JIRA API response error: " + string.Join("; ", messages.Concat(errors.Properties().Select(e => $"{e.Value} ({e.Name})")));
                    rethrow = false;
                }
                catch
                {
                }

                if (rethrow)
                    throw;
                else
                    throw new Exception(message, ex);
            }
        }
    }
}
