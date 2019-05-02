using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Test {
   
    class MainClass
    {
        enum ExitStatus{
            NORMAL, DID_NOT_COMPILE, INFINITE_LOOP, UNKNOWN
        };
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
                    int i = testcase.IndexOf("//returns ");
                    string expectedReturn = testcase.Substring(i + 10).Split('\n')[0].Trim();

                    string expectedOutput = "";
                    var rex = new Regex(@"output is ""([^\n]*)""");
                    Match m = rex.Match(testcase);
                    if(m != null) {
                        expectedOutput = m.Groups[1].ToString().Replace("\\n", "\n"); 
                    }
                    //testcase.Substring(i + 10).Split('\n')[0].Trim().Replace("\\n", "\n");

                    //each array has: filename, expected contents, actual contents
                    var expectedFiles = new List<string[]>();
                    rex = new Regex(@"//output file (\S+) has ""([^\n]+)""");
                    foreach(Match mm in rex.Matches(testcase)) {
                        string fname = mm.Groups[1].ToString();
                        string fcontents = mm.Groups[2].ToString().Replace("\\n", "\n");
                        expectedFiles.Add(new string[] { 
                            fname, fcontents, ""
                        });
                    }

                    string expectedInput = "";
                    rex = new Regex(@"//input is ""([^\n]+)""");
                    m = rex.Match(testcase);
                    if(m != null) {
                        expectedInput = m.Groups[1].ToString().Trim().Replace("\\n", "\n");
                    }

                    ExitStatus exitStatus = ExitStatus.UNKNOWN;
                    if(expectedReturn == "failure") {
                        try {
                            Compiler.compile(srcfile, asmfile, objfile, exefile);
                        } catch(Exception) {
                            exitStatus = ExitStatus.DID_NOT_COMPILE;
                        }
                    } else {
                        try {
                            Compiler.compile(srcfile, asmfile, objfile, exefile);
                        } catch(Exception e) {
                            Console.WriteLine("Did not compile: "+e.Message);
                            exitStatus = ExitStatus.DID_NOT_COMPILE;
                            throw;
                        }
                    }


                    string stdout = "";
                    string stderr = "";
                    int exitcode=-1;

                    if(exitStatus != ExitStatus.DID_NOT_COMPILE) {
                        var si = new ProcessStartInfo();
                        si.FileName = exefile;
                        si.UseShellExecute = false;
                        si.RedirectStandardInput = true;
                        si.RedirectStandardOutput = true;
                        si.RedirectStandardError = true;
                        using(var proc = Process.Start(si)) {
                            proc.StandardInput.Write(expectedInput);
                            proc.StandardInput.Flush();
                            var stdoutData = proc.StandardOutput.ReadToEndAsync();
                            var stderrData = proc.StandardError.ReadToEndAsync();
                            proc.WaitForExit(1000);
                            if(!proc.HasExited) {
                                proc.Kill();
                                proc.WaitForExit();
                                exitStatus = ExitStatus.INFINITE_LOOP;
                                exitcode = -1;
                            } else {
                                exitcode = proc.ExitCode;
                                exitStatus = ExitStatus.NORMAL;
                            }
                            stdout = stdoutData.Result.Replace("\r\n","\n");
                            stderr = stderrData.Result;
                        }
                    } 
                        
                    
                    bool ok;
                    if(expectedReturn == "failure") {
                        ok = (exitStatus == ExitStatus.DID_NOT_COMPILE);
                    } else if(expectedReturn == "infinite") {
                        ok = (exitStatus == ExitStatus.INFINITE_LOOP);
                    } else {
                        if(exitStatus != ExitStatus.NORMAL)
                            ok = false;
                        else {
                            if(expectedReturn == "nonzero")
                                ok = (exitcode != 0);
                            else
                                ok = (exitcode == Convert.ToInt32(expectedReturn));
                        }
                    }

                    if(expectedOutput != "" && expectedOutput != stdout)
                        ok = false;

                    foreach(var t in expectedFiles) {
                        try {
                            using(StreamReader rdr = new StreamReader(t[0])) {
                                t[2] = rdr.ReadToEnd().Replace("\r\n","\n");
                                if(t[2] != t[1])
                                    ok = false;
                            }
                        } catch(IOException) {
                            ok = false;
                        }
                    }


                    if(ok) {
                        Console.WriteLine("OK!");
                    } else {
                        Console.WriteLine(testcase);
                        if(exitStatus == ExitStatus.DID_NOT_COMPILE) {
                            Console.WriteLine("Error: Did not compile but it should have");
                        } else if( exitStatus == ExitStatus.INFINITE_LOOP ){
                            Console.WriteLine("Infinite loop detected");
                        } else {
                            Console.WriteLine("-----------------");
                            Console.WriteLine("Error: Expectation mismatch");
                            if(expectedReturn != ""+exitcode) {
                                Console.WriteLine("Expected return code from main():");
                                Console.WriteLine(expectedReturn);
                                Console.WriteLine("Actual return code from main():");
                                Console.WriteLine(exitcode);
                            }
                            if(expectedOutput != "" && expectedOutput != stdout) {
                                Console.WriteLine("Expected program output: ");
                                Console.WriteLine(expectedOutput);
                                Console.WriteLine("Actual program output:");
                                Console.WriteLine(stdout);
                                Console.WriteLine(stderr);
                            }
                            foreach(var t in expectedFiles) {
                                if(t[1] != t[2]) {
                                    Console.WriteLine("Expected contents of file " + t[0]);
                                    Console.WriteLine(t[1]);
                                    Console.WriteLine("Actual contents of file " + t[0]);
                                    Console.WriteLine(t[2]);
                                }
                            }
                            Console.WriteLine("-----------------");
                        }
                        //Console.ReadLine();
                        Environment.Exit(1);
                    }
                }
            }

            Console.WriteLine("All OK!");
            Console.ReadLine();
        }
    }
}
