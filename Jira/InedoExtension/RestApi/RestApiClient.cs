using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Inedo.Extensibility.IssueSources;
using Inedo.Extensions.Jira.Clients;

namespace Inedo.Extensions.Jira.RestApi
{
#warning This needs a complete rewrite to be converted from generic objects to Newtonsoft.Json and deserialize needs to be typed not Dictionaries
    internal sealed class RestApiClient
    {
        private string host;
        private string apiBaseUrl;

        public RestApiClient(string host)
        {
            this.host = host.TrimEnd('/');
            this.apiBaseUrl = this.host + "/rest/api/2/";
        }

        public string UserName { get; set; }
        public string Password { get; set; }

        public async Task<IEnumerable<Permission>> GetPermissionsAsync()
        {
            var results = (Dictionary<string, object>)await this.InvokeAsync("GET", "mypermissions");
            var permissions = (Dictionary<string, object>)results["permissions"];
            return permissions.Select(p => new Permission((Dictionary<string, object>)p.Value));
        }

        public Task AddCommentAsync(string issueKey, string comment)
        {
            return this.InvokeAsync("POST", $"issue/{issueKey}/comment", data: new { body = comment });
        }

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
                data: new
                {
                    transition = new { id = transitionId }
                }
            );
        }

        public async Task<IEnumerable<Transition>> GetTransitionsAsync(string issueKey)
        {
            var results = (Dictionary<string, object>)await this.InvokeAsync(
                "GET",
                $"issue/{issueKey}/transitions"
            );

            var transitions = (IEnumerable<object>)results["transitions"];
            return transitions.Select(t => new Transition((Dictionary<string, object>)t));
        }

        public Task ReleaseVersionAsync(string projectKey, string versionId)
        {
            return this.InvokeAsync(
                "PUT",
                $"version/{versionId}",
                data: new
                {
                    released = true,
                    releaseDate = DateTime.Now
                }
            );
        }

        public Task CreateVersionAsync(string projectKey, string versionNumber)
        {
            return this.InvokeAsync(
                "POST",
                "version",
                data: new
                {
                    name = versionNumber,
                    project = projectKey
                }
            );
        }

        public async Task<Issue> CreateIssueAsync(string projectKey, string summary, string description, string issueTypeId, string fixForVersion)
        {
            var fixVersions = new List<object>();
            if (!string.IsNullOrEmpty(fixForVersion))
            {
                var version = (await this.GetVersionsAsync(projectKey)).FirstOrDefault(v => v.Name == fixForVersion);
                if (version != null)
                    fixVersions.Add(new { id = version.Id });
            }

            var result = (Dictionary<string, object>)await this.InvokeAsync(
                "POST",
                "issue",
                data: new
                {
                    fields = new
                    {
                        project = new
                        {
                            key = projectKey
                        },
                        summary,
                        description,
                        issuetype = new
                        {
                            id = issueTypeId
                        },
                        fixVersions
                    }
                }
            );

            return await this.GetIssueAsync(result["key"].ToString());
        }

        public async Task<Issue> GetIssueAsync(string issueKey)
        {
            var result = (Dictionary<string, object>)await this.InvokeAsync(
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
            var result = (Dictionary<string, object>)await this.InvokeAsync(
                "GET",
                "search",
                new QueryString { Jql = $"project='{projectKey}' and fixVersion='{versionName}'" }
            );

            var issues = (IEnumerable<object>)result["issues"];
            return issues.Select(i => new Issue((Dictionary<string, object>)i, this.host));
        }

        public async Task<IEnumerable<IIssueTrackerIssue>> GetIssuesAsync(string jql)
        {
            var result = (Dictionary<string, object>)await this.GetAsync("search", new QueryString { Jql = jql }).ConfigureAwait(false);

            var issues = (IEnumerable<object>)result["issues"];

            return from i in issues
                   select new Issue((Dictionary<string, object>)i, this.host);
        }

        private async Task<object> GetAsync(string relativeUrl, QueryString query)
        {
            using (var client = this.CreateClient())
            {
                string url = this.apiBaseUrl + relativeUrl + query?.ToString();

                var response = await client.GetAsync(url).ConfigureAwait(false);
                await HandleError(response).ConfigureAwait(false);
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var serializer = new JavaScriptSerializer();
                return serializer.DeserializeObject(json);
            }
        }

        private async Task<object> PostAsync(string relativeUrl, QueryString query, object data)
        {
            using (var client = this.CreateClient())
            {
                string url = this.apiBaseUrl + relativeUrl + query?.ToString();

                var serializer = new JavaScriptSerializer();

                var response = await client.PostAsync(url, new StringContent(serializer.Serialize(data))).ConfigureAwait(false);
                await HandleError(response).ConfigureAwait(false);
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return serializer.DeserializeObject(json);
            }
        }

        private HttpClient CreateClient()
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.UserAgent.Clear();
            if (!string.IsNullOrEmpty(this.UserName))
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(InedoLib.UTF8Encoding.GetBytes(this.UserName + ":" + this.Password)));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(SDK.ProductName, SDK.ProductVersion.ToString()));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("InedoJiraExtension", typeof(RestApiClient).Assembly.GetName().Version.ToString()));
            client.DefaultRequestHeaders.Add("ContentType", "application/json");

            return client;
        }

        private static async Task HandleError(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var message = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new Exception($"An error was returned (HTTP {response.StatusCode}) from the JIRA REST API: {message}");
        }

        private async Task<object> InvokeAsync(string method, string relativeUrl, QueryString query = null, object data = null)
        {
            var url = this.apiBaseUrl + relativeUrl + query?.ToString();

            var request = WebRequest.CreateHttp(url);
            request.UserAgent = "InedoJiraExtension/" + typeof(RestApiClient).Assembly.GetName().Version.ToString();
            request.ContentType = "application/json";
            request.Method = method;
            if (data != null)
            {
                using (var requestStream = await request.GetRequestStreamAsync())
                using (var writer = new StreamWriter(requestStream, InedoLib.UTF8Encoding))
                {
                    var js = new JavaScriptSerializer();
                    writer.Write(js.Serialize(data));
                }
            }

            if (!string.IsNullOrEmpty(this.UserName))
            {                
                request.Headers[HttpRequestHeader.Authorization] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(this.UserName + ":" + this.Password));                
            }

            try
            {
                using (var response = await request.GetResponseAsync())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    var js = new JavaScriptSerializer();
                    return js.DeserializeObject(reader.ReadToEnd());
                }
            }
            catch (WebException ex) when (ex.Response != null)
            {
                using (var responseStream = ex.Response.GetResponseStream())
                {
                    string message;
                    try
                    {
                        var js = new JavaScriptSerializer();
                        var result = (Dictionary<string, object>)js.DeserializeObject(new StreamReader(responseStream).ReadToEnd());
                        var messages = (IEnumerable<object>)result["errorMessages"];
                        var errors = (Dictionary<string, object>)result["errors"];
                        message = "JIRA API response error: " + string.Join("; ", messages.Concat(errors.Select(e => $"{e.Value} ({e.Key})")));
                    }
                    catch
                    {
                        throw ex;
                    }

                    throw new Exception(message, ex);
                }
            }
        }
    }
}
