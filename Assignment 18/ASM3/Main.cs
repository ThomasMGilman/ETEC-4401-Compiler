using System;
using System.IO;
using System.Diagnostics;

namespace Test {
   
    class MainClass
    {
        public static void Main(string[] args)
        {
            string srcfile = "xyz.txt";
            string asmfile = "xyz.asm";
            string objfile = "xyz.o";
            string exefile = "xyz.exe";

            var inputfile = "inputs.txt";
            Console.WriteLine("Working directory: " + Environment.CurrentDirectory);
            Console.WriteLine("Reading inputs from " + inputfile);

            using(var sr = new StreamReader(inputfile)) {
                string txt = sr.ReadToEnd();
                foreach(var testcase1 in txt.Split( new string[]{"//-"}, StringSplitOptions.RemoveEmptyEntries )) {
                    var testcase = testcase1.Trim();
                    if(testcase.Length == 0)
                        continue;
                    using(var sw = new StreamWriter(srcfile, false)) {
                        sw.Write(testcase);
                    }
                    int i = testcase.IndexOf("//");
                    string expected = testcase.Substring(i + 2).Split('\n')[0].Trim();
                    bool compiled;
                    if(expected == "fail") {
                        try {
                            Compiler.compile(srcfile, asmfile, objfile, exefile);
                            compiled = true;
                        } catch(Exception e) {
                            Console.WriteLine(e.Message);
                            compiled = false;
                        }
                    } else{
                        Compiler.compile(srcfile, asmfile, objfile, exefile);
                        compiled = true;
                    }
                    int exitcode;
                    bool infiniteLoop = false;
                    if(compiled) {
                        var si = new ProcessStartInfo();
                        si.FileName = exefile;
                        si.UseShellExecute = false;
                        si.RedirectStandardOutput = true;
                        si.RedirectStandardError = true;
                        var proc = Process.Start(si);
                        proc.OutputDataReceived += (s, e) => { Console.WriteLine(e.Data); };
                        proc.ErrorDataReceived += (s, e) => { Console.WriteLine(e.Data); };
                        proc.BeginOutputReadLine();
                        proc.BeginErrorReadLine();
                        proc.WaitForExit(1000);
                        if(!proc.HasExited) {
                            proc.Kill();
                            proc.WaitForExit();
                            infiniteLoop = true;
                            exitcode = -1;
                        } else {
                            exitcode = proc.ExitCode;
                        }
                    } else {
                        exitcode = -1;
                    }
                        
                    bool ok;
                    if(!compiled) {
                        if(expected == "fail")
                            ok = true;
                        else
                            ok = false;
                    } else if(expected == "fail")
                        ok = false;
                    else if(expected == "nonzero") {
                        ok = (exitcode != 0 && !infiniteLoop);
                    } else if(expected == "infinite") {
                        ok = (infiniteLoop == true);
                    } else {
                        ok = (exitcode == Convert.ToInt32(expected) && !infiniteLoop) ;
                    }
                    if(ok) {
                        Console.WriteLine("OK! "+ (infiniteLoop ? "infinite":""+exitcode)+" "+expected);
                    } else {
                        Console.WriteLine(testcase);
                        if(!compiled) {
                            Console.WriteLine("Error: Did not compile");
                        } else {
                            Console.WriteLine("Error: Got " + exitcode + " but expected " + expected);
                        }
                        Console.ReadLine();
                        Environment.Exit(1);
                    }
                }
            }
            Console.WriteLine("All OK!");
            Console.ReadLine();
        }
    }
}
