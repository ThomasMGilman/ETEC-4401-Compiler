using System;
using System.Windows.Forms;

class MainClass
{
    [STAThread]
    public static void Main(string[] args)
    {
        string gfile;
        string ifile;

        if (args.Length == 0)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "All stuff|*.*";
            dlg.ShowDialog();
            gfile = dlg.FileName;
            if (gfile.Trim().Length == 0)
                return;
            dlg.ShowDialog();
            ifile = dlg.FileName;
            if (ifile.Trim().Length == 0)
                return;
            dlg.Dispose();
        }
        else
        {
            gfile = args[0];
            ifile = args[1];
        }

        Compiler.interpret(gfile, ifile);
        Console.WriteLine("OK!");
        Console.Read();
    }
}