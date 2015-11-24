namespace VEBuild.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Collections.Generic;
    using System.IO;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProjectType
    {
        Executable,
        SharedLibrary,
        StaticLibrary
    }

    public class Project : SerializedObject<Project>
    {
        public static Project Load (string filename, Solution solution)
        {
            var project = Deserialize(filename);

            project.Location = filename;
            project.SetSolution(solution);

            foreach(var file in project.SourceFiles)
            {
                file.SetProject(project);
            }

            return project;
        }

        public Project()
        {
            Languages = new List<Language>();
            References = new List<string>();
            PublicIncludes = new List<string>();
            Includes = new List<string>();
            SourceFiles = new List<SourceFile>();
            CompilerArguments = new List<string>();
            ToolChainArguments = new List<string>();
            CCompilerArguments = new List<string>();
            CppCompilerArguments = new List<string>();
            BuiltinLibraries = new List<string>();

        }

        public void SetSolution (Solution solution)
        {
            this.Solution = solution;
        }

        [JsonIgnore]
        public Solution Solution { get; private set; }

        [JsonIgnore]
        public string Directory
        {
            get
            {
                return Path.GetDirectoryName(Location);
            }
        }

        [JsonIgnore]
        public string Location { get; private set; }

        public Project GetReference (string reference)
        {
            Project result = null;

            foreach(var project in Solution.LoadedProjects)
            {
                if(project.Name == reference)
                {
                    result = project;
                    break;
                }
            }

            return result;
        }

        public string Name { get; set; }       
        public List<Language> Languages { get; set; }
        public ProjectType Type { get; set; }
        public List<string> References { get; set; }
        public List<string> PublicIncludes { get; set; }
        public List<string> Includes { get; set; }
        public List<SourceFile> SourceFiles { get; set; }      
        public List<string> CompilerArguments { get; set; }
        public List<string> CCompilerArguments { get; set; }
        public List<string> CppCompilerArguments { get; set; }
        public List<string> ToolChainArguments { get; set; }
        public List<string> BuiltinLibraries { get; set; }
        public string LinkerScript { get; set; }

    }
}
