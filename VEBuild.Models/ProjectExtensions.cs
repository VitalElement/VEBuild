using System.Collections.Generic;
using System.IO;

namespace VEBuild.Models
{
    public static class ProjectExtensions
    {
        public static string GetOutputDirectory(this Project project, Project superProject)
        {
            string outputDirectory = Path.Combine(superProject.Directory, "obj");

            if (project != superProject)
            {
                outputDirectory = Path.Combine(outputDirectory, project.Name);
            }

            return outputDirectory;
        }

        public static List<string> GetDependencies(string dependencyFile)
        {
            var result = new List<string>();

            StreamReader sr = new StreamReader(dependencyFile);

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();

                if (!string.IsNullOrEmpty(line))
                {
                    if (!line.EndsWith(":") && !line.EndsWith(": \\"))
                    {
                        result.Add(line.Replace(" \\", string.Empty).Trim());
                    }
                }
            }

            sr.Close();

            return result;
        }        
    }
}
