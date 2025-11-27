using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreatModeler.TF.Git.Contract;
using ThreatModeler.TF.Git.Contract.Models;

namespace ThreatModeler.TF.Git.Implementation
{
    public class GitService : IGitService
    {
        private readonly ILogger<GitService> _logger;

        public GitService(ILogger<GitService> logger)
        {
            _logger = logger;
        }

        public void SyncRepository(GitSettings settings)
        {
            using (_logger.BeginScope("SyncOperation Path={Path}", settings.LocalPath))
            {
                _logger.LogInformation("Starting Sync for Branch: {Branch}", settings.Branch);

                try
                {
                    if (string.IsNullOrEmpty(settings.LocalPath))
                        throw new ArgumentNullException(nameof(settings.LocalPath));

                    // 1. Ensure Directory
                    if (!Directory.Exists(settings.LocalPath))
                    {
                        _logger.LogDebug("Creating directory: {Path}", settings.LocalPath);
                        Directory.CreateDirectory(settings.LocalPath);
                    }

                    // 2. Clone or Pull
                    // We check if it is a valid repo. If not, we clone.
                    if (!Repository.IsValid(settings.LocalPath))
                    {
                        CloneRepository(settings);
                    }
                    else
                    {
                        PullRepository(settings);
                    }

                    _logger.LogInformation("Sync completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Sync failed.");
                    throw;
                }
            }
        }

        public void CommitAndPush(GitCommitContext context)
        {
            using (_logger.BeginScope("PushOperation Branch={Branch}", context.Branch))
            {
                try
                {
                    // Validation
                    if (string.IsNullOrWhiteSpace(context.CommitMessage))
                    {
                        throw new ArgumentException("Commit message is required for this operation.");
                    }

                    if (!Repository.IsValid(context.LocalPath))
                    {
                        throw new DirectoryNotFoundException($"Repository not found at {context.LocalPath}. Run Sync first.");
                    }

                    using (var repo = new Repository(context.LocalPath))
                    {
                        // 1. Stage
                        _logger.LogDebug("Staging changes...");
                        Commands.Stage(repo, "*");

                        // 2. Commit
                        var author = new Signature(context.AuthorName, context.AuthorEmail, DateTimeOffset.Now);

                        if (repo.RetrieveStatus().IsDirty)
                        {
                            _logger.LogInformation("Committing changes: {Message}", context.CommitMessage);
                            repo.Commit(context.CommitMessage, author, author);
                        }
                        else
                        {
                            _logger.LogWarning("No changes detected to commit.");
                        }

                        // 3. Push
                        _logger.LogInformation("Pushing to remote...");
                        var remote = repo.Network.Remotes["origin"];

                        // PushOptions still has CredentialsProvider directly
                        var options = new PushOptions
                        {
                            CredentialsProvider = GetCredentials(context)
                        };

                        string pushRefSpec = $"refs/heads/{context.Branch}";
                        repo.Network.Push(remote, pushRefSpec, options);
                    }

                    _logger.LogInformation("Push completed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Push failed.");
                    throw;
                }
            }
        }

        // --- Private Helper Methods ---

        private void CloneRepository(GitSettings settings)
        {
            _logger.LogInformation("Cloning repository from {Url}...", settings.RepoUrl);

            // FIX FOR LibGit2Sharp v0.31.0:
            // Credentials must be inside FetchOptions, which is passed to CloneOptions constructor.
            var fetchOptions = new FetchOptions
            {
                CredentialsProvider = GetCredentials(settings)
            };

            var cloneOptions = new CloneOptions(fetchOptions)
            {
                BranchName = settings.Branch
            };

            Repository.Clone(settings.RepoUrl, settings.LocalPath, cloneOptions);
        }

        private void PullRepository(GitSettings settings)
        {
            _logger.LogInformation("Pulling repository...");
            using (var repo = new Repository(settings.LocalPath))
            {
                var remote = repo.Network.Remotes["origin"];

                // Credentials for Fetch/Pull also go into FetchOptions
                var fetchOptions = new FetchOptions
                {
                    CredentialsProvider = GetCredentials(settings)
                };

                // 1. Fetch
                Commands.Fetch(repo, remote.Name, new string[0], fetchOptions, null);

                // 2. Checkout correct branch if needed
                if (repo.Head.FriendlyName != settings.Branch)
                {
                    _logger.LogInformation("Switching to branch {Branch}...", settings.Branch);
                    Commands.Checkout(repo, settings.Branch);
                }

                // 3. Merge (Pull)
                var signature = new Signature(settings.AuthorName, settings.AuthorEmail, DateTimeOffset.Now);
                var pullOptions = new PullOptions
                {
                    FetchOptions = fetchOptions
                };

                Commands.Pull(repo, signature, pullOptions);
            }
        }

        // DRY Principle: Reusable credentials logic
        private CredentialsHandler GetCredentials(GitSettings settings)
        {
            return (url, user, types) => new UsernamePasswordCredentials
            {
                Username = settings.Username,
                Password = settings.Password
            };
        }
    }
}