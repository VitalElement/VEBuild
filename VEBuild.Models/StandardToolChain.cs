namespace VEBuild.Models
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class StandardToolChain : ToolChain
    {
        public StandardToolChain (ToolchainSettings settings)
        {
            this.Settings = settings;
        }

        protected ToolchainSettings Settings { get; private set; }

        public abstract void Compile(IConsole console, Project superProject, Project project, SourceFile file, string outputFile, CompileResult result);

        public abstract LinkResult Link(IConsole console, Project superProject, Project project, CompileResult assemblies, string outputDirectory);

        public abstract ProcessResult Size(IConsole console, Project project, LinkResult linkResult);

        public abstract string GetCompilerArguments(Project project, Language language);

        public abstract string GetLinkerArguments(Project project);

        public override async Task<bool> Build(IConsole console, Project project)
        {
            bool result = false;

            console.Clear();
            console.WriteLine("Starting Build...");            

            if (Settings.ToolChainLocation == null)
            {
                console.WriteLine("Tool chain path has not been configured.");

                result = false;
            }
            else
            {
                if (project.Type == ProjectType.StaticLibrary)
                {
                    result = (await BuildLibrary(console, project, project)).ExitCode == 0;
                }
                else
                {
                    result = (await BuildExecutable(console, project)).ExitCode == 0;
                }
            }

            console.WriteLine();

            if (result)
            {
                console.WriteLine("Build Completed Successfully.");
            }
            else
            {
                console.WriteLine("Build Failed.");
            }

            return result;
        }

        private async Task<CompileResult> BuildExecutable(IConsole console, Project project)
        {
            var result = new CompileResult();

            var compileResults = new CompileResult();

            foreach (var reference in project.References)
            {
                var loadedReference = project.GetReference(reference);

                if(loadedReference == null)
                {
                    throw new Exception(string.Format("Unable to find reference {0}, in directory {1}", reference, project.Solution.Location));
                }

                if (loadedReference.Type == ProjectType.StaticLibrary)
                {
                    var referenceResult = await BuildLibrary(console, project, loadedReference);

                    if (referenceResult.ExitCode == 0)
                    {
                        compileResults.LibraryLocations.AddRange(referenceResult.LibraryLocations);
                        compileResults.NumberOfObjectsCompiled += referenceResult.NumberOfObjectsCompiled;
                    }
                    else
                    {
                        result.ExitCode = -1;
                        return result;
                    }
                }
                else
                {
                    var subResult = await BuildExecutable(console, loadedReference);

                    if (subResult.ExitCode == 0)
                    {
                        foreach (var executable in subResult.ExecutableLocations)
                        {
                            string outputDirectory = Path.Combine(project.Directory, "bin");

                            string destination = Path.Combine(outputDirectory, Path.GetFileName(executable));

                            if (!Directory.Exists(outputDirectory))
                            {
                                Directory.CreateDirectory(outputDirectory);
                            }

                            File.Copy(executable, destination, true);
                        }
                    }
                    else
                    {
                        result.ExitCode = -1;
                        return result;
                    }
                }
            }

            //bool hasBuiltSomething = false;

            //if (project.ToBuild(this, project))
            //{
            //    hasBuiltSomething = true;
            //    console.WriteLine(string.Format("[BB] - Building Executable - {0}", project.Title));
            //}

            var compilationResult = await Compile(console, project, project);

            //if (hasBuiltSomething)
            //{
            //    console.WriteLine();
            //}

            compilationResult.NumberOfObjectsCompiled += compileResults.NumberOfObjectsCompiled;

            if (compilationResult.ExitCode == 0)
            {
                if (compilationResult.Count > 0)
                {
                    compilationResult.ObjectLocations.AddRange(compileResults.ObjectLocations);
                    compilationResult.LibraryLocations.AddRange(compileResults.LibraryLocations);

                    result = Link(console, project, project, compilationResult);
                }

                return result;
            }
            else
            {
                result.ExitCode = -1;
                return result;
            }
        }

        private CompileResult Link(IConsole console, Project superProject, Project project, CompileResult compilationResults)
        {
            var result = new CompileResult();

            string outputLocation = string.Empty;

            var objDirectory = project.GetObjectDirectory(superProject);

            if(!Directory.Exists(objDirectory))
            {
                Directory.CreateDirectory(objDirectory);
            }

            var binDirectory = project.GetBinDirectory(superProject);

            if(!Directory.Exists(binDirectory))
            {
                Directory.CreateDirectory(binDirectory);
            }

            outputLocation = binDirectory;            

            string executable = Path.Combine(outputLocation, project.Name);

            if (project.Type == ProjectType.StaticLibrary)
            {
                executable = Path.Combine(outputLocation, "lib" + project.Name);
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

            if (!File.Exists(executable) || compilationResults.NumberOfObjectsCompiled > 0)
            {
                console.WriteLine(string.Format("[LL]    [{0}]", project.Name));

                var linkResults = Link(console, superProject, project, compilationResults, outputLocation);

                if (linkResults.ExitCode == 0)
                {                    
                    if (project.Type == ProjectType.StaticLibrary)
                    {
                        result.LibraryLocations.Add(executable);
                    }
                    else
                    {
                        Size(console, project, linkResults);
                        result.ExecutableLocations.Add(executable);
                    }

                    result.NumberOfObjectsCompiled += compilationResults.NumberOfObjectsCompiled;
                }
                else
                {                    
                    result.ExitCode = -1;
                }

                console.WriteLine();
            }
            else
            {
                if (project.Type == ProjectType.StaticLibrary)
                {
                    result.LibraryLocations.Add(executable);
                }
                else
                {
                    result.ExecutableLocations.Add(executable);
                }

                if (superProject == project)
                {
                    Size(console, project, new LinkResult() { Executable = executable });
                    console.WriteLine();
                }
            }

            return result;
        }

        private async Task<CompileResult> BuildLibrary(IConsole console, Project superProject, Project project)
        {
            var result = new CompileResult();

            CompileResult referenceResults = new CompileResult();

            foreach (var reference in project.References)
            {
                var loadedReference = project.GetReference(reference);

                if (loadedReference == null)
                {
                    throw new Exception(string.Format("Unable to find reference {0}, in directory {1}", reference, project.Solution.Location));
                }


                if (loadedReference.Type == ProjectType.StaticLibrary)
                {
                    var refResult = await BuildReference(console, superProject, loadedReference);

                    if (refResult.ExitCode == 0)
                    {
                        foreach (var obj in refResult.ObjectLocations)
                        {
                            referenceResults.ObjectLocations.Add(obj);
                        }

                        referenceResults.NumberOfObjectsCompiled += refResult.NumberOfObjectsCompiled;
                    }
                    else
                    {
                        result.ExitCode = -1;

                        return result;
                    }
                }
                else
                {
                    var subResult = await BuildExecutable(console, loadedReference);

                    if (subResult.ExitCode == 0)
                    {
                        foreach (var executable in subResult.ExecutableLocations)
                        {
                            string outputDirectory = Path.Combine(project.Directory, "bin");

                            string destination = Path.Combine(outputDirectory, Path.GetFileName(executable));

                            if (!Directory.Exists(outputDirectory))
                            {
                                Directory.CreateDirectory(outputDirectory);
                            }

                            File.Copy(executable, destination, true);
                        }
                    }
                    else
                    {
                        result.ExitCode = -1;
                        return result;
                    }
                }
            }

            // bool hasBuiltSomething = false;

            //if (project.ToBuild(this, superProject))
            //{
            //    hasBuiltSomething = true;
            //    console.WriteLine(string.Format("[BB] - Building Library - {0}", project.Title));
            //}

            var compilationResult = await Compile(console, superProject, project);
            compilationResult.NumberOfObjectsCompiled += referenceResults.NumberOfObjectsCompiled;

            //if (hasBuiltSomething)
            //{
            //    console.WriteLine();
            //}

            if (compilationResult.ExitCode == 0)
            {
                compilationResult.ObjectLocations.AddRange(referenceResults.ObjectLocations);

                if (compilationResult.Count > 0)
                {
                    result.NumberOfObjectsCompiled += compilationResult.NumberOfObjectsCompiled;
                    result = Link(console, superProject, project, compilationResult);
                }

                return result;
            }
            else
            {
                return compilationResult;
            }
        }


        private async Task<CompileResult> Compile(IConsole console, Project superProject, Project project)
        {
            CompileResult result = new CompileResult();

            if (project.SourceFiles.Count == 0)
            {
                return result;
            }

            await Task.Factory.StartNew(() =>
            {
                var outputDirectory = project.GetOutputDirectory(superProject);

                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var objDirectory = project.GetObjectDirectory(superProject);

                if (!Directory.Exists(objDirectory))
                {
                    Directory.CreateDirectory(objDirectory);
                }

                Semaphore compileThread = new Semaphore(16, 16);
                int compileJobs = 0;
                object compileJobsLock = new object();


                foreach (var file in project.SourceFiles)
                {
                    if (Path.GetExtension(file.Location) == ".c" || Path.GetExtension(file.Location) == ".cpp")
                    {
                        var outputName = Path.GetFileNameWithoutExtension(file.Location) + ".o";
                        var dependencyFile = Path.Combine(objDirectory, Path.GetFileNameWithoutExtension(file.Location) + ".d");
                        var objectFile = Path.Combine(objDirectory, outputName);

                        bool dependencyChanged = false;
                        object resultLock = new object();

                        if (File.Exists(dependencyFile))
                        {
                            var dependencies = ProjectExtensions.GetDependencies(dependencyFile);

                            foreach (var dependency in dependencies)
                            {
                                if (!File.Exists(dependency) || File.GetLastWriteTime(dependency) > File.GetLastWriteTime(objectFile))
                                {
                                    dependencyChanged = true;
                                    break;
                                }
                            }
                        }

                        if (dependencyChanged || !File.Exists(objectFile))
                        {
                            compileThread.WaitOne();

                            lock (compileJobsLock)
                            {
                                compileJobs++;
                            }

                            console.WriteLine(string.Format("[CC]    [{0}]    {1}", project.Name, Path.GetFileName(file.Location)));

                            new Thread(() =>
                            {
                                this.Compile(console, superProject, project, file, objectFile, result);
                                compileThread.Release(1);

                                lock (resultLock)
                                {
                                    if (result.ExitCode == 0 && File.Exists(objectFile))
                                    {
                                        result.ObjectLocations.Add(objectFile);
                                        result.NumberOfObjectsCompiled++;
                                    }
                                    else
                                    {
                                        console.WriteLine("Compilation failed.");
                                    }
                                }

                                lock (compileJobsLock)
                                {
                                    compileJobs--;
                                }

                            }).Start();


                        }
                        else
                        {
                            result.ObjectLocations.Add(objectFile);
                        }
                    }
                    else
                    {
                        break;
                    }
                }


                while (compileJobs != 0)
                {
                    Thread.Sleep(10);
                };
            });

            return result;
        }

        private async Task<CompileResult> BuildReference(IConsole console, Project superProject, Project project)
        {
            var compileResults = new CompileResult();
            //bool hasBuiltSomething = false;

            foreach (var reference in project.References)
            {
                var loadedReference = project.GetReference(reference);

                if (loadedReference == null)
                {
                    throw new Exception(string.Format("Unable to find reference {0}, in directory {1}", reference, project.Solution.Location));
                }

                var result = await BuildReference(console, superProject, loadedReference);

                if (result.ExitCode == 0)
                {
                    compileResults.NumberOfObjectsCompiled += result.NumberOfObjectsCompiled;
                    compileResults.ObjectLocations.AddRange(result.ObjectLocations);
                }
                else
                {
                    compileResults.ExitCode = -1;
                    return compileResults;
                }
            }

            //if (project.ToBuild(this, superProject))
            //{
            //    console.WriteLine(string.Format("[BB] - Building Referenced Project - {0}", project.Title));
            //    hasBuiltSomething = true;
            //}

            var superResults = await Compile(console, superProject, project);

            //if (hasBuiltSomething)
            //{
            //    console.WriteLine();
            //}

            if (superResults.ExitCode == 0)
            {
                compileResults.NumberOfObjectsCompiled += superResults.NumberOfObjectsCompiled;
                compileResults.ObjectLocations.AddRange(superResults.ObjectLocations);
                return compileResults;
            }
            else
            {
                superResults.ExitCode = -1;
                return superResults;
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
