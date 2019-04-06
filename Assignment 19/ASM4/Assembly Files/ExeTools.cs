using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;

namespace ExeTools
{
    class ExeTools
    {
        public enum OS
        {
            WIN, MAC, LINUX, UNKNOWN
        }

        public static void Run(string input,string cmd, params string[] argsA)
        {
            string args;
            if (argsA.Length == 0)
                args = "";
            else {
                string[] tmp = new string[argsA.Length];
                for (int i = 0; i < argsA.Length; ++i)
                {
                    tmp[i] = '"' + argsA[i] + '"';
                }
                args = string.Join(" ", argsA);
            }
            //Console.WriteLine(cmd+" "+args);
            var si = new ProcessStartInfo();
            si.FileName = cmd;
            si.Arguments = args;
            si.RedirectStandardInput = true;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.UseShellExecute = false;
            var proc = new Process(); //Process.Start(si);
            proc.StartInfo = si;
            proc.OutputDataReceived += (s, a) => { Console.Write(a.Data); };
            proc.ErrorDataReceived += (s, a) => { Console.Write(a.Data); };
            proc.Start();
            if( input.Length > 0 ){
                proc.StandardInput.Write(input);
                proc.StandardInput.Write("\n");
            }
            proc.StandardInput.Flush();
            proc.StandardInput.Close();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                throw new Exception("Process failed: " + cmd + " "+args);
            }
        }

        /** Input: Assembly code.
         * Returns: Object file*/
        public static void Assemble(string asmfile, string objfile)
        {
            switch (OperatingSystem)
            {
                case OS.LINUX:
                    Run("", "nasm", "-f", "elf64", "-o", objfile, asmfile);
                    break;
                case OS.WIN:
                    Run("", "nasm", "-f", "win64", "-o", objfile, asmfile);
                    break;
                case OS.MAC:
                    Run("", "nasm", "--prefix", "_", "-f", "macho64", "-o", objfile, asmfile);
                    break;
            }
        }

        public static void Link(string objfile, string exefile)
        {
            System.IO.File.Delete(exefile);
            switch (OperatingSystem)
            {
                case OS.LINUX:
                    Run("","gcc", "-m64", objfile, "-o", exefile);
                    break;
                case OS.WIN:
                    var inp = String.Format(@"
""c:\program files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvars64.bat"" 
cd ""{0}""
link ""{1}"" ""/OUT:{2}"" /SUBSYSTEM:CONSOLE /nologo msvcrt.lib",
                        Directory.GetCurrentDirectory(), objfile, exefile);
                    Run(inp,"cmd");
                    break;
                case OS.MAC:
                    Run("", "ld", "-o", exefile, objfile, "-macosx_version_min", "10.13", "-lSystem");
                    break;
            }
            if (!System.IO.File.Exists(exefile))
                throw new Exception("Link failed");
        }

        static OS opsys=OS.UNKNOWN;
        public static OS OperatingSystem {
            get {
                if(opsys != OS.UNKNOWN)
                    return opsys;
                var plat = System.Environment.OSVersion.Platform;
                if(plat == PlatformID.MacOSX)
                    opsys = OS.MAC;
                else if(plat == PlatformID.Unix) {
                    //https://stackoverflow.com/questions/38790802/determine-operating-system-in-net-core
                    try {
                        var s = System.IO.File.ReadAllText("/proc/sys/kernel/ostype");
                        if(s.IndexOf("inux") != -1)
                            opsys = OS.LINUX;
                        else
                            opsys = OS.MAC;
                    } catch(Exception) {
                        opsys = OS.MAC;
                    }
                } else
                    opsys = OS.WIN;
                return opsys;
            }
        }
    }

}
