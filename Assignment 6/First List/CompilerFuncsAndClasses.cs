using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public struct Terminal
{
    public string terminal;
    public Regex nonTerminal;
}

public struct longestProduction
{
    public Production p;
    public int longestProdNum;
    public string[] longestProd;
}

public class Production
{
    public string lhs;
    public string rhs;
    public int line;
    public List<string[]> productions;
    public HashSet<string> Firsts;
    public Production(string lhs, string rhs, int line)
    {
        this.lhs = lhs;
        this.rhs = rhs;
        this.line = line;
        this.productions = new List<string[]>();
        this.Firsts = new HashSet<string>();
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
        for (int i = 0; i < prods.Length; i++)
        {
            prods[i] = prods[i].Trim();
            this.productions.Add(prods);
        }
    }
}

public class compiler
{
    private string grammarFile;
    static private string[] grammarLines;
    static private Regex middle = new Regex(@"->");
    static private List<Terminal> terminals = new List<Terminal>();
    static private List<Production> productions = new List<Production>();
    static private HashSet<string> nullable;
    static private int currentLineNum = 0;

    public compiler(string grammarFile)
    {
        this.grammarFile = grammarFile;
        grammarLines = System.IO.File.ReadAllLines(@grammarFile);
        setTerminals();
        nullable = computeNullables();
        computeAllFirsts();
    }
    private void setTerminals()
    {
        int lineNum = 0;
        string line = grammarLines[lineNum];
        Regex grammarReg;

        while (line.Length != 0)
        {
            line = line.Trim();
            int index = middle.Match(line).Index;
            var mid = middle.Match(line);
            var rhs = line.Substring(index + mid.Length).Trim();
            var term = line.Substring(0, index).Trim();

            if (rhs.Length > 0 && term.Length > 0)
            {
                try
                {
                    grammarReg = new Regex(rhs);
                    if (terminals.Any(item => item.terminal == term))
                    {
                        Console.Write("\nError at line {0}, Terminal already present in list lhs: {1}", lineNum, term);
                        Console.Read();
                        System.Environment.Exit(-1);
                    }
                    Terminal T = new Terminal();
                    T.terminal = term;
                    T.nonTerminal = grammarReg;
                    terminals.Add(T);
                }
                catch
                {
                    Console.Write("\nError at line: {0}, Invalid Regex: {1}", lineNum, rhs);
                    Console.Read();
                    System.Environment.Exit(-1);
                }
                line = grammarLines[++lineNum];
            }
            else
            {
                Console.WriteLine("\nERROR: '{0} -> {1}' has invalid (lhs or rhs)", term, rhs);
                Console.Read();
                System.Environment.Exit(-1);
            }
        }
        while (line.Length == 0)
            line = grammarLines[++lineNum];

        currentLineNum = lineNum;
        setProductions();
    }
    private void setProductions()
    {
        string line;
        int midIndex;

        for (; currentLineNum < grammarLines.Length; currentLineNum++)
        {
            line = grammarLines[currentLineNum];
            line = line.Trim();
            midIndex = middle.Match(line).Index;
            var mid = middle.Match(line);
            var terminal = line.Substring(0, midIndex).Trim();
            var rhs = line.Substring(midIndex + mid.Length);
            string[] prods = rhs.Split('|');

            Production newP = new Production(terminal, rhs, currentLineNum);
            productions.Add(newP);
        }
    }
    public List<Production> GetProductions()
    {
        return productions;
    }
    public List<Terminal> GetTerminals()
    {
        return terminals;
    }
    public HashSet<string> getNullables()
    {
        return nullable;
    }
    public Dictionary<string, HashSet<string>> getFirsts()
    {
        Dictionary<string, HashSet<string>> firsts = new Dictionary<string, HashSet<string>>();
        foreach(Production p in productions)
        {
            firsts.Add(p.lhs, p.Firsts);
        }
        return firsts;
    }
    public void printTerminals()
    {
        foreach(Terminal t in terminals)
        {
            Console.WriteLine("{0} -> {1}", t.terminal, t.nonTerminal.ToString());
        }
    }
    public void printNullableSet()
    {
        Console.Write("nullable set: {");
        foreach (string s in nullable)
        {
            Console.Write(" {0} ", s);
        }
        Console.Write("}");
    }
    public void printProductions()
    {
        foreach(Production p in productions)
            Console.WriteLine("{0} -> {1}", p.lhs, p.rhs);
    }
    public void printNumProductions()
    {
        longestProduction longProd = new longestProduction();
        bool setFirst = true;
        foreach (Production p in productions)                            //find first longest production
        {
            Console.WriteLine("{0} {1}", p.lhs, p.productions.Count);
            if (setFirst)
            {
                longProd.p = p;
                longProd.longestProd = p.productions[0];
                longProd.longestProdNum = p.productions[0].Length;
                foreach (string[] prod in p.productions)
                {
                    foreach (string s in prod)
                    {
                        string[] production = s.Split(' ');
                        if (production.Length > longProd.longestProdNum)
                        {
                            longProd.longestProd = production;                //set longest production to longest production
                            longProd.longestProdNum = production.Length;      //set longest production num to new longest prod num
                        }
                    }
                            
                }
                setFirst = false;
            }
            else
            {
                foreach (string[] prod in p.productions)
                {
                    foreach(string s in prod)
                    {
                        string[] production = s.Split(' ');
                        if (production.Length > longProd.longestProdNum)
                        {
                            longProd.p = p;                             //set longest production to production with new longest production
                            longProd.longestProd = production;          //set longest production to longest production
                            longProd.longestProdNum = production.Length;//set longest production num to new longest prod num
                        }
                    }
                }
            }
        }
        Console.Write("{0} {1} -> ", longProd.longestProdNum, longProd.p.lhs);
        foreach (string s in longProd.longestProd)
            Console.Write("{0} ", s);
        Console.WriteLine("\n");
    }
    public void printFirsts()
    {
        foreach(Production p in productions)
        {
            Console.Write("{0} : [ ", p.lhs);
            if (p.Firsts.Count() > 0)
            {
                for (int f = 0; f < p.Firsts.Count(); f++)
                {
                    if (f == p.Firsts.Count() - 1)
                        Console.WriteLine("{0} ]", p.Firsts.ElementAt(f));
                    else
                        Console.Write(" {0}, ", p.Firsts.ElementAt(f));
                }
            }
            else
                Console.WriteLine(" ]");
        }
    }
    public void printFirsts(Production p)
    {
        Console.Write("{0} : [ ", p.lhs);
        if (p.Firsts.Count() > 0)
        {
            for (int f = 0; f < p.Firsts.Count(); f++)
            {
                if (f == p.Firsts.Count() - 1)
                    Console.WriteLine("{0} ]", p.Firsts.ElementAt(f));
                else
                    Console.Write(" {0}, ", p.Firsts.ElementAt(f));
            }
        }
        else
            Console.WriteLine(" ]");
    }
    private static HashSet<string> computeNullables()
    {
        HashSet<string> nullables = new HashSet<string>();
        bool nonNullabel = false, allNullablesFound = false;

        while (!allNullablesFound)
        {
            allNullablesFound = true;

            foreach (Production p in productions)
            {
                foreach (string[] prods in p.productions)
                {
                    foreach(string s in prods)
                    {
                        string[] production = s.Split(' ');
                        nonNullabel = false;
                        foreach (string ss in production)               //Check through the production, make sure there is no non nullable with a nullable
                        {
                            if (!nullables.Contains(ss) && ss.ToLower() != "lambda")
                            {
                                nonNullabel = true;
                            }
                        }
                        if(!nonNullabel && !nullables.Contains(p.lhs))
                        {
                            nullables.Add(p.lhs);
                            allNullablesFound = false;
                        }
                    }
                }
            }
        }
        return nullables;
    }
    private bool check_lhs_equals(string comp_lhs)
    {
        foreach(Production p in productions)
        {
            if (p.lhs.Equals(comp_lhs))
                return true;
        }
        return false;
    }
    private Production getProduction(string lhs)
    {
        foreach(Production p in productions)
        {
            if(p.lhs.Trim().ToLower().Equals(lhs.Trim().ToLower()))
            {
                return p;
            }
        }
        return null;
    }
    private bool addFirsts(Production p1, Production p2)
    {
        bool notDifferent = true;
        foreach(string f in p2.Firsts)
        {
            if (!p1.Firsts.Contains(f))
            {
                notDifferent = false;
                p1.Firsts.Add(f);
            }
        }
        return notDifferent;
    }
    private void computeFirsts()    //add all the nonterminals in each production to their firsts list
    {
        foreach(Production p in productions)
        {
            foreach(string[] prod in p.productions)
            {
                foreach(string production in prod)
                {
                    string[] terms = production.Split(' ');
                    string term = terms[0];
                    if (!check_lhs_equals(term) && !term.ToLower().Trim().Equals("lambda"))
                        p.Firsts.Add(term);
                }
            }
        }
    }
    private void computeAllFirsts()
    {
        bool noChanges, onNullable, allFirstsFound = false;
        computeFirsts();
        int index = 0;

        while(!allFirstsFound)
        {
            Production p2 = null;
            noChanges = true;
            foreach(Production p in productions)
            {
                foreach (string[] prod in p.productions)
                {
                    foreach (string production in prod)
                    {
                        index = 0;
                        string[] nonTerms = production.Split(' ');
                        string nonTerm = nonTerms[index];
                        onNullable = true;
                        p2 = null;

                        while (onNullable)
                        {
                            if (check_lhs_equals(nonTerm))
                            {
                                p2 = getProduction(nonTerm);
                                if (p2 != null)
                                {
                                    if (addFirsts(p, p2) == false && noChanges == true)   //check each firsts list of nonTerms and union if lists contain differences
                                        noChanges = false;
                                }
                            }
                            else
                            {
                                if(!nonTerm.Trim().ToLower().Equals("lambda") && !p.Firsts.Contains(nonTerm))
                                {
                                    p.Firsts.Add(nonTerm);
                                    noChanges = false;
                                }
                            }
                            if (!nullable.Contains(nonTerm) || nonTerm.Trim().ToLower().Equals("lambda"))
                                onNullable = false;
                            else
                                nonTerm = nonTerms[index++];
                        }
                    }
                }
            }
            if (noChanges == true)
                allFirstsFound = true;
        }
    }
}

public class Compiler
{
    public Compiler()
    { }
    public static Dictionary<string, HashSet<string>> computeFirsts(string gFile)
    {
        compiler c = new compiler(gFile);
        return c.getFirsts();
    }
    public static HashSet<string> computeNullables(string gFile)
    {
        compiler c = new compiler(gFile);
        return c.getNullables();
    }
}