using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;

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

        var tree = Compiler.parse(gfile, ifile);
        dumpIt(tree);
        //Compiler.interpret(gfile, ifile);
        //Console.Read();
    }

    class ShadowNode
    {
        public dynamic realNode;
        public int unique;
        public List<ShadowNode> Children = new List<ShadowNode>();
        static int ctr = 0;
        public ShadowNode parent;
        public ShadowNode(dynamic r, ShadowNode parent)
        {
            this.realNode = r;
            this.unique = ctr++;
            this.parent = parent;
        }
        public void walk(Action<ShadowNode> a)
        {
            a(this);
            foreach (var c in Children)
            {
                c.walk(a);
            }
        }
    }
    static void dumpIt(dynamic root)
    {
        ShadowNode sroot = theShadowKnows(root, null);
        using (StreamWriter wr = new StreamWriter("tree.dot"))
        {
            wr.Write("digraph d{\n");
            wr.Write("node [shape=box];\n");
            sroot.walk((n) => {
                wr.Write("n" + n.unique + " [label=\"");
                string sym = n.realNode.Symbol;
                sym = sym.Replace("\"", "\\\"");
                wr.Write(sym);
                if (n.realNode.Token != null)
                {
                    var tok = n.realNode.Token;
                    var lex = tok.Lexeme;
                    lex = lex.Replace("\\", "\\\\");
                    lex = lex.Replace("\n", "\\n");
                    lex = lex.Replace("\"", "\\\"");
                    wr.Write("\\n");
                    wr.Write(lex);
                }
                wr.Write("\"");
                if (n.realNode.Token != null)
                {
                    wr.Write(",style=filled,fillcolor=\"#c0c0c0\"");
                }
                wr.Write("];\n");
            });

            sroot.walk((n) => {
                if (n.parent != null)
                    wr.Write("n" + n.parent.unique + "->n" + n.unique + ";\n");
            });

            wr.Write("}\n");
        }
    }

    static ShadowNode theShadowKnows(dynamic realnode, ShadowNode parent)
    {
        ShadowNode shadow = new ShadowNode(realnode, parent);
        foreach (var c in realnode.Children)
        {
            shadow.Children.Add(theShadowKnows(c, shadow));
        }
        return shadow;
    }
}