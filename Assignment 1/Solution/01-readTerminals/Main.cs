using System;
using System.Windows.Forms;

class MainClass
{
    [System.STAThread]
    public static void Main(string[] args)
    {
        string infile;
        if(args.Length == 0) {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "All stuff|*.*";
            dlg.ShowDialog();
            infile = dlg.FileName;
            if(infile.Trim().Length == 0)
                return;
            dlg.Dispose();
        } else {
            infile = args[0];
        }
        Grammar g = new Grammar(infile, false);
        Console.WriteLine(g);
    }
}
