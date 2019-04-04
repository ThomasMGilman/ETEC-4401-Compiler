//Thomas Gilman
//Jim Hudson
//ETEC 4401 Compiler
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

public class compiler : CompilerFuncs
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
        Assembler asmblr = new Assembler(productionTreeRoot, compilerType);
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

    public void dumpLR_DFA()
    {
        dumpLR_DFA(startState, grammarFile, inputFile, compilerType);
    }

    public void fullTestPrint()
    {
        printTerminals(terminals);
        printProductions(productions);
        printNullableSet(nullables);
        printFirsts(productions);
        printFollows(productions);
        printTokens(tokens);
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