namespace VEBuild.Models
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;


    [JsonConverter(typeof(StringEnumConverter))]
    public enum Language
    {
        C,
        Cpp
    }

    public abstract class Compiler
    {
        public abstract string[] GetDefaultArgs();

        public abstract string[] GetLinkerDefaultArgs();     
        
        public Language Language { get; protected set; }   

        public string DefaultExtension { get; protected set; }

        public string Key { get; protected set; }

        public bool IsCross { get; protected set; }

        public abstract bool CanCompile(string filename);
    }

    public abstract class CCompiler : Compiler
    {
        public CCompiler()
        {
            Language = Language.C;
            DefaultExtension = "c";
            Key = "Unknown";            
        }

        public abstract string[] GetWarningArguments(int level);

        public virtual string[] GetDependencyGenerationArgs(string outTarget, string outFile)
        {
            return new string[] {"-MMD", "-MQ", outTarget, "-MF", outFile };
        }

        public string DependencyFileExtension
        {
            get
            {
                return "d";
            }
        }

        public string CompileOnlyArgs
        {
            get
            {
                return "-c";
            }
        }

        public string GetOutputArgs(string target)
        {
            return string.Format("-o {0}", target);
        }

        public string GetLinkerOutputArgs (string outputName)
        {
            return string.Format("-o {0}", outputName);
        }

        public string GetIncludeArgs (string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                path = ".";
            }

            return string.Format("-I{0}", path);
        }

        public override bool CanCompile(string filename)
        {
            bool result = false;

            var extension = Path.GetExtension(filename);

            if(extension == "c" || extension == "h")
            {
                result = true;
            }

            return result;
        }
    }
}
