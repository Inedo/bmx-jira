using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inedo.Diagnostics;
using Inedo.Extensibility.Credentials;
using Inedo.Extensibility.IssueSources;
using UsernamePasswordCredentials = Inedo.Extensions.Credentials.UsernamePasswordCredentials;

namespace Inedo.Extensions.Jira.Clients
{
    internal abstract class JiraClient : IDisposable
    {
        protected string userName;
        protected string password;
        protected string serverUrl;
        protected ILogSink log;

        protected JiraClient(string serverUrl, string userName, string password, ILogSink log)
        {
            if (string.IsNullOrEmpty(serverUrl))
                throw new ArgumentNullException(nameof(serverUrl));
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentNullException(nameof(userName));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));
            
            this.serverUrl = serverUrl;
            this.userName = userName;
            this.password = password;
            this.log = log;
        }

        public static JiraClient Create(string serverUrl, SecureCredentials credentials, ILogSink log = null)
        {
            if (credentials is not UsernamePasswordCredentials upCredentials)
                throw new InvalidCastException("Jira requires a Username Password Credential");
            log?.LogDebug($"Loading specified JIRA REST client...");
            return new JiraRestClient(serverUrl, upCredentials.UserName, AH.Unprotect(upCredentials.Password), log);
        }

        public abstract Task<IEnumerable<JiraProject>> GetProjectsAsync();
        public abstract Task<IEnumerable<ProjectVersion>> GetProjectVersionsAsync(string projectKey);
        public abstract Task<IEnumerable<Transition>> GetTransitionsAsync(JiraContext context);

        public abstract Task ValidateConnectionAsync();

        public abstract Task<IEnumerable<IIssueTrackerIssue>> EnumerateIssuesAsync(JiraContext context);
        public abstract Task AddCommentAsync(JiraContext context, string issueId, string commentText);
        public abstract Task TransitionIssueAsync(JiraContext context, string issueId, string issueStatus);
        public abstract Task TransitionIssuesInStatusAsync(JiraContext context, string fromStatus, string toStatus);
        public abstract Task CloseIssueAsync(JiraContext context, string issueId);
        public abstract Task CreateReleaseAsync(JiraContext context);
        public abstract Task DeployReleaseAsync(JiraContext context);
        public abstract Task<IIssueTrackerIssue> CreateIssueAsync(JiraContext context, string title, string description, string type);

        public abstract Task<IEnumerable<JiraIssueType>> GetIssueTypesAsync(string projectId);

        public async Task CloseAllIssuesAsync(JiraContext context)
        {
            foreach (var issue in await this.EnumerateIssuesAsync(context))
            {
                if (issue.IsClosed)
                {
                    this.log?.LogDebug($"{issue.Id} is already closed, skipping...");
                    continue;
                }

                await this.CloseIssueAsync(context, issue.Id);
            }
        }

        public async Task<JiraProject> FindProjectAsync(string projectName)
        {
            return (await this.GetProjectsAsync()).FirstOrDefault(p => string.Equals(projectName, p.Name, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<JiraIssueType> FindIssueTypeAsync(string projectId, string issueTypeName)
        {
            return (await this.GetIssueTypesAsync(projectId)).FirstOrDefault(p => string.Equals(issueTypeName, p.Name, StringComparison.OrdinalIgnoreCase));
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose() => this.Dispose(true);
    }
}
