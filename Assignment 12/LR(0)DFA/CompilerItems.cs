using System;
using System.IO;
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
        return base.GetHashCode();
    }
    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;
        LR0Item o = obj as LR0Item;
        if (o == null)
            return false;

        return base.Equals(obj);
    }
    public static bool operator ==(LR0Item o1, LR0Item o2)
    {
        return Object.Equals(o1, o2);
    }
    public static bool operator !=(LR0Item o1, LR0Item o2)
    {
        return !(o1 == o2);
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
    public State()
    {
        Items = new HashSet<LR0Item>();
        Transitions = new Dictionary<string, State>();
    }
}

public class TreeNode
{
    public string Symbol;
    public Token Token = null;
    public List<TreeNode> Children;

    public TreeNode(string Symbol)
    {
        this.Symbol = Symbol;
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