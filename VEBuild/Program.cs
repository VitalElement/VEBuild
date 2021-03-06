﻿namespace VEBuild
{
    using System;
    using System.IO;
    using VEBuild.Models;
    using CommandLine;
    using CommandLine.Text;
    using System.Linq;

    class Program
    {
        const string baseDir = @"c:\development\vebuild\test";

        static Solution LoadSolution(ProjectOption options)
        {
            bool inProjectDirectory = false;

            if (string.IsNullOrEmpty(options.Project))
            {
                inProjectDirectory = true;
                options.Project = Path.GetFileNameWithoutExtension(Directory.GetCurrentDirectory());
            }

            var solutionDirectory = Directory.GetCurrentDirectory();

            if (inProjectDirectory)
            {
                solutionDirectory = Directory.GetParent(solutionDirectory).FullName;
            }

            var solution = Solution.Load(solutionDirectory);

            return solution;
        }

        static Project FindProject(Solution solution, string project)
        {
            try
            {
                var result = solution.FindProject(project);

                return result;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        static int RunBuild(BuildOptions options)
        {
            var solution = LoadSolution(options);
            var project = FindProject(solution, options.Project);

            var gccSettings = new ToolchainSettings();
            gccSettings.ToolChainLocation = @"c:\vestudio\appdata\repos\GCCToolchain\bin";
            gccSettings.IncludePaths.Add("arm-none-eabi\\include\\c++\\4.9.3");
            gccSettings.IncludePaths.Add("arm-none-eabi\\include\\c++\\4.9.3\\arm-none-eabi\\thumb");
            gccSettings.IncludePaths.Add("lib\\gcc\\arm-none-eabi\\4.9.3\\include");

            var toolchain = new GccToolChain(gccSettings);

            toolchain.Jobs = options.Jobs;
            var console = new ProgramConsole();
            
            if (project != null)
            {
                var stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();

                try {
                    project.ResolveReferences(console);
                }
                catch (Exception e)
                {
                    console.WriteLine(e.Message);
                }

                var awaiter = toolchain.Build(console, project);
                awaiter.Wait();

                stopWatch.Stop();
                console.WriteLine(stopWatch.Elapsed.ToString());
            }
            else
            {
                console.WriteLine("Nothing to build.");
            }

            return 1;
        }

        static int RunClean(CleanOptions options)
        {
            var solution = LoadSolution(options);

            var gccSettings = new ToolchainSettings();
            gccSettings.ToolChainLocation = @"c:\vestudio\appdata\repos\GCCToolchain\bin";
            gccSettings.IncludePaths.Add("arm-none-eabi\\include\\c++\\4.9.3");
            gccSettings.IncludePaths.Add("arm-none-eabi\\include\\c++\\4.9.3\\arm-none-eabi\\thumb");
            gccSettings.IncludePaths.Add("lib\\gcc\\arm-none-eabi\\4.9.3\\include");

            var toolchain = new GccToolChain(gccSettings);

            var console = new ProgramConsole();

            var project = FindProject(solution, options.Project);

            if (project != null)
            {
                toolchain.Clean(console, project).Wait();
            }
            else
            {
                console.WriteLine("Nothing to clean.");
            }

            return 1;
        }

        static string NormalizePath (string path)
        {
            if (path != null)
            {
                return Path.GetFullPath(new Uri(path).LocalPath)
                           .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else
            {
                return null;
            }
        }

        static int RunRemove (RemoveOptions options)
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), options.File);

            if (File.Exists(file))
            {
                var solution = LoadSolution(options);
                var project = FindProject(solution, options.Project);

                if (project != null)
                {
                    // todo normalize paths.
                    var currentFile = project.SourceFiles.Where((s) => s.File.Normalize() == options.File.Normalize()).FirstOrDefault();

                    if (currentFile != null)
                    {
                        project.SourceFiles.RemoveAt(project.SourceFiles.IndexOf(currentFile));                        
                        project.Save();
                        Console.WriteLine("File removed.");

                        return 1;
                    }
                    else
                    {
                        Console.WriteLine("File not found in project.");
                        return -1;
                    }
                    
                }
                else
                {
                    Console.WriteLine("Project not found.");
                    return -1;
                }
            }
            else
            {
                Console.WriteLine("File not found.");
                return -1;
            }
        }

        static int RunAdd(AddOptions options)
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), options.File);

            if (File.Exists(file))
            {
                var solution = LoadSolution(options);
                var project = FindProject(solution, options.Project);

                if(project != null)
                {
                    project.SourceFiles.Add(new SourceFile { File = options.File });
                    project.Save();
                    Console.WriteLine("File added.");
                    return 1;
                }
                else
                {
                    Console.WriteLine("Project not found.");
                    return -1;
                }
            }
            else
            {
                Console.WriteLine("File not found.");
                return -1;
            }
        }

        static int RunAddReference(AddReferenceOptions options)
        {
            var solution = LoadSolution(options);
            var project = FindProject(solution, options.Project);

            if (project != null)
            {
                var currentReference = project.References.Where((r) => r.Name == options.Name).FirstOrDefault();
                
                if (currentReference != null)
                {
                    project.References[project.References.IndexOf(currentReference)] = new Reference { Name = options.Name, GitUrl = options.GitUrl, Revision = options.Revision };
                }
                else
                {
                    project.References.Add(new Reference { Name = options.Name, GitUrl = options.GitUrl, Revision = options.Revision });
                }

                project.Save();

                Console.WriteLine("Successfully added reference.");
            }
            
            return 1;
        }

        static int RunCreate(CreateOptions options)
        {
            string projectPath = string.Empty;

            if (string.IsNullOrEmpty(options.Project))
            {
                projectPath = Directory.GetCurrentDirectory();
                options.Project = Path.GetFileNameWithoutExtension(projectPath);
            }
            else
            {
                projectPath = Path.Combine(Directory.GetCurrentDirectory(), options.Project);
            }

            if(!Directory.Exists(projectPath))
            {
                Directory.CreateDirectory(projectPath);                
            }

            var project = Project.Create(projectPath, options.Project);
            project.Type = options.Type;
            project.Save();

            if(project != null)
            {
                Console.WriteLine("Project created successfully.");
                return 1;
            }
            else
            {
                Console.WriteLine("Unable to create project. May already exist.");
                return -1;
            }
        }

        static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<AddOptions, RemoveOptions, AddReferenceOptions, BuildOptions, CleanOptions, CreateOptions>(args).MapResult(
              (BuildOptions opts) => RunBuild(opts),
                (AddOptions opts) => RunAdd(opts),
                (AddReferenceOptions opts) => RunAddReference(opts),
              (CleanOptions opts) => RunClean(opts),
              (CreateOptions opts) => RunCreate(opts),
              (RemoveOptions opts)=>RunRemove(opts),
              errs => 1);

            return result;
        }

        static void GenerateTestProjects()
        {
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

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

            //if (!Directory.Exists(projectDir))
            //{
            //    Directory.CreateDirectory(projectDir);
            //}

            var projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            //project.Serialize(projectFile);

            project = new Project();

            project.Name = "STM32F4Cube";
            project.Languages.Add(Language.C);
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.StaticLibrary;

            project.PublicIncludes.Add("../STM32DiscoveryBootloader");
            project.PublicIncludes.Add("../STM32HalPlatform/USB/CustomHID");
            project.PublicIncludes.Add("./Drivers/STM32F4xx_HAL_Driver/Inc");
            project.PublicIncludes.Add("Middlewares/ST/STM32_USB_Device_Library/Core/Inc");
            project.PublicIncludes.Add("Drivers/CMSIS/Device/ST/STM32F4xx/Include");
            project.PublicIncludes.Add("Drivers/CMSIS/Include");

            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_adc.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_adc_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_can.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_cec.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_cortex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_crc.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_cryp.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_cryp_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_dac.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_dac_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_dcmi.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_dcmi_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_dma.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_dma_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_dma2d.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_eth.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_flash.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_flash_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_flash_ramfunc.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_fmpi2c.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_fmpi2c_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_gpio.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_hash.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_hash_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_hcd.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_i2c.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_i2c_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_i2s.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_i2s_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_irda.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_iwdg.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_ltdc.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_msp_template.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_nand.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_nor.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_pccard.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_pcd.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_pcd_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_pwr.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_pwr_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_qspi.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_rcc.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_rcc_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_rng.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_rtc.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_rtc_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_sai.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_sai_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_sd.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_sdram.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_smartcard.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_spdifrx.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_spi.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_sram.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_tim.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_tim_ex.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_uart.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_usart.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_hal_wwdg.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_ll_fmc.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_ll_fsmc.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_ll_sdmmc.c" });
            project.SourceFiles.Add(new SourceFile { File = "./Drivers/STM32F4xx_HAL_Driver/Src/stm32f4xx_ll_usb.c" });
            project.SourceFiles.Add(new SourceFile { File = "Middlewares/ST/STM32_USB_Device_Library/Core/Src/usbd_conf_template.c" });
            project.SourceFiles.Add(new SourceFile { File = "Middlewares/ST/STM32_USB_Device_Library/Core/Src/usbd_core.c" });
            project.SourceFiles.Add(new SourceFile { File = "Middlewares/ST/STM32_USB_Device_Library/Core/Src/usbd_ctlreq.c" });
            project.SourceFiles.Add(new SourceFile { File = "Middlewares/ST/STM32_USB_Device_Library/Core/Src/usbd_ioreq.c" });
            project.SourceFiles.Add(new SourceFile { File = "Drivers/CMSIS/Device/ST/STM32F4xx/Source/Templates/system_stm32f4xx.c" });


            projectDir = Path.Combine(baseDir, project.Name);

            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            project.Serialize(projectFile);

            project = new Project();

            project.Name = "IntegratedDebugProtocol";
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.StaticLibrary;

            project.PublicIncludes.Add("./");

            project.SourceFiles.Add(new SourceFile { File = "IDP.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "IDPPacket.cpp" });

            project.References.Add(new Reference { Name = "Utils" });

            projectDir = Path.Combine(baseDir, project.Name);

            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            project.Serialize(projectFile);

            project = new Project();

            project.Name = "CommonHal";
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.StaticLibrary;

            project.PublicIncludes.Add("./");

            project.References.Add(new Reference { Name = "Utils" });

            project.SourceFiles.Add(new SourceFile { File = "II2C.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "Interrupt.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "IPort.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "ISpi.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "IUsbHidDevice.cpp" });
            //project.SourceFiles.Add(new SourceFile { File = "SerialPort.cpp" });

            projectDir = Path.Combine(baseDir, project.Name);

            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            project.Serialize(projectFile);

            project = new Project();

            project.Name = "STM32HalPlatform";
            project.Languages.Add(Language.Cpp);
            project.Languages.Add(Language.C);

            project.Type = ProjectType.StaticLibrary;

            project.PublicIncludes.Add("./");
            project.PublicIncludes.Add("./USB");

            project.SourceFiles.Add(new SourceFile { File = "SignalGeneration/STM32FrequencyChannel.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "SignalGeneration/STM32PwmChannel.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "USB/CustomHID/usb_device.c" });
            project.SourceFiles.Add(new SourceFile { File = "USB/CustomHID/usbd_conf.c" });
            project.SourceFiles.Add(new SourceFile { File = "USB/CustomHID/usbd_customhid.c" });
            project.SourceFiles.Add(new SourceFile { File = "USB/CustomHID/usbd_desc.c" });
            project.SourceFiles.Add(new SourceFile { File = "USB/STM32UsbHidDevice.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "STM32Adc.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "STM32BootloaderService.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "STM32InputCaptureChannel.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "STM32QuadratureEncoder.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "STM32Timer.cpp" });

            project.References.Add(new Reference { Name = "CommonHal" });
            project.References.Add(new Reference { Name = "STM32F4Cube" });
            project.References.Add(new Reference { Name = "Utils" });

            projectDir = Path.Combine(baseDir, project.Name);

            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            project.Serialize(projectFile);

            project = new Project();

            project.Name = "Utils";
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.StaticLibrary;

            project.PublicIncludes.Add("./");

            project.SourceFiles.Add(new SourceFile { File = "CRC.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "Event.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "PidController.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "StraightLineFormula.cpp" });

            projectDir = Path.Combine(baseDir, project.Name);

            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            project.Serialize(projectFile);

            project = new Project();

            project.Name = "GxInstrumentationHidDevice";
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.StaticLibrary;

            project.PublicIncludes.Add("./");

            project.SourceFiles.Add(new SourceFile { File = "GxInstrumentationHidDevice.cpp" });

            project.References.Add(new Reference { Name = "CommonHal" });
            project.References.Add(new Reference { Name = "IntegratedDebugProtocol" });
            project.References.Add(new Reference { Name = "STM32F4Cube" });

            projectDir = Path.Combine(baseDir, project.Name);

            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            project.Serialize(projectFile);

            project = new Project();

            project.Name = "Dispatcher";
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.StaticLibrary;

            project.PublicIncludes.Add("./");

            project.SourceFiles.Add(new SourceFile { File = "Dispatcher.cpp" });

            project.References.Add(new Reference { Name = "Utils" });

            projectDir = Path.Combine(baseDir, project.Name);

            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            project.Serialize(projectFile);

            project = new Project();

            project.Name = "GxBootloader";
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.StaticLibrary;

            project.PublicIncludes.Add("./");

            project.SourceFiles.Add(new SourceFile { File = "GxBootloader.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "GxBootloaderHidDevice.cpp" });

            project.References.Add(new Reference { Name = "IntegratedDebugProtocol" });
            project.References.Add(new Reference { Name = "Utils" });
            project.References.Add(new Reference { Name = "GxInstrumentationHidDevice" });
            project.References.Add(new Reference { Name = "Dispatcher" });

            projectDir = Path.Combine(baseDir, project.Name);

            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            project.Serialize(projectFile);

            project = new Project();

            project.Name = "STM32DiscoveryBootloader";
            project.Languages.Add(Language.C);
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.Executable;

            project.Includes.Add("./");

            project.References.Add(new Reference { Name = "ArmSystem", GitUrl = "http://gxgroup.duia.eu/gx/ArmSystem.git", Revision = "HEAD" });
            project.References.Add(new Reference { Name = "CommonHal" });
            project.References.Add(new Reference { Name = "GxBootloader" });
            project.References.Add(new Reference { Name = "STM32F4Cube" });
            project.References.Add(new Reference { Name = "STM32HalPlatform" });

            project.SourceFiles.Add(new SourceFile { File = "startup_stm32f40xx.c" });
            project.SourceFiles.Add(new SourceFile { File = "main.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "Startup.cpp" });
            project.SourceFiles.Add(new SourceFile { File = "DiscoveryBoard.cpp" });

            project.ToolChainArguments.Add("-mcpu=cortex-m4");
            project.ToolChainArguments.Add("-mthumb");
            project.ToolChainArguments.Add("-mfpu=fpv4-sp-d16");
            project.ToolChainArguments.Add("-mfloat-abi=hard");

            project.ToolChainArguments.Add("-fno-exceptions");
            project.ToolChainArguments.Add("-O3");
            project.ToolChainArguments.Add("-Os");

            project.CompilerArguments.Add("-ffunction-sections");
            project.CompilerArguments.Add("-fdata-sections");
            project.CompilerArguments.Add("-Wno-unknown-pragmas");

            project.CppCompilerArguments.Add("-fno-rtti");


            project.BuiltinLibraries.Add("m");
            project.BuiltinLibraries.Add("c_nano");
            project.BuiltinLibraries.Add("supc++_nano");
            project.BuiltinLibraries.Add("stdc++_nano");

            project.Defines.Add("__FPU_USED");
            project.Defines.Add("STM32F407xx");

            project.LinkerScript = "link.ld";

            project.BuildDirectory = "build";

            projectDir = Path.Combine(baseDir, project.Name);

            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            projectFile = Path.Combine(projectDir, string.Format("{0}.{1}", project.Name, Solution.projectExtension));
            project.Serialize(projectFile);
        }
    }
}
