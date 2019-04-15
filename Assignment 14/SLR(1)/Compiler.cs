//Thomas Gilman
//Jim Hudson
//ETEC 4401 Compiler
// 26th March, 2019
using System;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

public class compiler
{
    private string grammarFile, inputFile;
    static private string[] grammarLines;
    static private Regex middle;
    static private List<Terminal> terminals;
    static private List<Token> tokens;
    static private List<Production> productions;
    static private List<State> states;
    static private TreeNode productionTreeRoot = null;
    static private State startState = null;
    static private HashSet<string> nullables;
    static private Dictionary<string, Production> productionDict;
    static private Dictionary<string, HashSet<string>> Follows;
    static private Dictionary<string, Dictionary<string, HashSet<string>>> LLTable;
    static private List<Dictionary<string, Tuple<string, int, string>>> LRTable;
    static private int currentLineNum;

    public compiler(string grammarFile, string inputFile = null, int cType = 0)
    {
        //compiler tasks, must do in order
        this.grammarFile = grammarFile;
        this.inputFile = inputFile;

        if (this.grammarFile != null)
        {
            init(grammarFile, inputFile);
            if (this.inputFile != null)
                TokenizeInputFile();

            switch (cType)
            {
                case (0):               //LL_Grammar
                    LL_Grammar_Proc();
                    break;
                case (1):               //LR_Grammar
                    //create Start State
                    LR_Grammar_Proc();
                    break;
            }
        }
        else
            throw new Exception("ERROR!!!! Did not pass a Grammar File!!");
    }

    private void init(string grammarFile, string inputFile)
    {
        //initiallize all the class globals
        grammarLines = File.ReadAllLines(@grammarFile);
        middle = new Regex(@"->");
        terminals = new List<Terminal>();
        tokens = new List<Token>();
        productions = new List<Production>();
        nullables = new HashSet<string>();
        productionDict = new Dictionary<string, Production>();
        Follows = new Dictionary<string, HashSet<string>>();
        LLTable = new Dictionary<string, Dictionary<string, HashSet<string>>>();
        LRTable = new List<Dictionary<string, Tuple<string, int, string>>>();
        productionTreeRoot = null;
        currentLineNum = 0;

        setTerminals();
        computeNullables();
        computeAllFirsts();
        computeFollows();
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
    private void TokenizeInputFile()
    {
        //Tokenization here
        string[] inputLines = File.ReadAllLines(this.inputFile);
        string input;
        int lineNum = 0, index = 0;
        bool tokenized = false;
        StringBuilder sb = new StringBuilder();

        foreach (string l in inputLines)
            sb.Append(l.Trim());
        input = sb.ToString();

        while (index < input.Length)
        {
            tokenized = false;
            if (input[index] == '\n')
            {
                lineNum++;
                index++;
            }
            else if (input[index] == ' ')
                index++;
            else
            {
                foreach (Terminal t in terminals)
                {
                    if (tokenized)
                        break;
                    var lex = t.nonTerminal.Match(input, index);
                    if (lex.Success && lex.Index == index)
                    {
                        Token newT = new Token(t.terminal, lex.ToString(), lineNum);
                        index += lex.Length;
                        tokenized = true;
                        tokens.Add(newT);
                    }
                }
                if (tokenized == false)
                {
                    Console.WriteLine("\nERROR!! : failed to Tokenize! line {0}: '{1}' at index: {2}", lineNum, input, index);
                    Console.Read();
                    System.Environment.Exit(-1);
                }
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
    public TreeNode getTree()
    {
        if (productionTreeRoot == null)
            computeLLTree();
        return productionTreeRoot;
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
    public State getLR0_DFA()
    {
        return startState;
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
    public void printTable()
    {
        Console.WriteLine("LL(1) Table:");
        foreach(KeyValuePair<string, Dictionary<string, HashSet<string>>> nonterminal in LLTable)
        {
            foreach(KeyValuePair<string, HashSet<string>> terminal in LLTable[nonterminal.Key])
                Console.WriteLine("\t{0} , {1} ::= {2}", nonterminal.Key, terminal.Key, LLTable[nonterminal.Key][terminal.Key].First());
        }
    }
    public void dumpLL_Tree()
    {
        if (productionTreeRoot != null && inputFile != null && grammarFile != null)
            LLdot.dumpIt(productionTreeRoot);
    }
    public void printLRTable()
    {
        int row = 0;
        foreach(Dictionary<string, Tuple<string, int, string>> keyValuePairs in LRTable)
        {
            Console.WriteLine("Row {0}:", row++);
            foreach(KeyValuePair<string, Tuple<string, int, string>> keyValuePair in keyValuePairs)
            {
                Console.WriteLine("\t{0} : '{1} {2} {3}'", keyValuePair.Key,
                    keyValuePair.Value.Item1, keyValuePair.Value.Item2, keyValuePair.Value.Item3);
            }
        }
    }
    public void dumpLR_DFA()
    {
        if (startState != null)
        {
            LRdot dfaOut = new LRdot(startState, this.grammarFile);
        }
        else
            throw new Exception("User Error, did not specify the use of an LR Compiler.\nattempted to output a LR_DFA without a LR(0) Start State!!");
    }

    //LL(0) Stuff
    private void LL_Grammar_Proc()
    {
        computeLLTable();

        if (this.inputFile != null)
            computeLLTree();

        //Test Prints
        /*
        printTerminals();
        printProductions();
        printFirsts();
        printFollows();
        printNullableSet();
        printTable();
        */
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
                p1.FirstDict.Add(f, p2.FirstDict[f]);
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
                    if (!p.FirstDict.ContainsKey(term))
                        p.FirstDict.Add(term, production);
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
                                p.FirstDict.Add(term, production);
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
            string[] terms = production.Trim().Split(' ');
            foreach(string term in terms)
                Product.Add(term);
        }
        return Product;
    }
    private List<string> getProductionAsList(string production)
    {
        List<string> Product = new List<string>();
        if (production.Length > 0)
        {
            string[] terms = production.Trim().Split(' ');
            foreach (string term in terms)
            {
                if(term.ToLower() != "lambda")
                    Product.Add(term);
            }
        }
        return Product;
    }
    private void computeLLTable()
    {
        foreach (Production p in productions)
        {
            Dictionary<string, HashSet<string>> entry = new Dictionary<string, HashSet<string>>();
            
            foreach (string production in p.productions)
            {
                foreach (string s in findFirst(production, p))
                {
                    if (!entry.ContainsKey(s))
                    {
                        entry.Add(s, getProductionAsHash(production));
                    }
                    else
                    {
                        //entry[s].Add(production);
                        throw new Exception("Exception Line:{"+p.line+"}\nEntry: '"+p.lhs+ "' already contains '"+s+ "' -> '"+string.Join(" ", entry[s])+"'\n" +
                            "cannot add goes to '"+s+ "' -> '"+production+"', grammar not LL(1)!!");
                    }
                }
            }
            LLTable.Add(p.lhs, entry);
        }
    }
    private int computeLLTree()
    {
        LinkedList<TreeNode> stack = new LinkedList<TreeNode>();
        TreeNode stacktop;
        Token t;
        
        productionTreeRoot = new TreeNode(productions[0].lhs);  //set root to start production
        stack.AddLast(new TreeNode("$"));                       //add end symbol to start of stack
        stack.AddLast(productionTreeRoot);                      //add first start production

        if (this.inputFile == null)
            throw new Exception("Compiler needs both a Grammar File and an Input File!");
        if (tokens.Count == 0)
            throw new Exception("Nil input tokenized! Make sure the input file has content and can associate with the grammar");

        int inputIndex = 0;
        while (inputIndex <= tokens.Count && stack.Count > 0)
        {
            if(inputIndex == tokens.Count)
                t = new Token("$", "$", -1);
            else
                t = tokens[inputIndex];

            stacktop = stack.Last.Value;

            if (productionDict.ContainsKey(stacktop.Symbol)) //top symbol is nonterminal
            {
                //get nonterminal, remove it and push production that starts on IF onto the stack backwards
                try
                {
                    stack.RemoveLast();
                    if(LLTable[stacktop.Symbol][t.Symbol].ElementAt(0) != "lambda")
                    {
                        List<TreeNode> Children = new List<TreeNode>();
                        foreach (string sym in LLTable[stacktop.Symbol][t.Symbol])
                            Children.Add(new TreeNode(sym));
                        stacktop.Children.AddRange(Children);
                        Children.Reverse();
                        foreach (TreeNode child in Children)
                            stack.AddLast(child);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Stack contents:");
                    while(stack.Count > 0)
                    {
                        var p = stack.Last.Value;
                        stack.RemoveLast();
                        Console.WriteLine('\t'+ p.Symbol);
                    }
                    throw new Exception("Syntax error: " + e.Message + " at line: "+ t.line + "(working on " + stacktop.Symbol + " got " + t.Symbol +" )");
                }
            }
            else if (stacktop.Symbol == t.Symbol) //same symbol, pop
            {
                stacktop.Token = t;
                stack.RemoveLast();
                inputIndex++;
            }
            else
                throw new Exception("Error: Lexeme '" + t.Lexeme + "' does not match top symbol '" + stacktop.Symbol + "'!!!");
        }

        if (stack.Count == 0 && inputIndex - 1 == tokens.Count) //good
            return 1;
        else if(stack.Count == 0)
            throw new Exception("Trailing garbage");
        else
            throw new Exception("Early end of file");
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

    //LR(0) stuff
    private void LR_Grammar_Proc()
    {
        //create Start State
        states = new List<State>();
        startState = new State();
        LR0Item start = new LR0Item("S'", new List<string> { "S" }, 0);
        Dictionary<HashSet<LR0Item>, State> seen = new Dictionary<HashSet<LR0Item>, State>(new EQ());
        Stack<State> todo = new Stack<State>();
        

        startState.Items.Add(start);
        computeClosure(startState.Items);

        states.Add(startState);
        seen.Add(startState.Items, startState);
        todo.Push(startState);

        while (todo.Count > 0)
        {
            State Q = todo.Pop();
            Dictionary<string, HashSet<LR0Item>> transitions = computeTransitions(Q);
            addStates(Q, transitions, seen, todo);
        }

        computeSLRTable();

        printSeenMap(seen);
        printLRTable();
        LRdot dot = new LRdot(startState, grammarFile);
        if (this.inputFile != null)
            SLR_Parse();
    }
    private Dictionary<string, HashSet<LR0Item>> computeTransitions(State State)
    {
        //make copy of transitions dictionary in State
        Dictionary<string, HashSet<LR0Item>> transitions = new Dictionary<string, HashSet<LR0Item>>();

        //add new transitions if not in dict
        foreach (LR0Item item in State.Items)
        {
            if(!item.DposAtEnd())
            {
                string sym = item.Rhs[item.Dpos];
                if (!transitions.ContainsKey(sym))
                    transitions.Add(sym, new HashSet<LR0Item>());

                transitions[sym].Add(new LR0Item(item.Lhs, item.Rhs, item.Dpos + 1));
            }
        }
        //return copy as new Dict
        return transitions;
    }
    private bool checkSeen(HashSet<LR0Item> hash, Dictionary<HashSet<LR0Item>, State> seenMap)
    {
        EQ checker = new EQ();
        foreach(KeyValuePair<HashSet<LR0Item>, State> pair in seenMap)
        {
            if (checker.Equals(hash, pair.Key))
            {
                return true;
            }
        }
        return false;
    }
    private void printSeenMap(Dictionary<HashSet<LR0Item>, State> seenMap)
    {
        Console.WriteLine("Seen map contains:");
        foreach(KeyValuePair<HashSet<LR0Item>, State> keyPair in seenMap)
        {
            Console.WriteLine("\tHashSet Contains Items: "); keyPair.Value.printHashSet();
        }
        Console.WriteLine("------------------------------------");
    }
    private void computeClosure(HashSet<LR0Item> stateItems)
    {
        int stateIndex = 0;
        List<LR0Item> toConsider = stateItems.ToList();

        while (stateIndex < toConsider.Count)
        {
            LR0Item item = toConsider[stateIndex];
            stateIndex++;
            if (!item.DposAtEnd())
            {
                string sym = item.Rhs[item.Dpos];
                if (productionDict.ContainsKey(sym)) //nonterminal
                {
                    foreach (string p in productionDict[sym].productions)
                    {
                        LR0Item item2 = new LR0Item(sym, getProductionAsList(p), 0);
                        if (!stateItems.Contains(item2))
                        {
                            stateItems.Add(item2);
                            toConsider.Add(item2);
                        }
                    }
                }
            }
        }
    }
    private void addStates(State state, Dictionary<string, HashSet<LR0Item>>  transitions, Dictionary<HashSet<LR0Item>, State> seen, Stack<State> todo)
    {
        foreach (KeyValuePair<string, HashSet<LR0Item>> key in transitions)
        {
            computeClosure(key.Value);
            if (!checkSeen(key.Value, seen))
            {
                State newState = new State();
                newState.Items = key.Value;

                states.Add(newState);
                seen.Add(key.Value, newState);
                todo.Push(newState);
            }
            state.Transitions[key.Key] = seen[key.Value];
        }
    }

    private void computeSLRTable()
    {
        foreach(State s in states)
        {
            s.printItems();
            Dictionary<string, Tuple<string, int, string>> row = new Dictionary<string, Tuple<string, int, string>>();
            Tuple<string, int, string> tuple;
            foreach (KeyValuePair<string, State> t in s.Transitions)
            {
                if (!productionDict.ContainsKey(t.Key)) //symbol is terminal
                    tuple = new Tuple<string, int, string>("S", s.Transitions[t.Key].index, "");
                else
                    tuple = new Tuple<string, int, string>("T", s.Transitions[t.Key].index, "");

                row.Add(t.Key, tuple);
            }
            foreach(LR0Item item in s.Items)
            {
                if(item.DposAtEnd()) //the item is at the end of rhs
                {
                    if(item.Lhs == "S'")
                    {
                        tuple = new Tuple<string, int, string>("R", item.Rhs.Count, item.Lhs);
                        row.Add("$", tuple);
                    }
                    else
                    {
                        foreach (string follow in Follows[item.Lhs])
                        {
                            tuple = new Tuple<string, int, string>("R", item.Rhs.Count, item.Lhs);
                            row.Add(follow, tuple);
                        }
                    }
                }
            }
            LRTable.Add(row);
        }
    }
    private void SLR_Parse()
    {
        Stack<int> stateStack = new Stack<int>();
        Stack<TreeNode> nodeStack = new Stack<TreeNode>();
        int tokenIndex = 0;

        stateStack.Push(0);

        try
        {
            while (true)
            {
                int s = stateStack.Peek();
                string t;
                if (tokenIndex == tokens.Count)
                    t = "$";
                else
                    t = tokens[tokenIndex].Symbol;

                if (!LRTable[s].ContainsKey(t))
                    throw new Exception("Syntax Error!! State:\n'" + states[s].ToString() + "'\nNo Entry for Token:'" + t + "'");
                else
                {
                    Tuple<string, int, string> action = LRTable[s][t];
                    Console.WriteLine("Action: {0}, {1}, {2}", action.Item1, action.Item2, action.Item3);

                    if (action.Item1 == "S") //Shift
                    {
                        stateStack.Push(action.Item2);
                        nodeStack.Push(new TreeNode(t, tokens[tokenIndex]));
                        if (tokenIndex < tokens.Count())
                            Console.WriteLine("\tShift Item2:'{0}' Node:'{1}' TokenLex:'{2}'", action.Item2, t, tokens[tokenIndex].Lexeme);
                        tokenIndex++;
                    }
                    else                    //Reduce
                    {
                        TreeNode n = new TreeNode(action.Item3); //Reduce to Symbol

                        Console.WriteLine("Popping {0} items:", action.Item2);
                        for(int popNum = 0; popNum < action.Item2; popNum++)
                        {
                            Console.WriteLine("\tPop: {0}\t: {1}, {2}", stateStack.Peek(), nodeStack.Peek().Symbol, nodeStack.Peek().Token == null ? "null" : nodeStack.Peek().Token.Lexeme);
                            stateStack.Pop();
                            n.Children.Insert(0, nodeStack.Pop());
                        }
                        Console.WriteLine("Reduced To: {0}", action.Item3);
                        if (action.Item3 == "S'")
                        {
                            if (tokenIndex == tokens.Count && t == "$")
                            {
                                productionTreeRoot = n;
                                return;
                            }
                            else
                                throw new Exception("Compiler Error!!! " +
                                    "Token is either not at the end or symbol is not '$'\n" +
                                    "Token index: '" + tokenIndex + "', Token:'" + t + "'");
                        }

                        stateStack.Push(LRTable[stateStack.Peek()][action.Item3].Item2);
                        nodeStack.Push(n);
                    }
                }
            }
        }
        catch(Exception e)
        {
            Console.WriteLine("ERROR: " + e.Message);
            int stackCount = stateStack.Count;
            Console.WriteLine("\tStateStack Contents:");
            while(stackCount-- > 0)
                Console.WriteLine("\t\t"+stateStack.Pop().ToString());
            stackCount = nodeStack.Count;
            Console.WriteLine("\tNodeStack Contents:");
            while (stackCount-- > 0)
            {
                TreeNode node = nodeStack.Pop();
                Console.Write("\t\t{0} : {1}", node.Symbol, node.Token == null ? "null" : node.Token.Lexeme);
            }
            throw new Exception(e.Message);
        }
    }
}

public class Compiler
{
    static compiler c;
    public Compiler()
    {}
    public static Dictionary<string, HashSet<string>> computeFirsts(string gFile)
    {
        c = new compiler(gFile);
        return c.getFirsts();
    }
    public static Dictionary<string, HashSet<string>> computeFollow(string gFile)
    {
        c = new compiler(gFile);
        return c.getFollows();
    }
    public static Dictionary<string, Dictionary<string, HashSet<string>>> computeLLTable(string gFile)
    {
        c = new compiler(gFile);
        return c.getTable();
    }
    public static TreeNode parse(string gFile, string iFile)
    {
        c = new compiler(gFile, iFile);
        return c.getTree();
    }
    public static TreeNode compile(string gFile, string iFile)
    {
        c = new compiler(gFile, iFile, 1);
        return c.getTree();
    }
    public static void makelr0dfa(string gFile)
    {
        c = new compiler(gFile, null, 1);
        c.dumpLR_DFA();
    }
}