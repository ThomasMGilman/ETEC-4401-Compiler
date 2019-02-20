//Thomas Gilman
//Jim Hudson
//ETEC 4401 Compiler
// 12th February, 2019
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

public struct Terminal
{
    public string terminal;
    public Regex nonTerminal;
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
    public HashSet<string> Follow;

    public Production(string lhs, string rhs, int line)
    {
        this.lhs = lhs;
        this.rhs = rhs;
        this.line = line;
        this.productions = new List<string>();
        this.Firsts = new HashSet<string>();
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
        foreach(string production in prods)
        {
            this.productions.Add(production.Trim());
        }
    }
    public void resetRHS()
    {
        rhs = string.Join(" | ", productions);
    }
}

public class compiler
{
    private string grammarFile;
    static private string[] grammarLines;
    static private Regex middle;
    static private List<Terminal> terminals;
    static private List<Production> productions;
    static private HashSet<string> nullables;
    static private Dictionary<string, Production> productionDict;
    static private Dictionary<string, HashSet<string>> Follows;
    static private Dictionary<string, Dictionary<string, HashSet<string>>> LLTable;
    static private int currentLineNum;

    public compiler(string grammarFile)
    {
        //initiallize all the class globals
        this.grammarFile = grammarFile;
        grammarLines = System.IO.File.ReadAllLines(@grammarFile);
        middle = new Regex(@"->");
        terminals = new List<Terminal>();
        productions = new List<Production>();
        nullables = new HashSet<string>();
        productionDict = new Dictionary<string, Production>();
        Follows = new Dictionary<string, HashSet<string>>();
        LLTable = new Dictionary<string, Dictionary<string, HashSet<string>>>();
        currentLineNum = 0;

        //compiler tasks, must do in order
        setTerminals();
        computeNullables();
        computeAllFirsts();
        computeFollows();
        computeTable();

        //Test Prints
        //printTerminals();
        //printProductions();
        //printFirsts();
        //printFollows();
        //printNullableSet();
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
            if (grammarLines[currentLineNum].Length > 0)
            {
                line = grammarLines[currentLineNum];
                line = line.Trim();
                midIndex = middle.Match(line).Index;
                var mid = middle.Match(line);
                var terminal = line.Substring(0, midIndex).Trim();
                var rhs = line.Substring(midIndex + mid.Length);

                Production newP = new Production(terminal, rhs, currentLineNum);
                //Console.WriteLine("Line: {0} adding {1} -> {2}", currentLineNum, terminal, rhs);
                if(productionDict.ContainsKey(terminal))
                {
                    Console.WriteLine("Production Already exists!!! {0} -> {1}\ntrying to add production at line: {2} {3} -> {4}",
                        terminal, productionDict[terminal].rhs, currentLineNum, terminal, rhs);
                }
                productions.Add(newP);
                if(!productionDict.ContainsKey(terminal))
                    productionDict.Add(terminal, newP);
                else
                    Console.WriteLine("ERROR!!!Line:{0} '{1} -> {2}' already exists in dictionary '{3} -> {4}'",
                        currentLineNum, terminal, rhs, terminal, productionDict[terminal].rhs);
            }
            else
                break;
        }
    }
    private void removeLeftRecursion()
    {
        bool leftRecursionRemoved = false;
        string newLHS, newProduction;

        while (!leftRecursionRemoved)
        {
            leftRecursionRemoved = true;
            for(int pIndex = 0; pIndex < productions.Count; pIndex++)
            {
                for (int productionIndex = 0; productionIndex < productions[pIndex].productions.Count; productionIndex++)
                {
                    string[] terms = productions[pIndex].productions[productionIndex].Trim().Split(' ');
                    if (terms[0] == productions[pIndex].lhs)    //found Left Recursion
                    {
                        for (int index = 0; index < terms.Length - 1; index++)
                            terms[index] = terms[index + 1];    //move all the terms over to the left
                        newLHS = productions[pIndex].lhs + '`';
                        terms[terms.Length - 1] = newLHS;       //append new terminal to end
                        newProduction = string.Join(" ", terms);

                        if (!productionDict.ContainsKey(newLHS)) //add new productions
                        {
                            Production subProduction = new Production(newLHS, newProduction, productions[pIndex].line);
                            subProduction.productions.Add("lambda");
                            subProduction.resetRHS();
                            productions.Add(subProduction);
                            productionDict.Add(newLHS, subProduction);
                        }
                        else  //update already existing productions
                        {
                            productionDict[newLHS].productions.Add(newProduction);
                            productionDict[newLHS].resetRHS();
                        }
                        for (int ppIndex = 0; ppIndex < productions[pIndex].productions.Count; ppIndex++) //update the current termninals productions that are not left recursive
                        {
                            
                            if (ppIndex != productionIndex)
                            {
                                string[] checkTerms = productions[pIndex].productions[ppIndex].Trim().Split(' ');
                                string newProd = productionDict[productions[pIndex].lhs].productions[ppIndex] + " " + newLHS;
                                if (checkTerms[0] != productions[pIndex].lhs && checkTerms[checkTerms.Length - 1] != newLHS)
                                    productionDict[productions[pIndex].lhs].productions[ppIndex] = newProd;
                            } 
                        }
                        productionDict[productions[pIndex].lhs].productions.Remove(productionDict[productions[pIndex].lhs].productions[productionIndex]);
                        productionDict[productions[pIndex].lhs].resetRHS();
                        leftRecursionRemoved = false;
                    }
                }
                productions[pIndex] = productionDict[productions[pIndex].lhs];
            }
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
        return nullables;
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
    public Dictionary<string, HashSet<string>> getFollows()
    {
        Dictionary<string, HashSet<string>> Follows = new Dictionary<string, HashSet<string>>();
        foreach (Production p in productions)
        {
            Follows.Add(p.lhs, p.Follow);
        }
        return Follows;
    }
    public Dictionary<string, Dictionary<string, HashSet<string>>> getTable()
    {
        return LLTable;
    }
    public void printTerminals()
    {
        foreach(Terminal t in terminals)
        {
            Console.WriteLine("{0} -> {1}", t.terminal, t.nonTerminal.ToString());
        }
        Console.WriteLine();
    }
    public void printNullableSet()
    {
        Console.Write("nullable set: {");
        foreach (string s in nullables)
        {
            Console.Write(" {0} ", s);
        }
        Console.WriteLine("}\n");
    }
    public void printProductions()
    {
        foreach(Production p in productions)
            Console.WriteLine("{0} -> {1}", p.lhs, p.rhs);
        Console.WriteLine();
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
                foreach (string production in p.productions)
                {
                    string[] prod = production.Split(' ');
                    if (prod.Length > longProd.longestProdNum)
                    {
                        longProd.longestProd = production;                  //set longest production to longest production
                        longProd.longestProdNum = prod.Length;              //set longest production num to new longest prod num
                    }   
                }
                setFirst = false;
            }
            else
            {
                foreach (string production in p.productions)
                {
                    string[] prod = production.Split(' ');
                    if (prod.Length > longProd.longestProdNum)
                    {
                        longProd.p = p;                             //set longest production to production with new longest production
                        longProd.longestProd = production;          //set longest production to longest production
                        longProd.longestProdNum = prod.Length;      //set longest production num to new longest prod num
                    }
                }
            }
        }
        Console.WriteLine("{0} {1} -> {2}\n", longProd.longestProdNum, longProd.p.lhs, longProd.longestProd);
    }
    public void printFirsts(Production production = null)
    {
        Console.WriteLine("Firsts");
        if(production == null)
        {
            foreach (Production p in productions)
            {
                Console.Write("\t{0} : [ ", p.lhs);
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
        else
        {
            Console.Write("{0} : [ ", production.lhs);
            if (production.Firsts.Count() > 0)
            {
                for (int f = 0; f < production.Firsts.Count(); f++)
                {
                    if (f == production.Firsts.Count() - 1)
                        Console.WriteLine("{0} ]", production.Firsts.ElementAt(f));
                    else
                        Console.Write(" {0}, ", production.Firsts.ElementAt(f));
                }
            }
            else
                Console.WriteLine(" ]");
        }
        Console.WriteLine();
    }
    public void printFollows(Production production = null)
    {
        Console.WriteLine("Follows:");
        if (production == null)
        {
            foreach (Production p in productions)
            {
                Console.Write("\t{0} : [ ", p.lhs);
                if (p.Follow.Count() > 0)
                {
                    for (int f = 0; f < p.Follow.Count(); f++)
                    {
                        if (f == p.Follow.Count() - 1)
                            Console.WriteLine("{0} ]", p.Follow.ElementAt(f));
                        else
                            Console.Write(" {0}, ", p.Follow.ElementAt(f));
                    }
                }
                else
                    Console.WriteLine(" ]");
            }
        }
        else
        {
            Console.Write("{0} : [ ", production.lhs);
            if (production.Follow.Count() > 0)
            {
                for (int f = 0; f < production.Follow.Count(); f++)
                {
                    if (f == production.Follow.Count() - 1)
                        Console.WriteLine("{0} ]", production.Follow.ElementAt(f));
                    else
                        Console.Write(" {0}, ", production.Follow.ElementAt(f));
                }
            }
            else
                Console.WriteLine(" ]");
        }
        Console.WriteLine();
    }
    private void computeNullables()
    {
        bool nonNullabel = false, allNullablesFound = false;

        while (!allNullablesFound)
        {
            allNullablesFound = true;

            foreach (Production p in productions)
            {
                foreach (string production in p.productions)
                {
                    string[] prod = production.Split(' ');
                    nonNullabel = false;
                    foreach (string ss in prod)               //Check through the production, make sure there is no non nullable with a nullable
                    {
                        if (!nullables.Contains(ss) && ss.ToLower() != "lambda")
                        {
                            nonNullabel = true;
                        }
                    }
                    if (!nonNullabel && !nullables.Contains(p.lhs))
                    {
                        nullables.Add(p.lhs);
                        allNullablesFound = false;
                    }
                }
            }
        }
    }
    private static Production getProduction(string lhs)
    {
        if (productionDict.ContainsKey(lhs.Trim()))
            return productionDict[lhs.Trim()];
        else
            return null;
    }
    /// <summary>
    /// adds the firsts of production2 to the firsts list of the first production
    /// </summary>
    private bool addFirsts(Production p1, Production p2)
    {
        bool Different = false;
        foreach(string f in p2.Firsts)
        {
            if (!p1.Firsts.Contains(f))
            {
                Different = true;
                p1.Firsts.Add(f);
                productionDict[p1.lhs].Firsts.Add(f);
            }
        }
        return Different;
    }
    /// <summary>
    /// adds the firsts of the second production to follow list of the first production
    /// </summary>
    private bool addFirstsToFollows(Production p1, Production p2)
    {
        bool Different = false;
        foreach (string f in p2.Firsts)
        {
            if (!p1.Follow.Contains(f))
            {
                Different = true;
                p1.Follow.Add(f);
                productionDict[p1.lhs].Follow.Add(f);
            }
        }
        return Different;
    }
    /// <summary>
    /// adds the follows of the second production to follow list of the first production
    /// </summary>
    private bool addFollows(Production p1, Production p2)
    {
        bool Different = false;
        foreach (string f in p2.Follow)
        {
            if (!p1.Follow.Contains(f))
            {
                Different = true;
                p1.Follow.Add(f);
                productionDict[p1.lhs].Follow.Add(f);
            }
        }
        return Different;
    }
    /// <summary>
    /// add all first terminals in each nonterminals production to their firsts set
    /// </summary>
    private void computeFirsts()
    {
        foreach(Production p in productions)
        {
            foreach(string production in p.productions)
            {
                string[] terms = production.Trim().Split(' ');
                string term = terms[0];
                if (!productionDict.ContainsKey(term) && !term.ToLower().Trim().Equals("lambda"))
                {
                    p.Firsts.Add(term);
                    productionDict[p.lhs].Firsts.Add(term);
                }
            }
        }
    }
    /// <summary>
    /// Goes through each production and adds all the firsts of the nonterminals productions to their firsts set.
    /// Calls ComputeFirsts() first to set each nonterminals firsts that are terminals as long as any nonterminal preceding it is nullable.
    /// Adds the Firsts of each first nonterminal in each production to the productions nonterminal.
    /// </summary>
    private void computeAllFirsts()
    {
        bool noChanges, onNullable, allFirstsFound = false;
        //adds the first nonTerminals of each production to their nonterminals firsts Set
        computeFirsts(); 
        int index = 0;

        while(!allFirstsFound)
        {
            Production p2 = null;
            noChanges = true;
            foreach(Production p in productions)
            {
                foreach (string production in p.productions)
                {
                    index = 0;
                    string[] Terms = production.Trim().Split(' ');
                    string term = Terms[index];
                    onNullable = true;
                    p2 = null;

                    while (onNullable)
                    {
                        if ((p2 = getProduction(term)) != null)
                        {
                            if (addFirsts(p, p2) == true && noChanges == true)   //check each firsts list of nonTerms and union if lists contain differences
                                noChanges = false;
                        }
                        else
                        {
                            if (!term.ToLower().Equals("lambda") && !p.Firsts.Contains(term))
                            {
                                p.Firsts.Add(term);
                                productionDict[p.lhs].Firsts.Add(term);
                                noChanges = false;
                            }
                        }
                        if (!nullables.Contains(term) || term.ToLower().Equals("lambda") || index >= Terms.Length - 1)
                            onNullable = false;
                        else
                            term = Terms[index++];
                    }
                }
            }
            if (noChanges == true)
                allFirstsFound = true;
        }
    }
    private void computeFollows()
    {
        bool allFollowsFound = false, changes, isNullable;
        string[] terms;
        Production curNonTerm, p1, p2;

        while(!allFollowsFound)
        {
            changes = false;
            for(int i = 0; i < productions.Count(); i++)
            {
                curNonTerm = productions[i];
                foreach (string production in curNonTerm.productions)
                {
                    terms = production.Split(' ');
                    for (int ii = 0; ii < terms.Length; ii++)
                    {
                        isNullable = true;
                        if (i == 0)                     //starting nonterminal
                        {
                            if (!curNonTerm.Follow.Contains("$"))
                            {
                                curNonTerm.Follow.Add("$");
                                productionDict[curNonTerm.lhs].Follow.Add("$");
                                changes = true;
                            }
                        }
                        if ((p1 = getProduction(terms[ii])) != null) //get nonterminal production
                        {
                            int checkToEnd = 1;
                            while (isNullable)
                            {
                                int followingTermIndex = ii + checkToEnd;
                                if (followingTermIndex < terms.Length) //next item is not past length of production
                                {
                                    if ((p2 = getProduction(terms[followingTermIndex])) != null) //nonterminal
                                    {
                                        if (!nullables.Contains(p2.lhs)) //non nullable
                                        {
                                            isNullable = false;
                                            if (addFirstsToFollows(p1, p2) == true)
                                                changes = true;
                                        }
                                        else                            //nonterminal is nullable
                                        {
                                            if (addFirstsToFollows(p1, p2) == true)
                                                changes = true;
                                        }
                                    }
                                    else                                                    //terminal
                                    {
                                        if (!p1.Follow.Contains(terms[followingTermIndex]))
                                        {
                                            p1.Follow.Add(terms[followingTermIndex]);
                                            changes = true;
                                        }
                                        isNullable = false;
                                    }
                                }
                                else                //nonterminal is at end of production
                                {
                                    if (addFollows(p1, curNonTerm) == true)
                                        changes = true;
                                    isNullable = false;
                                }
                                checkToEnd++;
                            }
                        }
                    }
                }
            }
            if(changes == false)
                allFollowsFound = true;
        }
        foreach(Production p in productions)
            Follows.Add(p.lhs, p.Follow);
    }
    private void setProductionDict()
    {
        foreach(Production p in productions)
        {
            if (productionDict.ContainsKey(p.lhs))
                productionDict[p.lhs] = p;
            else
                Console.WriteLine("Production '{0} -> {1}' does not exist in Dictionary!!!", p.lhs, p.rhs);
        }
    }
    private HashSet<string> findFirst(string P, Production e)
    {
        int index = 0;
        HashSet<string> S = new HashSet<string>();
        string[] prod = P.Split(' ');
        string term = prod[index];
        bool nullable = true;

        while(nullable)
        {
            nullable = false;
            if (productionDict.ContainsKey(term))               //nonterminal
            {
                S.UnionWith(productionDict[term].Firsts);       //add nonTerminals firsts

                if (nullables.Contains(term))                   //nonTerminal is nullable
                {
                    S.UnionWith(e.Follow);                      //add nonTerminals follows
                    if(index < prod.Length - 1)
                    {
                        term = prod[++index];
                        nullable = true;
                    }
                }
            }
            else if (term.ToLower().Equals("lambda"))           //terminal
                S.UnionWith(e.Follow);
            else
                S.Add(term);
        }
        return S;
    }
    private HashSet<string> getProductionAsHash(string production)
    {
        HashSet<string> Product = new HashSet<string>();
        if (production.Length > 0)
        {
            string[] terms = production.Trim().Split();
            foreach(string term in terms)
                Product.Add(term);
        }
        return Product;
    }
    private void computeTable()
    {
        foreach (Production p in productions)
        {
            Dictionary<string, HashSet<string>> entry = new Dictionary<string, HashSet<string>>();
            foreach (string production in p.productions)
            {
                foreach (string s in findFirst(production, p))
                {
                    if (!entry.ContainsKey(s))
                        entry.Add(s, getProductionAsHash(production));
                    else
                    {
                        //entry[s].Add("| " + production);
                        throw new Exception("Entry: '"+p.lhs+ "' already contains '"+s+ "' -> '"+string.Join(" ", entry[s])+"'\n" +
                            "cannot add goes to '"+s+ "' -> '"+production+"', grammar not LL(1)!!");
                    }
                }
            }
            LLTable.Add(p.lhs, entry);
        }
    }
    private void outPutNewProductionsToFile()
    {
        string outPutFileName = "Production";
        using (StreamWriter sw = File.CreateText(outPutFileName))
        {
            foreach (Production p in productions)
                sw.WriteLine("{0} -> {1}.", p.lhs, p.rhs);
            sw.Close();
        }
        Console.WriteLine("File has been written");   
    }
}

public class Compiler
{
    public Compiler()
    {}
    public static Dictionary<string, HashSet<string>> computeFirsts(string gFile)
    {
        compiler c = new compiler(gFile);
        return c.getFirsts();
    }
    public static Dictionary<string, HashSet<string>> computeFollow(string gFile)
    {
        compiler c = new compiler(gFile);
        return c.getFollows();
    }
    public static Dictionary<string, Dictionary<string, HashSet<string>>> computeLLTable(string gFile)
    {
        compiler c = new compiler(gFile);
        return c.getTable();
    }
}