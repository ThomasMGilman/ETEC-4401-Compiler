using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/*
 * These are the structs and classes used by the compiler class.
 * Used by compiler to process grammar and input files passed by caller.
 */

public struct Terminal
{
    public string terminal;
    public Regex nonTerminal;
}

public struct LR_Transition
{
    char sym;
    int index;
}

public class Token
{
    public string Symbol;
    public string Lexeme;
    public int line;
    public Token(string sym, string lexeme, int line)
    {
        this.Symbol = sym;
        this.Lexeme = lexeme;
        this.line = line;
    }
    public override string ToString()
    {
        return string.Format("[{0,10} {1,4} {2,25}]",
            this.Symbol, this.line, this.Lexeme);
    }
}

public struct longestProduction
{
    public Production p;
    public int longestProdNum;
    public string longestProd;
}

public class Production
{
    public string lhs;
    public string rhs;
    public int line;
    public List<string> productions;
    public HashSet<string> Firsts;
    public Dictionary<string, string> FirstDict;
    public HashSet<string> Follow;

    public Production(string lhs, string rhs, int line)
    {
        this.lhs = lhs;
        this.rhs = rhs;
        this.line = line;
        this.productions = new List<string>();
        this.Firsts = new HashSet<string>();
        this.FirstDict = new Dictionary<string, string>();
        this.Follow = new HashSet<string>();
        setProductions();
    }
    public override string ToString()
    {
        return string.Format("[{0,10} {1,4} {2,25}]",
            this.lhs, this.line, this.rhs);
    }
    public void setProductions()
    {
        string[] prods = rhs.Split('|');
        foreach (string production in prods)
        {
            this.productions.Add(production.Trim());
        }
    }
    public void resetRHS()
    {
        rhs = string.Join(" | ", productions);
    }
}

public class LR0Item
{
    public readonly string Lhs;
    public readonly List<string> Rhs;
    public readonly int Dpos;           //index of thing after dist. pos.
    public LR0Item(string lhs, List<string> rhs, int dpos)
    {
        this.Lhs = lhs;
        this.Rhs = rhs;
        this.Dpos = dpos;
    }
    public bool DposAtEnd()
    {
        return Dpos == Rhs.Count;
    }
    public override int GetHashCode()
    {
        int h = 0;

        h ^= Lhs.GetHashCode();
        h ^= Dpos.GetHashCode();
        foreach (string term in Rhs)
            h ^= term.GetHashCode();

        return h;
    }
    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        LR0Item o = obj as LR0Item;
        if (o == null 
            || o.Lhs != this.Lhs 
            || o.Dpos != this.Dpos 
            || o.Rhs.Count != this.Rhs.Count)
            return false;
        
        for(int i = 0; i < this.Rhs.Count; i++)
        {
            if (o.Rhs[i] != this.Rhs[i])
                return false;
        }
        return true;
    }
    public static bool operator ==(LR0Item o1, LR0Item o2)
    {
        return Object.Equals(o1, o2);
    }
    public static bool operator !=(LR0Item o1, LR0Item o2)
    {
        return !(o1 == o2);
    }
    public override string ToString()
    {
        string lr0item = this.Lhs + " -> ";
        if (this.Rhs.Count > 0)
        {
            int rhsCount = this.Rhs.Count;
            for (int i = 0; i <= rhsCount; i++)
            {
                if (i == this.Dpos)
                    lr0item += "*";
                if (i < rhsCount)
                {
                    lr0item += this.Rhs[i];
                    if (i != rhsCount - 1)
                        lr0item += " ";
                }

            }
        }
        else
            lr0item += "*";
        return lr0item;
    }
}

class EQ : IEqualityComparer<HashSet<LR0Item>>
{
    public EQ() { }
    public bool Equals(HashSet<LR0Item> a, HashSet<LR0Item> b)
    {
        return a.SetEquals(b);
    }
    public int GetHashCode(HashSet<LR0Item> x)
    {
        int h = 0;
        foreach (var i in x)
        {
            h ^= i.GetHashCode();
        }
        return h;
    }
}

public class State
{
    public HashSet<LR0Item> Items;
    public Dictionary<string, State> Transitions;
    public readonly int index;
    private static int sCounter = 0;
    public State(int cntStart = -1)
    {
        if (cntStart >= 0)
            sCounter = cntStart;

        index = sCounter++;
        Items = new HashSet<LR0Item>();
        Transitions = new Dictionary<string, State>();
    }
    public void printHashSet()
    {
        foreach (LR0Item i in Items.ToList())
        {
            Console.Write("\t{0} -> ", i.Lhs);
            for (int index = 0; index <= i.Rhs.Count; index++)
            {
                if (index == i.Dpos)
                    Console.Write("*");
                if (index < i.Rhs.Count)
                    Console.Write("{0} ", i.Rhs[index]);
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
    public override string ToString()
    {
        string states = "\"";
        List<LR0Item> iList = this.Items.ToList();
        for (int i = 0; i < iList.Count; i++)
        {
            states += iList[i].ToString();
            if (i != iList.Count - 1)
                states += "\\n";
        }
        states += "\"";
        //Console.WriteLine("Wrote:\n[\n{0}]", states);

        return states;
    }
    public void printItems()
    {
        Console.WriteLine("State{0} items:", this.index);
        foreach (LR0Item i in Items)
            Console.WriteLine("\t{0}",i.ToString());
    }
}

public class TreeNode
{
    public string Symbol;
    public Token Token = null;
    public List<TreeNode> Children;

    public TreeNode(string Symbol, Token t = null)
    {
        this.Symbol = Symbol;
        if (t != null)
            this.Token = t;
        Children = new List<TreeNode>();
    }
}

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
                    wr.Write(n.ToString()+"->" + node2.ToString() + " [label=\"" + sym + "\"];\n");
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