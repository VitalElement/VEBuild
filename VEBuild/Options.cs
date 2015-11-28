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

    abstract class ProjectOption
    {
        [Value(0, MetaName = "Project", HelpText = "Name of project to run command on")]
        public string Project { get; set; }
    }

    [Verb("build", HelpText ="Builds the project in the current directory.")]
    class BuildOptions : ProjectOption
    {

        [Option('j', "jobs", Required = false, Default = 1, HelpText = "Number of jobs for compiling.")]
        public int Jobs { get; set; }        
    }

    [Verb("clean", HelpText = "Cleans the specified project.")]
    class CleanOptions : ProjectOption
    {
    }

    [Verb("add", HelpText = "Adds source files to the specified project file.")]
    class AddOptions
    {
        [Option(Required = true, HelpText = "File name to add to directory.")]
        public int File { get; set; }

    }
}
