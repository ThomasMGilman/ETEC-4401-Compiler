using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;

namespace Test {
   
    class MainClass
    {
        public static void Main(string[] args)
        {
            string srcfile = Path.Combine(Environment.CurrentDirectory,"xyz.txt");
            string asmfile = Path.Combine(Environment.CurrentDirectory,"xyz.asm");
            string objfile = Path.Combine(Environment.CurrentDirectory,"xyz.o");
            string exefile = Path.Combine(Environment.CurrentDirectory,"xyz.exe");

            var inputfile = "inputs.txt";
            Console.WriteLine("Working directory: " + Environment.CurrentDirectory);
            Console.WriteLine("Reading inputs from " + inputfile);

            using(var sr = new StreamReader(inputfile)) {
                string txt = sr.ReadToEnd();
                foreach(var testcase1 in txt.Split( new string[]{"//-"}, StringSplitOptions.RemoveEmptyEntries ) ){
                    var testcase = testcase1.Trim();
                    if(testcase.Length == 0)
                        continue;
                    using(var sw = new StreamWriter(srcfile,false)) {
                        sw.Write(testcase);
                    }
                    int i = testcase.IndexOf("//");
                    string expected = testcase.Substring(i + 2).Split('\n')[0].Trim();
                    Compiler.compile(srcfile, asmfile, objfile, exefile);
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
                    bool infiniteLoop=false;
                    proc.WaitForExit(1000);
                    if(!proc.HasExited) {
                        proc.Kill();
                        proc.WaitForExit();
                        infiniteLoop = true;
                    }
                    bool ok;
                    if(expected == "nonzero") {
                        ok = (proc.ExitCode != 0 && !infiniteLoop );
                    } else if(expected == "infinite") {
                        ok = (infiniteLoop == true);
                    } else {
                        ok = (proc.ExitCode == Convert.ToInt32(expected) && !infiniteLoop) ;
                    }
                    if(ok) {
                        Console.WriteLine("OK! "+ (infiniteLoop ? "infinite":""+proc.ExitCode)+" "+expected);
                    } else {
                        Console.WriteLine("Error: Got " + proc.ExitCode + " but expected " + expected);
                        Console.ReadLine();
                        Environment.Exit(1);
                    }
                }
            }
            Console.WriteLine("All OK! Press 'enter' to quit");
            Console.ReadLine();
        }
    }
}
