namespace VEBuild
{
    using System.IO;
    using VEBuild.Models;

    class Program
    {
        static void Main(string[] args)
        {
            string baseDir = @"c:\development\vebuild\test";
            string solutionExtension = "vebuild";
            string projectExtension = "build";

            if(!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            var solution = new Solution();

            solution.Name = "UHLD";
            solution.Projects.Add(new ProjectDescription() { Name = "CardLaminator" });

            string solutionFile = Path.Combine(baseDir, string.Format("{0}.{1}", solution.Name, solutionExtension));
            solution.Serialize(solutionFile);

            var project = new Project();

            project.Name = "RevBBoard";
            project.Languages.Add(Language.C);
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.Executable;

            project.References.Add("ArmSystem");
            project.References.Add("GxBootloader");

            project.PublicIncludes.Add("./");
            project.Includes.Add("./USB/include");

            project.SourceFiles.Add(new SourceFile { File = "main.c" });
            project.SourceFiles.Add(new SourceFile { File = "startup.c", Flags = "-std=gnu99" });

            var projectFile = Path.Combine(baseDir, string.Format("{0}.{1}", project.Name, projectExtension));
            project.Serialize(projectFile);

            var deserializedSolution = Solution.Deserialize(solutionFile);
            var deserializedProject = Project.Deserialize(projectFile);
        }
    }
}
