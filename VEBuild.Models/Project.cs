namespace VEBuild.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using LibGit2Sharp;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProjectType
    {
        Executable,
        SharedLibrary,
        StaticLibrary
    }

    public class Project : SerializedObject<Project>
    {
        [JsonIgnore]
        public bool IsBuilding { get; set; }

        public static Project Load(string filename, Solution solution)
        {
            var project = Deserialize(filename);

            project.Location = filename;
            project.SetSolution(solution);

            foreach (var file in project.SourceFiles)
            {
                file.SetProject(project);
            }

            return project;
        }

        public Project()
        {
            Languages = new List<Language>();
            References = new List<Reference>();
            PublicIncludes = new List<string>();
            Includes = new List<string>();
            SourceFiles = new List<SourceFile>();
            CompilerArguments = new List<string>();
            ToolChainArguments = new List<string>();
            CCompilerArguments = new List<string>();
            CppCompilerArguments = new List<string>();
            BuiltinLibraries = new List<string>();
            Defines = new List<string>();

        }

        /// <summary>
        /// Resolves each reference, cloning and updating Git referenced projects where possible.
        /// </summary>
        public void ResolveReferences (IConsole console)
        {
            foreach (var reference in References)
            {
                if (!string.IsNullOrEmpty(reference.GitUrl))
                {
                    var referenceDirectory = Path.Combine(SolutionDirectory, reference.Name);

                    if(!Directory.Exists (referenceDirectory))
                    {
                        var options = new CloneOptions();
                        options.OnProgress = (serveroutput) =>
                        {
                            console.OverWrite(serveroutput);
                            return true;
                        };  

                        options.CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials() { Username = "dan@walms.co.uk", Password = "****" };

                        console.WriteLine(string.Format("Cloning Reference {0}", reference.Name));
                        var clone = Repository.Clone(reference.GitUrl, referenceDirectory, options);


                    }
                    //else if(Repository.IsValid(referenceDirectory))
                    //{

                    //}                    
                    //else
                    //{
                    //    throw new Exception(string.Format("Trying to resolve reference {0}, but there is already a directory with that name. {1}", reference.Name, referenceDirectory));
                    //}
                }
            }              
        }

        public void SetSolution(Solution solution)
        {
            this.Solution = solution;
        }

        [JsonIgnore]
        private string SolutionDirectory
        {
            get
            {
                return Directory.GetParent(CurrentDirectory).FullName;                
            }
        }

        protected List<string> GenerateReferenceIncludes()
        {
            List<string> result = new List<string>();

            foreach (var reference in References)
            {
                var loadedReference = GetReference(reference);

                result.AddRange(loadedReference.GenerateReferenceIncludes());
            }

            foreach (var includePath in PublicIncludes)
            {
                result.Add(Path.Combine(CurrentDirectory, includePath));
            }

            return result;
        }

        public List<string> GetReferencedIncludes()
        {
            List<string> result = new List<string>();

            foreach (var reference in References)
            {
                var loadedReference = GetReference(reference);

                result.AddRange(loadedReference.GenerateReferenceIncludes());
            }

            return result;
        }

        [JsonIgnore]
        public Solution Solution { get; private set; }

        [JsonIgnore]
        public string CurrentDirectory
        {
            get
            {
                return Path.GetDirectoryName(Location);
            }
        }

        [JsonIgnore]
        public string Location { get; private set; }

        public Project GetReference(Reference reference)
        {
            Project result = null;

            foreach (var project in Solution.Projects)
            {
                if (project.Name == reference.Name)
                {
                    result = project;
                    break;
                }
            }

            if (result == null)
            {
                throw new Exception(string.Format("Unable to find reference {0}, in directory {1}", reference, Solution.CurrentDirectory));
            }

            return result;
        }

        public string Name { get; set; }
        public List<Language> Languages { get; set; }
        public ProjectType Type { get; set; }


        public bool ShouldSerializeReferences()
        {
            return References.Count > 0;
        }

        public List<Reference> References { get; set; }

        public bool ShouldSerializePublicIncludes()
        {
            return PublicIncludes.Count > 0;
        }

        public List<string> PublicIncludes { get; set; }

        public bool ShouldSerializeIncludes()
        {
            return Includes.Count > 0;
        }

        public List<string> Includes { get; set; }

        public bool ShouldSerializeDefines()
        {
            return Defines.Count > 0;
        }

        public List<string> Defines { get; set; }

        public bool ShouldSerializeFiles()
        {
            return SourceFiles.Count > 0;
        }
        public List<SourceFile> SourceFiles { get; set; }

        public bool ShouldSerializeCompilerArguments()
        {
            return CompilerArguments.Count > 0;
        }

        public List<string> CompilerArguments { get; set; }

        public bool ShouldSerializeCCompilerArguments()
        {
            return CCompilerArguments.Count > 0;
        }

        public List<string> CCompilerArguments { get; set; }

        public bool ShouldSerializeCppCompilerArguments()
        {
            return CppCompilerArguments.Count > 0;
        }

        public List<string> CppCompilerArguments { get; set; }

        public bool ShouldSerializeToolChainArguments()
        {
            return ToolChainArguments.Count > 0;
        }
        public List<string> ToolChainArguments { get; set; }

        public bool ShouldSerializeBuiltinLibraries()
        {
            return BuiltinLibraries.Count > 0;
        }
        public List<string> BuiltinLibraries { get; set; }

        public string BuildDirectory { get; set; }
        public string LinkerScript { get; set; }

    }
}
