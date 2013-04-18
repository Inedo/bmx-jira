﻿using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMasterExtensions.Jira.JiraApi;

namespace Inedo.BuildMasterExtensions.Jira
{
    [Serializable]
    internal sealed class JiraCategory : CategoryBase
    {
        public enum CategoryTypes { Project }

        public CategoryTypes CategoryType { get; private set; }

        private JiraCategory(string categoryId, string categoryName, CategoryBase[] subCategories, CategoryTypes categoryType)
            : base(categoryId, categoryName, subCategories) 
        { 
            CategoryType = categoryType;
        }

        internal static JiraCategory CreateProject(RemoteProject remoteProject)
        {
            return new JiraCategory(
                remoteProject.key,
                remoteProject.name,
                new JiraCategory[] {},
                CategoryTypes.Project);
        }
    }
}
