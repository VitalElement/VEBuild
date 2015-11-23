namespace VEBuild.Models
{
    using System.Collections.Generic;

    public class Solution : SerializedObject<Solution>
    {
        public Solution()
        {
            Projects = new List<ProjectDescription>();
        }

        public string Name { get; set; }
        public List<ProjectDescription> Projects { get; set; }        
    }
}
