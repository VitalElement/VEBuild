using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VEBuild.Models
{
    public abstract class FastToolChain : ToolChain
    {
        public FastToolChain(ToolchainSettings settings)
        {
            this.Settings = settings;
        }

        protected ToolchainSettings Settings { get; private set; }

        public abstract CompileResult Compile(IConsole console, Project superProject, Project project, SourceFile file, string outputFile);

        public abstract LinkResult Link(IConsole console, Project superProject, Project project, CompileResult assemblies, string outputDirectory);

        public abstract ProcessResult Size(IConsole console, Project project, LinkResult linkResult);

        public abstract string GetCompilerArguments(Project superProject, Project project, Language language);

        public abstract string GetLinkerArguments(Project project);

        private object resultLock = new object();
        private int numTasks = 0;

        private void ClearBuildFlags(Project project)
        {
            foreach (var reference in project.References)
            {
                var loadedReference = project.GetReference(reference);

                ClearBuildFlags(loadedReference);
            }

            project.IsBuilding = false;
        }

        bool terminateBuild = false;

        private int GetFileCount (Project project)
        {
            int result = 0;

            foreach (var reference in project.References)
            {
                var loadedReference = project.GetReference(reference);

                result += GetFileCount(loadedReference);                
            }

            if(!project.IsBuilding)
            {
                project.IsBuilding = true;

                result += project.SourceFiles.Count;
            }

            return result;
        }

        private int fileCount = 0;
        private int buildCount = 0;

        private void SetFileCount(Project project)
        {
            ClearBuildFlags(project);

            fileCount = GetFileCount(project);

            ClearBuildFlags(project);
        }

        public override async Task<bool> Build(IConsole console, Project project)
        {
            bool result = true;
            terminateBuild = false;

            SetFileCount(project);
            buildCount = 0;
            
            var compiledReferences = new List<CompileResult>();
            var compiledProject = new List<CompileResult>();

            foreach (var reference in project.References)
            {
                var loadedReference = project.GetReference(reference);

                await CompileProject(console, project, loadedReference, compiledReferences);

                if(terminateBuild)
                {
                    break;
                }
            }

            if (!terminateBuild)
            {
                await CompileProject(console, project, project, compiledProject);

                if (!terminateBuild)
                {
                    await WaitForCompileJobs();
                    
                    foreach (var compiledReference in compiledReferences)
                    {
                        result = compiledReference.ExitCode == 0;

                        if(!result)
                        {
                            break;
                        }
                    }                    

                    if (result)
                    {
                        result = compiledProject.First().ExitCode == 0;

                        if (result)
                        {

                            foreach (var compiledReference in compiledReferences)
                            {
                                Link(console, project, compiledReference, compiledProject.First());
                            }

                            Link(console, project, compiledProject.First(), compiledProject.First());
                        }
                    }

                    ClearBuildFlags(project);
                }
            }

            console.WriteLine();

            if(result)
            {
                console.WriteLine("Build Successful");
            }
            else
            {
                console.WriteLine("Build Failed");
            }

            return result;
        }

        private async Task WaitForCompileJobs()
        {
            await Task.Factory.StartNew(() =>
            {
                while (numTasks > 0)
                {
                    Thread.Sleep(10);
                }
            });
        }

        private void Link(IConsole console, Project superProject, CompileResult compileResult, CompileResult linkResults)
        {
            var binDirectory = compileResult.Project.GetBinDirectory(superProject);

            if (!Directory.Exists(binDirectory))
            {
                Directory.CreateDirectory(binDirectory);
            }

            string outputLocation = binDirectory;

            string executable = Path.Combine(outputLocation, compileResult.Project.Name);

            if (compileResult.Project.Type == ProjectType.StaticLibrary)
            {
                executable = Path.Combine(outputLocation, "lib" + compileResult.Project.Name);
                executable += ".a";
            }
            else
            {
                executable += ".elf";
            }

            if (!Directory.Exists(outputLocation))
            {
                Directory.CreateDirectory(outputLocation);
            }

            console.OverWrite(string.Format("[LL]    [{0}]", compileResult.Project.Name));

            var linkResult = Link(console, superProject, compileResult.Project, compileResult, outputLocation);

            if (linkResult.ExitCode == 0)
            {
                if (compileResult.Project.Type == ProjectType.StaticLibrary)
                {
                    linkResults.LibraryLocations.Add(executable);
                }
                else
                {
                    console.WriteLine();
                    Size(console, compileResult.Project, linkResult);
                    linkResults.ExecutableLocations.Add(executable);
                }
            }
        }

        private async Task CompileProject(IConsole console, Project superProject, Project project, List<CompileResult> results = null)
        {
            if (!terminateBuild)
            {
                if (results == null)
                {
                    results = new List<CompileResult>();
                }

                foreach (var reference in project.References)
                {
                    var loadedReference = project.GetReference(reference);

                    await CompileProject(console, superProject, loadedReference, results);
                }

                var outputDirectory = project.GetOutputDirectory(superProject);

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                
                bool doWork = false;

                lock (resultLock)
                {
                    if (!project.IsBuilding)
                    {
                        project.IsBuilding = true;
                        doWork = true;
                    }
                }

                if (doWork)
                {
                    var objDirectory = project.GetObjectDirectory(superProject);

                    if (!Directory.Exists(objDirectory))
                    {
                        Directory.CreateDirectory(objDirectory);
                    }

                    var compileResults = new CompileResult();
                    compileResults.Project = project;

                    var tasks = new List<Task>();
                    //var parallelResult = Parallel.ForEach(project.SourceFiles, (file) =>
                    int numLocalTasks = 0;

                    foreach (var file in project.SourceFiles)
                    {
                        if (terminateBuild)
                        {
                            break;
                        }

                        if (Path.GetExtension(file.Location) == ".c" || Path.GetExtension(file.Location) == ".cpp")
                        {
                            var outputName = Path.GetFileNameWithoutExtension(file.Location) + ".o";
                            var dependencyFile = Path.Combine(objDirectory, Path.GetFileNameWithoutExtension(file.Location) + ".d");
                            var objectFile = Path.Combine(objDirectory, outputName);

                            bool dependencyChanged = false;

                            if (File.Exists(dependencyFile))
                            {
                                List<string> dependencies = new List<string>();

                                //lock(resultLock)
                                {
                                    dependencies.AddRange(ProjectExtensions.GetDependencies(dependencyFile));

                                    foreach (var dependency in dependencies)
                                    {
                                        if (!File.Exists(dependency) || File.GetLastWriteTime(dependency) > File.GetLastWriteTime(objectFile))
                                        {
                                            dependencyChanged = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (dependencyChanged || !File.Exists(objectFile))
                            {
                                while (numTasks >= 32)
                                {
                                    Thread.Yield();
                                }

                                lock (resultLock)
                                {
                                    numLocalTasks++;
                                    numTasks++;
                                    console.OverWrite(string.Format("[CC {0}/{1}]    [{2}]    {3}", ++buildCount, fileCount, project.Name, Path.GetFileName(file.Location)));
                                }
                                
                                new Thread(new ThreadStart(() =>
                                {
                                    var compileResult = Compile(console, superProject, project, file, objectFile);

                                    lock (resultLock)
                                    {
                                        if (compileResults.ExitCode == 0 && compileResult.ExitCode != 0)
                                        {
                                            terminateBuild = true;
                                            compileResults.ExitCode = compileResult.ExitCode;
                                        }
                                        else
                                        {
                                            compileResults.ObjectLocations.Add(objectFile);
                                        }

                                        numTasks--;
                                        numLocalTasks--;

                                        if (numLocalTasks == 0)
                                        {
                                            results.Add(compileResults);
                                        }
                                    }
                                })).Start();
                            }
                        }
                    }
                }
            }
        }

        public override async Task Clean(IConsole console, Project project)
        {
            await Task.Factory.StartNew(() =>
            {
                console.Clear();
                console.WriteLine("Starting Clean...");

                var outputDir = project.GetOutputDirectory(project);

                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, true);
                }

                console.WriteLine("Clean Completed.");
            });
        }
    }
}
