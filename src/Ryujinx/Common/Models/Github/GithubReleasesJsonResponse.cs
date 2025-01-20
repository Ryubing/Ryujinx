using System.Collections.Generic;

namespace Ryujinx.Ava.Common.Models.Github
{
    public class GithubReleasesJsonResponse
    {
        public string Name { get; set; }
        
        public string TagName { get; set; }
        public List<GithubReleaseAssetJsonResponse> Assets { get; set; }
    }
}
