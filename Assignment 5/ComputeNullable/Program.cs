//Thomas Gilman
//James Hudson
//ETEC 4401 Compiler
// 29th January, 2019
//Assignment 5 Nullable Set
using System;
using System.Windows.Forms;

namespace ComputeNullable
{
    class Program
    {
        [System.STAThread]
        static void Main(string[] args)
        {
            string infile;
            if (args.Length == 0)
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "All stuff|*.*";
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
            Compiler toCompile = new Compiler(infile);
            toCompile.printNullableSet();
            
            Console.Read();
        }
    }
}
