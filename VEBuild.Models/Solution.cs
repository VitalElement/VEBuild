namespace VEBuild.Models
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class Solution : SerializedObject<Solution>
    {
        public const string solutionExtension = "vsln";
        public const string projectExtension = "vproj";

        public static Solution Load (string directory)
        {
            var solution = new Solution();
            solution.CurrentDirectory = directory;

            if(!Directory.Exists(directory))
            {
                throw new Exception(string.Format("Directory does not exist {0}", directory));
            }

            var subfolders = Directory.GetDirectories(directory);

            foreach(var subfolder in subfolders)
            {
                var projectFile = string.Format("{0}.{1}", Path.GetFileName(subfolder), projectExtension);
                var projectLocation = Path.Combine(subfolder, projectFile);

                if (File.Exists(projectLocation))
                {
                    solution.Projects.Add(Project.Load(projectLocation, solution));

                }
                else
                {
                    throw new Exception(string.Format("Unable to find project file {0}", projectLocation));
                }
            }

            return solution;
        }

        public Solution()
        {
            Projects = new List<Project>();
        }

        [JsonIgnore]
        public string CurrentDirectory { get; private set; }
        
        [JsonIgnore]
        public List<Project> Projects { get; set; }
    }
}
