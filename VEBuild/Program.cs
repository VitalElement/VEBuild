﻿namespace VEBuild
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
            var project = solution.LoadedProjects[2];
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
            solution.Projects.Add(new ProjectDescription() { Name = "STM32F4Cube" });
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

            project.Defines.Add("__FPU_USED");
            project.Defines.Add("STM32F407xx");

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

            project.Name = "STM32DiscoveryBootloader";
            project.Languages.Add(Language.C);
            project.Languages.Add(Language.Cpp);

            project.Type = ProjectType.Executable;

            project.Includes.Add("./");

            project.References.Add("ArmSystem");
            project.References.Add("STM32F4Cube");

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
