﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class CompilerFuncs
{
    public CompilerFuncs()
    { }
    public HashSet<string> findFirst(Dictionary<string, Production> productionDict, HashSet<string> nullables, string P, Production e)
    {
        int index = 0;
        HashSet<string> S = new HashSet<string>();
        string[] prod = P.Split(' ');
        string term = prod[index];
        bool nullable = true;

        while (nullable)
        {
            nullable = false;
            if (productionDict.ContainsKey(term))               //nonterminal
            {
                S.UnionWith(productionDict[term].Firsts);       //add nonTerminals firsts

                if (nullables.Contains(term))                   //nonTerminal is nullable
                {
                    S.UnionWith(e.Follow);                      //add nonTerminals follows
                    if (index < prod.Length - 1)
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
    public HashSet<string> getProductionAsHash(string production)
    {
        HashSet<string> Product = new HashSet<string>();
        if (production.Length > 0)
        {
            string[] terms = production.Trim().Split(' ');
            foreach (string term in terms)
                Product.Add(term);
        }
        return Product;
    }
    public List<string> getProductionAsList(string production)
    {
        List<string> Product = new List<string>();
        if (production.Length > 0)
        {
            string[] terms = production.Trim().Split(' ');
            foreach (string term in terms)
            {
                if (term.ToLower() != "lambda")
                    Product.Add(term);
            }
        }
        return Product;
    }
    public void printTerminals(List<Terminal> terminals)
    {
        foreach (Terminal t in terminals)
            Console.WriteLine("{0} -> {1}", t.terminal, t.nonTerminal.ToString());

        Console.WriteLine();
    }
    public void printTokens(List<Token> tokens)
    {
        Console.WriteLine("Tokens:");
        foreach (Token t in tokens)
            Console.WriteLine("\tline:' {0} ', Sym:' {1} ', Lex:' {2} '", t.line, t.Symbol, t.Lexeme);
    }
    public void printNullableSet(HashSet<string> nullables)
    {
        Console.Write("nullable set: {");
        foreach (string s in nullables)
            Console.Write(" {0} ", s);

        Console.WriteLine("}\n");
    }
    public void printProductions(List<Production> productions)
    {
        foreach (Production p in productions)
            p.printProduction();
        Console.WriteLine();
    }
    public void printNumProductions(List<Production> productions)
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
    public void printFirsts(List<Production> productions, Production production = null)
    {
        Console.WriteLine("Firsts");
        if (production == null)
        {
            foreach (Production p in productions)
                p.printFirsts();
        }
        else
            production.printFirsts();

        Console.WriteLine();
    }
    public void printFollows(List<Production> productions, Production production = null)
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
    public void printLLTable(Dictionary<string, Dictionary<string, HashSet<string>>> LLTable)
    {
        Console.WriteLine("LL(1) Table:");
        foreach (KeyValuePair<string, Dictionary<string, HashSet<string>>> nonterminal in LLTable)
        {
            foreach (KeyValuePair<string, HashSet<string>> terminal in LLTable[nonterminal.Key])
                Console.WriteLine("\t{0} , {1} ::= {2}", nonterminal.Key, terminal.Key, LLTable[nonterminal.Key][terminal.Key].First());
        }
    }
    public void printLRTable(List<Dictionary<string, Tuple<string, int, string>>> LRTable)
    {
        int row = 0;
        foreach (Dictionary<string, Tuple<string, int, string>> keyValuePairs in LRTable)
        {
            Console.WriteLine("Row {0}:", row++);
            foreach (KeyValuePair<string, Tuple<string, int, string>> keyValuePair in keyValuePairs)
            {
                Console.WriteLine("\t{0} : '{1} {2} {3}'", keyValuePair.Key,
                    keyValuePair.Value.Item1, keyValuePair.Value.Item2, keyValuePair.Value.Item3);
            }
        }
    }
    public void printNumTabs(int tabCount)
    {
        for (int i = 0; i < tabCount; i++)
            Console.Write("\t");
    }
    public void printTreeNode(TreeNode n)
    {
        Console.WriteLine("TreeNode:{0} Token:({1})", n.Symbol, n.Token != null ? n.Token.Symbol + "," + n.Token.Lexeme : "null");
    }
    public void printTree(TreeNode root, int tabCount = 0)
    {
        printNumTabs(tabCount); printTreeNode(root);
        printNumTabs(tabCount); Console.WriteLine("Children:");
        foreach(TreeNode c in root.Children)
        {
            if (c.Children.Count > 0)
                printTree(c, tabCount + 1);
            else
            {
                printNumTabs(tabCount + 1); printTreeNode(c);
            }
        }
    }
    public void outPutNewProductionsToFile(List<Production> productions)
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
    public void dumpLL_Tree(TreeNode productionTreeRoot, string grammarFile, string inputFile, int compilerType)
    {
        if (productionTreeRoot != null && inputFile != null && grammarFile != null && compilerType == 0)
            LLdot.dumpIt(productionTreeRoot);
        else
            throw new Exception("Did not specify compilerType to be for LL(0) or pass an input file to parse");
    }
    public void dumpLR_DFA(State startState, string grammarFile, string inputFile, int compilerType)
    {
        if (startState != null && compilerType == 1 && inputFile != null && grammarFile != null)
        {
            LRdot dfaOut = new LRdot(startState, grammarFile);
        }
        else
            throw new Exception("User Error, did not specify the use of an LR Compiler.\nattempted to output a LR_DFA without a LR(0) Start State!!");
    }
}