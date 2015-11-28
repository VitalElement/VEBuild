namespace VEBuild
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CommandLine;
    using CommandLine.Text;
    using Models;

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
    class AddOptions : ProjectOption
    {
        [Value(0, Required = true, HelpText = "File name to add to directory.")]
        public int File { get; set; }
    }

    [Verb("addref", HelpText = "Adds a reference to the current project")]
    class AddReferenceOptions : ProjectOption
    {        
        [Value(1, Required = true, HelpText = "Name of the refernece.", MetaName = "Reference Name")]
        public string Name { get; set; }

        [Option('u', "giturl", HelpText = "Url to GitRepository containing a reference.")]
        public string GitUrl { get; set; }

        [Option('r', "revision", HelpText = "Revision to keep the reference at, this can be HEAD, any tag or SHA")]
        public string Revision { get; set; }
    }

    [Verb ("create", HelpText = "Creates new projects.")]
    class CreateOptions : ProjectOption
    {
        [Option('t', "Type", Required = true, HelpText = "Options are Exe or StaticLib")]
        public ProjectType IsLib { get; set; }
    }
}
