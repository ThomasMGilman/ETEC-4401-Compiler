//Thomas Gilman
//James Hudson
//ETEC 4401 Compiler
//Assignment 1 Regex
// 1/16/2019
using System.Windows.Forms;
using System;
using System.Text.RegularExpressions;
using System.Collections;

namespace Assignment1_Regex
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            string infile;
            int lineNum = 0;
            int num_error = 0;
            String[] lines;
            Hashtable Terminals = new Hashtable();
            Regex newReg;
            Regex middle = new Regex(@"->");

            if (args.Length == 0)
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "All files|*.*";
                dlg.ShowDialog();
                infile = dlg.FileName;
                if (infile.Trim().Length == 0)
                    return;
                dlg.Dispose();
            }
            else
            {
                infile = args[0];
            }

            Console.WriteLine("Got File");
            lines = System.IO.File.ReadAllLines(@infile);
            foreach (string line in lines)
            {
                line.Trim();
                if (line.Length == 0) //blank line
                    break;
                else
                {
                    lineNum++;
                    int index = middle.Match(line).Index;
                    var mid = middle.Match(line);
                    var rhs = line.Substring(index + mid.Length);
                    var Terminal = line.Substring(0, index);
                    rhs = rhs.Trim();
                    Terminal = Terminal.Trim();
                    if(rhs.Length > 0 && Terminal.Length > 0)
                    {
                        try
                        {
                            newReg = new Regex(rhs);                    //throws exception if Regex is not valid
                            if (Terminals.Contains(Terminal))           //check if key already in table, if it is error
                            {
                                num_error++;
                                Console.WriteLine("ERROR {0}: lhs already in Table Key: {1}", num_error, Terminal);
                            }
                            else
                                Terminals.Add(Terminal, rhs);
                        }
                        catch
                        {
                            num_error++;
                            Console.WriteLine("ERROR {0}: invalid Regex: {1}", num_error, rhs);
                        }
                    }
                    else
                    {
                        num_error++;
                        Console.WriteLine("ERROR {0}: '{1} -> {2}' has invalid (rhs or lhs)", num_error, Terminal, rhs);
                    }
                }
            }
            Console.WriteLine("Regex List:");
            foreach (DictionaryEntry item in Terminals)         //list keys and their values
                Console.WriteLine("\tKey: {0}, Value: {1}", item.Key, item.Value);

            Console.WriteLine("Total NumError: " + num_error);  //print error count
            Console.WriteLine("press a key to exit.");
            System.Console.ReadKey();
        }
    }
}
