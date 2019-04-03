//Thomas Gilman
//Jim Hudson
//ETEC 4401 Compiler
using System;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

public class compiler
{
    private string grammarFile, inputFile;
    private string[] grammarLines, inputLines;
    private Regex middle;
    private List<Terminal> terminals;
    private List<Token> tokens;
    private List<Production> productions;
    private TreeNode productionTreeRoot;
    private State startState;
    private HashSet<string> nullables;
    private Dictionary<string, Production> productionDict;
    private Dictionary<string, HashSet<string>> Follows;
    private Dictionary<string, Dictionary<string, HashSet<string>>> LLTable;
    private List<Dictionary<string, Tuple<string, int, string>>> LRTable;
    private int compilerType;

    public compiler(string gFile = null, string inFile = null, int cType = 0)
    {
        //compiler tasks, must do in order
        grammarFile = gFile;
        inputFile = inFile;
        compilerType = cType;
        init(grammarFile, inFile);
        switch (cType)
        {
            case (0):               //LL_Grammar
                LL_0_ produceLL_0 = new LL_0_(productionDict, productions, nullables, tokens, ref LLTable, ref productionTreeRoot, inputFile != null);
                break;
            case (1):               //LR_Grammar
                SLR_1_ produceSLR_1 = new SLR_1_(productionDict, productions, nullables, Follows, tokens, ref LRTable, ref productionTreeRoot, inputFile != null);
                break;
        }
    }

    private void init(string grammarFile, string inputFile)
    {
        //initiallize all the class globals

        grammarLines = grammarFile == null ? GrammarData.data.Split('\n') : File.ReadAllLines(@grammarFile);
        middle = new Regex(@"->");
        terminals = new List<Terminal>();
        tokens = new List<Token>();
        productions = new List<Production>();
        nullables = new HashSet<string>();
        productionDict = new Dictionary<string, Production>();
        Follows = new Dictionary<string, HashSet<string>>();
        LLTable = new Dictionary<string, Dictionary<string, HashSet<string>>>();
        productionTreeRoot = null;
        startState = null;

        if (this.inputFile != null)
            inputLines = File.ReadAllLines(inputFile);

        //Set productions, productionDict, Terminals, and Tokens if inputFile
        Producer producer = new Producer(grammarLines, ref terminals, ref productions, ref productionDict, ref tokens, (inputFile != null ? inputLines : null));
        //Set Production Dictionary, Production List, nullables, Firsts, Follows
        Compute computeProducts = new Compute(ref productionDict, ref productions, ref nullables, ref Follows);

        //fullTestPrint();
    }

    public List<Production> GetProductions()
    {
        return productions;
    }
    public List<Terminal> GetTerminals()
    {
        return terminals;
    }
    public List<string> generateAssembly()
    {
        Assembler asmblr = new Assembler(productionTreeRoot.Children[0]);
        return asmblr.getASM();
    }
    public TreeNode getTree()
    {
        if (productionTreeRoot == null)
        {
            if (inputFile == null)
                throw new Exception("Did not pass a input file to the compiler!!! Can not retrieve TreeRoot as there can not be one!!");
            LL_0_ produceLL_0 = new LL_0_(productionDict, productions, nullables, tokens, ref LLTable, ref productionTreeRoot, inputFile != null);
        }
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

    public void fullTestPrint()
    {
        printTerminals();
        printProductions();
        printNullableSet();
        printFirsts();
        printFollows();
        printTokens();
    }
    public void printTerminals()
    {
        foreach(Terminal t in terminals)
            Console.WriteLine("{0} -> {1}", t.terminal, t.nonTerminal.ToString());

        Console.WriteLine();
    }
    public void printTokens()
    {
        Console.WriteLine("Tokens:");
        foreach(Token t in tokens)
            Console.WriteLine("\tline:' {0} ', Sym:' {1} ', Lex:' {2} '",t.line, t.Symbol, t.Lexeme);
    }
    public void printNullableSet()
    {
        Console.Write("nullable set: {");
        foreach (string s in nullables)
            Console.Write(" {0} ", s);

        Console.WriteLine("}\n");
    }
    public void printProductions()
    {
        foreach (Production p in productions)
            p.printProduction();
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
                p.printFirsts();
        }
        else
            production.printFirsts();
            
        Console.WriteLine();
    }
    public void printFollows(Production production = null)
    {
        Console.WriteLine("Follows:");
        if (production == null)
        {
            foreach (Production p in productions)
                p.printFollows();
        }
        else
            production.printFollows();

        Console.WriteLine();
    }
    public void printLLTable()
    {
        Console.WriteLine("LL(1) Table:");
        foreach(KeyValuePair<string, Dictionary<string, HashSet<string>>> nonterminal in LLTable)
        {
            foreach(KeyValuePair<string, HashSet<string>> terminal in LLTable[nonterminal.Key])
                Console.WriteLine("\t{0} , {1} ::= {2}", nonterminal.Key, terminal.Key, LLTable[nonterminal.Key][terminal.Key].First());
        }
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
    public void dumpLL_Tree()
    {
        if (productionTreeRoot != null && inputFile != null && grammarFile != null && compilerType == 0)
            LLdot.dumpIt(productionTreeRoot);
        else
            throw new Exception("Did not specify compilerType to be for LL(0) or pass an input file to parse");
    }
    public void dumpLR_DFA()
    {
        if (startState != null && compilerType == 1 && inputFile != null && grammarFile != null)
        {
            LRdot dfaOut = new LRdot(startState, this.grammarFile);
        }
        else
            throw new Exception("User Error, did not specify the use of an LR Compiler.\nattempted to output a LR_DFA without a LR(0) Start State!!");
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
    public static void compile(string srcfile, string asmfile, string objfile, string exefile)
    {
        c = new compiler(null, srcfile, 1);
        List<string> asm = c.generateAssembly();
        string asmText = "";

        for (int i = 0; i < asm.Count; i++) //convert to string array
        {
            asmText += asm[i];
            if (i != asm.Count - 1)
                asmText += "\n";
        }
        File.WriteAllText(asmfile, asmText);
        ExeTools.ExeTools.Assemble(asmfile, objfile);
        ExeTools.ExeTools.Link(objfile, exefile);
    }
    public static void makelr0dfa(string gFile)
    {
        c = new compiler(gFile, null, 1);
        c.dumpLR_DFA();
    }
}