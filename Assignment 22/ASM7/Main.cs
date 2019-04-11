using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace Test {
   
    class MainClass
    {
        enum ExitStatus{
            NORMAL, DID_NOT_COMPILE, INFINITE_LOOP, UNKNOWN
        };

        static string maybeGet(XPathNavigator elem, string tag){
            var lst = elem.SelectChildren(tag,"");
            if(lst.Count == 0)
                return null;
            if(lst.Count > 1)
                throw new Exception("Too many");
            foreach(XPathNavigator x in lst) {
                return x.ToString().Replace("\r\n", "\n");
            }
            throw new Exception();
        }
        static List<string> maybeGetMulti(XPathNavigator elem, string tag){
            List<string> L = new List<string>();
            var lst = elem.SelectChildren(tag,"");
            if(lst.Count == 0)
                return L;
            foreach(XPathNavigator x in lst) {
                L.Add(x.ToString().Replace("\r\n", "\n"));
            }
            return L;
        }

        public static void Main(string[] args)
        {
            if(args.Length != 0) {
                Compiler.compile(args[0], args[0] + ".asm", args[0] + ".o", args[0] + ".exe");
                return;
            }

            string srcfile = "xyz.txt";
            string asmfile = "xyz.asm";
            string objfile = "xyz.o";
            string exefile = "xyz.exe";

            var inputfile = "inputs.txt";
            Console.WriteLine("Working directory: " + Environment.CurrentDirectory);
            Console.WriteLine("Reading inputs from " + inputfile);


            if(args.Length != 0) {
                Compiler.compile(args[0], asmfile, objfile, exefile);
                return;
            }
            //var doc = new System.Xml.XmlDocument();
            XPathDocument doc; 
            using(var sr = new StreamReader(inputfile)) {
                doc = new XPathDocument(sr);
            }

            //var root = doc.DocumentElement;
            var nav = doc.CreateNavigator();
            //var tests = root.GetElementsByTagName("test");
            foreach( XPathNavigator testelem in nav.SelectDescendants("test","",true) ){ //System.Xml.XmlElement testelem in tests){

                int lineNumber = (testelem as IXmlLineInfo).LineNumber;
                var shouldBeNull = maybeGet(testelem, "return");
                if(shouldBeNull != null)
                    throw new Exception("At line " + lineNumber + ": Use returns, not return");
                
                var expectedReturn = maybeGet(testelem, "returns");
                var expectedOutput = maybeGet(testelem, "output");
                var expectedInput = maybeGet(testelem, "input");
                var testcase = maybeGet(testelem, "code");

                //each array has: filename, expected contents, actual contents
                var expectedFiles = new List<string[]>();
                foreach(XPathNavigator fnode in testelem.SelectChildren("file","")) {
                    var nm = maybeGet(fnode, "name");
                    var co = maybeGet(fnode, "content");
                    if(nm == null || co == null)
                        throw new Exception();
                    expectedFiles.Add(new string[]{ nm, co, "" });
                }

                using(var sw = new StreamWriter(srcfile, false)) {
                    sw.Write(testcase);
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
                        Console.WriteLine("Test at line "+lineNumber+": Did not compile: " + e);
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
                        stdout = stdoutData.Result;
                        stderr = stderrData.Result;
                    }
                } 
                    
                
                bool ok = true;
                if(expectedReturn == "failure") {
                    ok = (exitStatus == ExitStatus.DID_NOT_COMPILE);
                } else if(expectedReturn == "infinite") {
                    ok = (exitStatus == ExitStatus.INFINITE_LOOP);
                } else {
                    if(exitStatus != ExitStatus.NORMAL)
                        ok = false;
                    else if( expectedReturn == null ){
                        //don't care what the value is
                    } else {
                        if(expectedReturn == "nonzero")
                            ok = (exitcode != 0);
                        else
                            ok = (exitcode == Convert.ToInt32(expectedReturn));
                    }
                }

                if(expectedOutput != null) {
                    if(expectedOutput != stdout.Replace("\r\n", "\n"))
                        ok = false;
                }

                foreach(var t in expectedFiles) {
                    try {
                        using(StreamReader rdr = new StreamReader(t[0])) {
                            t[2] = rdr.ReadToEnd();
                            if(t[2] != t[1])
                                ok = false;
                        }
                    } catch(IOException) {
                        ok = false;
                    }
                }


                if(ok) {
                    Console.WriteLine(lineNumber+": OK!");
                    foreach(var f in expectedFiles) {
                        File.Delete(f[0]);
                    }
                } else {
                    Console.WriteLine("At input.txt line " + lineNumber + ": ");
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
                            prettyprint(expectedOutput);
                            Console.WriteLine("Actual program output:");
                            prettyprint(stdout);
                            Console.WriteLine(stderr);
                        }
                        foreach(var t in expectedFiles) {
                            if(t[1] != t[2]) {
                                Console.WriteLine("Expected contents of file " + t[0]);
                                prettyprint(t[1]);
                                Console.WriteLine("Actual contents of file " + t[0]);
                                prettyprint(t[2]);
                            }
                        }
                        Console.WriteLine("-----------------");
                    }
                    //Console.ReadLine();
                    Environment.Exit(1);
                }
            }

            Console.WriteLine("All OK!");
            //Console.ReadLine();
        }

        static void prettyprint(string x){
            if(x == null){
                Console.WriteLine();
                return;
            }
            x = x.Replace("\n", "\u2424\n");
            x = x.Replace(" ", "\u2423");
            Console.WriteLine(x);
        }
    }
}
