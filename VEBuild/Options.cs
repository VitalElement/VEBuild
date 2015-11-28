namespace VEBuild
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CommandLine;
    using CommandLine.Text;

    class Options
    {
        [Option('p', "Project", Required = true, HelpText = "Name of project to build.")]
        public string Project { get; set; }

        [Option('j', "jobs", Required = false, HelpText = "Number of jobs for compiling.")]
        public int Jobs { get; set; }
    }

    [Verb("build", HelpText ="Builds the project in the current directory.")]
    class BuildOptions
    {
        [Value(0, Required =true, MetaName = "Project", HelpText = "Name of project to build")]        
        public string Project { get; set; }

        [Option('j', "jobs", Required = false, Default = 1, HelpText = "Number of jobs for compiling.")]
        public int Jobs { get; set; }        
    }

    [Verb("clean", HelpText = "Cleans the specified project.")]
    class CleanOptions
    {
        [Value(0, Required = true, MetaName = "Project", HelpText = "Name of project to clean")]
        public string Project { get; set; }
    }

    [Verb("add", HelpText = "Adds source files to the specified project file.")]
    class AddOptions
    {
        [Option(Required = true, HelpText = "File name to add to directory.")]
        public int File { get; set; }

    }
}
