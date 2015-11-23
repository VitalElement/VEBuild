namespace VEBuild.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System.Collections.Generic;


    [JsonConverter(typeof(StringEnumConverter))]
    public enum Language
    {
        C,
        Cpp
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ProjectType
    {
        Executable,
        SharedLibrary,
        StaticLibrary
    }

    public class Project : SerializedObject<Project>
    {
        public Project()
        {
            Languages = new List<Language>();
            References = new List<string>();
            PublicIncludes = new List<string>();
            Includes = new List<string>();
            SourceFiles = new List<SourceFile>();
        }

        public string Name { get; set; }       
        public List<Language> Languages { get; set; }
        public ProjectType Type { get; set; }
        public List<string> References { get; set; }
        public List<string> PublicIncludes { get; set; }
        public List<string> Includes { get; set; }
        public List<SourceFile> SourceFiles { get; set; }        
    }
}
