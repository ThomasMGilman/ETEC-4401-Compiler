using System;
using System.IO;
using System.Collections.Generic;

/*
 * class functions for shadowNode, dumpIt, and shadowknows written by James Hudson.
 * called by compiler class inorder to create a .dot file for viewing in graphviz or other similar program.
 */
class LLdot
{
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

    public static void dumpIt(dynamic root)
    {
        ShadowNode sroot = theShadowKnows(root, null);
        using (StreamWriter wr = new StreamWriter("tree.dot"))
        {
            wr.Write("digraph d{\n");
            wr.Write("node [shape=box];\n");
            sroot.walk((n) =>
            {
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

            sroot.walk((n) =>
            {
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

/*
 * class functions for walk, shuffle, dumpText, and dumpDot written by James Hudson.
 * called by compiler class inorder to create a .dot file for viewing in graphviz or other similar program.
 * dumpdot was modified, to call State.ToString() inorder to create the .dot node names
 */
class LRdot
{
    public LRdot(State startState, string gfile)
    {
        Dictionary<dynamic, int> nmap = new Dictionary<dynamic, int>();
        walk(startState, (n) => {
            int c = nmap.Count;
            nmap[n] = c;
        });

        var f = System.IO.Path.GetFileName(gfile);

        dumpText(f.Replace(".txt", "-dfa.txt"), startState, nmap);
        dumpDot(f.Replace(".txt", "-dfa.d"), startState, nmap);
    }
    static void dumpText<T>(string fname, T startState, Dictionary<dynamic, int> nmap)
    {
        using (StreamWriter wr = new StreamWriter(fname))
        {
            walk(startState, (T n) => {
                dynamic d = n;
                wr.WriteLine("State " + nmap[d]);
                wr.WriteLine(d.Items.Count + " items");


                List<dynamic> tmp = new List<dynamic>();
                foreach (var item in d.Items)
                {
                    tmp.Add(item);
                }

                shuffle(tmp);

                foreach (var item in tmp)
                {
                    wr.WriteLine("\t" + item.Lhs + " " + item.Dpos + " " + String.Join(" ", item.Rhs));
                }
                wr.WriteLine(d.Transitions.Count + " transitions");
                foreach (var keyvalue in d.Transitions)
                {
                    string sym = keyvalue.Key;
                    dynamic node2 = keyvalue.Value;
                    wr.WriteLine("\t" + sym + " " + nmap[node2]);
                }
            });
        }
        Console.WriteLine("Wrote DFA to " + fname);
    }
    static void dumpDot<T>(string fname, T startState, Dictionary<dynamic, int> nmap)
    {
        using (StreamWriter wr = new StreamWriter(fname))
        {
            wr.Write("digraph d{\n");
            wr.Write("node [shape=box];\n");
            walk(startState, (T n) => {
                wr.Write(n.ToString() + " [label=<");
                wr.Write(nmap[n] + "<br/>");
                dynamic nd = n;
                foreach (dynamic item in nd.Items)
                {
                    string lhs = item.Lhs;
                    var rhs = item.Rhs;
                    int dpos = item.Dpos;
                    wr.Write(lhs);
                    wr.Write("&rarr;");
                    for (int i = 0; i < dpos; i++)
                        wr.Write(rhs[i] + " ");
                    wr.Write("&bull; ");
                    for (int i = dpos; i < rhs.Count; ++i)
                        wr.Write(rhs[i] + " ");
                    wr.Write("<br/>");
                }
                wr.Write(">];");
            });

            walk(startState, (T n) => {
                dynamic nd = n;
                foreach (var keyvalue in nd.Transitions)
                {
                    string sym = keyvalue.Key;
                    dynamic node2 = keyvalue.Value;
                    wr.Write(n.ToString() + "->" + node2.ToString() + " [label=\"" + sym + "\"];\n");
                }
            });

            wr.Write("}\n");
        }

        Console.WriteLine("Wrote DFA to " + fname);
    }
    static Random R = new Random();
    static void shuffle<T>(List<T> tmp)
    {
        for (int i = 0; i < tmp.Count; ++i)
        {
            int j = R.Next(tmp.Count);
            var x = tmp[i];
            tmp[i] = tmp[j];
            tmp[j] = x;
        }
    }
    static void walk<T>(T node, Action<T> callback, HashSet<T> visited = null)
    {
        if (visited == null)
            walk(node, callback, new HashSet<T>());
        else
        {
            if (!visited.Contains(node))
            {
                visited.Add(node);
                callback(node);
                dynamic noded = node;
                List<dynamic> tmp = new List<dynamic>();
                foreach (var keyvalue in noded.Transitions)
                {
                    tmp.Add(keyvalue);
                }
                shuffle(tmp);
                foreach (var keyvalue in tmp)
                {
                    //string sym = keyvalue.Key;
                    dynamic node2 = keyvalue.Value;
                    walk(node2, callback, visited);
                }
            }
        }
    }
}