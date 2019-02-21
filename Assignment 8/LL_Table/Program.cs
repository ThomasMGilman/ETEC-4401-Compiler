using System;
using System.Windows.Forms;
using System.Collections.Generic;

class MainClass
{
    [STAThread]
    public static void Main(string[] args)
    {
        string gfile;
        if (args.Length == 0)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "All stuff|*.*";
            dlg.ShowDialog();
            gfile = dlg.FileName;
            if (gfile.Trim().Length == 0)
                return;
            dlg.Dispose();
        }
        else
        {
            gfile = args[0];
        }

        var lltable = Compiler.computeLLTable(gfile);
        Console.WriteLine("LL(1) Table: ");
        dumpIt(lltable);
    }

    static void dumpIt(dynamic lltable)
    {

        var nonterminals = new List<string>();
        var terminalSet = new HashSet<string>();
        foreach (var nonterminal in lltable.Keys)
        {
            nonterminals.Add(nonterminal);
            foreach (var terminal in lltable[nonterminal].Keys)
            {
                terminalSet.Add(terminal);
            }
        }
        var terminals = new List<string>();
        terminals.AddRange(terminalSet);
        nonterminals.Sort();
        terminals.Sort();
        foreach (var nonterminal in nonterminals)
        {
            foreach (var terminal in terminals)
            {
                if (lltable[nonterminal].ContainsKey(terminal))
                {
                    var production = lltable[nonterminal][terminal];
                    string pstring = "";
                    if (production.Count == 0)
                        pstring = "lambda";
                    else
                    {
                        foreach (var sym in production)
                        {
                            pstring += sym + " ";
                        }
                    }
                    Console.WriteLine(nonterminal + "," + terminal + " ::= " + pstring);
                }
            }
        }
        Console.Read();
    }
}