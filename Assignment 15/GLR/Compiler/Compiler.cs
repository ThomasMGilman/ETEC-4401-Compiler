﻿//Thomas Gilman
//Jim Hudson
//ETEC 4401 Compiler
using System;
using System.Collections.Generic;
using System.IO;

public class compiler : CompilerFuncs
{
    private string grammarFile, inputFile;
    private string[] grammarLines, inputLines;
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
    private List<Dictionary<string, List<Tuple<string, int, string>>>> GLRTable;
    private int compilerType;

    public compiler(string gFile = null, string inFile = null, int cType = 0)
    {
        //compiler tasks, must do in order
        grammarFile = gFile;
        inputFile = inFile;
        compilerType = cType;
        init(grammarFile, inFile);
        produceTreeRoot();
    }

    private void init(string grammarFile, string inputFile)
    {
        //initiallize all the class globals

        grammarLines = grammarFile == null ? GrammarData.data.Split('\n') : File.ReadAllLines(@grammarFile);
        terminals = new List<Terminal>();
        tokens = new List<Token>();
        productions = new List<Production>();
        nullables = new HashSet<string>();
        productionDict = new Dictionary<string, Production>();
        Follows = new Dictionary<string, HashSet<string>>();
        symtable = new Dictionary<string, dynamic>();
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
        //fullTestPrint();
        //printLRTable(LRTable);
        LLdot.dumpIt(productionTreeRoot);
        Assembler asmblr = new Assembler(productionTreeRoot, compilerType);
        return asmblr.getASM();
    }
    public TreeNode getTree()
    {
        if (productionTreeRoot == null)
        {
            if (inputFile == null)
                throw new Exception("Did not pass a input file to the compiler!!! Can not retrieve TreeRoot as there can not be one!!");
            produceTreeRoot();
        }
        return productionTreeRoot;
    }
    public void produceTreeRoot()
    {
        switch (compilerType)
        {
            case (0):               //LL_Grammar
                LL_0_ produceLL_0 = new LL_0_(productionDict, productions, nullables, tokens, ref LLTable, ref productionTreeRoot, inputFile != null);
                break;
            case (1):               //LR_Grammar
                SLR_1_ produceSLR_1 = new SLR_1_(productionDict, productions[0].lhs, Follows, tokens, ref LRTable, ref productionTreeRoot, ref startState, inputFile != null);
                break;
            case (2):
                GLR produceGLR = new GLR(productionDict, productions[0].lhs, Follows, tokens, ref GLRTable, ref productionTreeRoot, ref startState, inputFile != null);
                break;
            default:
                throw new Exception("Invalid Grammar Parse type!! Please Specify cType = {0,1,2} = {LL(0), SLR(1), GLR}!! Got cType: " + compilerType);
        }
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
    public Dictionary<string, Dictionary<string, HashSet<string>>> getLLTable()
    {
        return LLTable;
    }
    public State getStartState()
    {
        return startState;
    }
    public void Interpret()
    {
        Interpret(productionTreeRoot);
    }
    public void dumpLR_DFA()
    {
        dumpLR_DFA(startState, grammarFile == null ? "default.txt" : grammarFile, inputFile, compilerType);
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
        return c.getLLTable();
    }
    /// <summary>
    /// pass a grammarFile, inputFile, and specify a compilerType if you dont want it to be LL(0) by default.
    /// compilerTypes : {(0, LL(0)), (1, SLR(1)), (2, GLR)}
    /// </summary>
    /// <param name="gFile"></param>
    /// <param name="iFile"></param>
    /// <param name="compilerType"></param>
    /// <returns></returns>
    public static TreeNode parseTree(string gFile, string iFile, int compilerType = 0)
    {
        c = new compiler(gFile, iFile, compilerType);
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
    public static void interpret(string gFile, string iFile)
    {
        c = new compiler(gFile, iFile);
        c.Interpret();
    }
    public static void makelr0dfa(string gFile)
    {
        c = new compiler(gFile, null, 1);
        c.dumpLR_DFA();
    }
}