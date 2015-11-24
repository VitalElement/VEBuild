namespace VEBuild
{
    using System;
    using System.IO;
    using VEBuild.Models;

    class Program
    {        
        const string baseDir = @"c:\development\vebuild\test";        

        static void Main(string[] args)
        {
            GenerateTestProjects();

            var solution = Solution.Load(Path.Combine(baseDir, "UHLD.vsln"));

            var gccSettings = new ToolchainSettings();
            gccSettings.ToolChainLocation = @"c:\vestudio\appdata\repos\GCCToolchain\bin";
            gccSettings.IncludePaths.Add("arm-none-eabi\\include\\c++\\4.9.3");
            gccSettings.IncludePaths.Add("arm-none-eabi\\include\\c++\\4.9.3\\arm-none-eabi\\thumb");
            gccSettings.IncludePaths.Add("lib\\gcc\\arm-none-eabi\\4.9.3\\include");

            var toolchain = new GCCToolChain(gccSettings);
            var console = new ProgramConsole();
            var project = solution.LoadedProjects[1];
            var awaiter = toolchain.Clean(console, project);
            awaiter.Wait();

            awaiter = toolchain.Build(console, project);
            awaiter.Wait();

            Console.ReadKey();
        }

        static void GenerateTestProjects()
        {
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            var solution = new Solution();

            solution.Name = "UHLD";
            solution.Projects.Add(new ProjectDescription() { Name = "ArmSystem" });
            solution.Projects.Add(new ProjectDescription() { Name = "STM32DiscoveryBootloader" });

            string solutionFile = Path.Combine(baseDir, string.Format("{0}.{1}", solution.Name, Solution.solutionExtension));
            solution.Serialize(solutionFile);

            var project = new Project();

            project.Name = "ArmSystem";
            project.Languages.Add(Language.C);
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.StaticLibrary;

            project.PublicIncludes.Add("./");

            project.SourceFiles.Add(new SourceFile { File = "allocator.c" });
            project.SourceFiles.Add(new SourceFile { File = "startup.c" });
            project.SourceFiles.Add(new SourceFile { File = "syscalls.c" });
            project.SourceFiles.Add(new SourceFile { File = "CPPSupport.cpp" });

            var projectDir = Path.Combine(baseDir, project.Name);

            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            var projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            project.Serialize(projectFile);

            project = new Project();

            project.Name = "STM32DiscoveryBootloader";
            project.Languages.Add(Language.C);
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.Executable;

            project.Includes.Add("./");

            project.References.Add("ArmSystem");

            project.SourceFiles.Add(new SourceFile { File = "startup_stm32f40xx.c" });
            project.SourceFiles.Add(new SourceFile { File = "main.cpp" });
            //project.SourceFiles.Add(new SourceFile { File = "Startup.cpp" });
            //project.SourceFiles.Add(new SourceFile { File = "CPPSupport.cpp" });

            project.ToolChainArguments.Add("-mcpu=cortex-m4");
            project.ToolChainArguments.Add("-mthumb");
            project.ToolChainArguments.Add("-mfpu=fpv4-sp-d16");
            project.ToolChainArguments.Add("-mfloat-abi=hard");
            project.CompilerArguments.Add("-ffunction-sections");
            project.CompilerArguments.Add("-fdata-sections");
            project.CompilerArguments.Add("-Wno-unknown-pragmas");

            project.CppCompilerArguments.Add("-fno-rtti");
            project.CppCompilerArguments.Add("-fno-exceptions");

            project.BuiltinLibraries.Add("m");
            project.BuiltinLibraries.Add("c_nano");
            project.BuiltinLibraries.Add("supc++_nano");
            project.BuiltinLibraries.Add("stdc++_nano");

            project.LinkerScript = "link.ld";

            project.BuildDirectory = "build";

            projectDir = Path.Combine(baseDir, project.Name);

            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            project.Serialize(projectFile);

            var deserializedSolution = Solution.Deserialize(solutionFile);
            var deserializedProject = Project.Deserialize(projectFile);
        }
    }
}
