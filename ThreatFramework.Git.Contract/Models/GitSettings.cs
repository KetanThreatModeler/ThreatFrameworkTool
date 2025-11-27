using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatModeler.TF.Git.Contract.Models
{
    public class GitSettings
    {
        public string RepoUrl { get; set; }
        public string LocalPath { get; set; }
        public string Branch { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
    }
}
