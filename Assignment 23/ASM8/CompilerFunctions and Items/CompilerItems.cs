using System;
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
    public void printProduction()
    {
        Console.WriteLine("{0} -> {1}", lhs, rhs);
    }
    public void printFirsts()
    {
        Console.Write("\t{0} : [ ", lhs);
        if (Firsts.Count() > 0)
        {
            for (int f = 0; f < Firsts.Count(); f++)
            {
                if (f == Firsts.Count() - 1)
                    Console.WriteLine("{0} ]", Firsts.ElementAt(f));
                else
                    Console.Write(" {0}, ", Firsts.ElementAt(f));
            }
        }
        else
            Console.WriteLine(" ]");
    }
    public void printFollows()
    {
        Console.Write("\t{0} : [ ", lhs);
        if (Follow.Count() > 0)
        {
            for (int f = 0; f < Follow.Count(); f++)
            {
                if (f == Follow.Count() - 1)
                    Console.WriteLine("{0} ]", Follow.ElementAt(f));
                else
                    Console.Write(" {0}, ", Follow.ElementAt(f));
            }
        }
        else
            Console.WriteLine(" ]");
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
    public override string ToString()
    {
        return string.Format("[{0,10} {1,4} {2,25}]",
            this.lhs, this.line, this.rhs);
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

