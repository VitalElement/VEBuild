namespace VEBuild.Models
{
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.IO;

    public class Solution : SerializedObject<Solution>
    {
        public const string solutionExtension = "vsln";
        public const string projectExtension = "vproj";

        public static Solution Load (string filename)
        {            
            var solution = Deserialize(filename);

            solution.Location = filename;

            foreach(var project in solution.Projects)
            {
                var projectDir = Path.Combine(solution.Directory, project.Name);
                solution.LoadedProjects.Add(Project.Load(Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, projectExtension)), solution));
            }

            return solution;
        }

        public Solution()
        {
            Projects = new List<ProjectDescription>();
            LoadedProjects = new List<Project>();
        }

        [JsonIgnore]
        public string Location { get; private set; }

        [JsonIgnore]
        public string Directory
        {
            get
            {
                return Path.GetDirectoryName(Location);
            }
        }

        public string Name { get; set; }
        public List<ProjectDescription> Projects { get; set; }        

        [JsonIgnore]
        public List<Project> LoadedProjects { get; set; }
    }
}
