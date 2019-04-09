using System;
using System.Collections.Generic;
using System.Linq;

public class LL_0_ : CompilerFuncs
{
    List<Production> productions;
    HashSet<string> nullables;
    Dictionary<string, Production> productionDict;
    List<Token> tokens;
    public LL_0_(Dictionary<string, Production> prodDict, List<Production> prods, HashSet<string> nulls, List<Token> t, ref Dictionary<string, Dictionary<string, HashSet<string>>> LLTable, ref TreeNode productionTreeRoot, bool computeTree)
    {
        productions = prods;
        productionDict = prodDict;
        nullables = nulls;
        tokens = t;
        computeLLTable(ref LLTable);
        printLLTable(LLTable);
        if(computeTree)
            computeLLTree(ref LLTable, ref productionTreeRoot);
    }
    private void computeLLTable(ref Dictionary<string, Dictionary<string, HashSet<string>>> LLTable)
    {
        foreach (Production p in productions)
        {
            Dictionary<string, HashSet<string>> entry = new Dictionary<string, HashSet<string>>();

            foreach (string production in p.productions)
            {
                foreach (string s in findFirst(productionDict, nullables, production, p))
                {
                    if (!entry.ContainsKey(s))
                    {
                        entry.Add(s, getProductionAsHash(production));
                    }
                    else
                    {
                        //entry[s].Add(production);
                        throw new Exception("Exception Line:{" + p.line + "}\nEntry: '" + p.lhs + "' already contains '" + s + "' -> '" + string.Join(" ", entry[s]) + "'\n" +
                            "cannot add goes to '" + s + "' -> '" + production + "', grammar not LL(1)!!");
                    }
                }
            }
            LLTable.Add(p.lhs, entry);
        }
    }
    private int computeLLTree(ref Dictionary<string, Dictionary<string, HashSet<string>>> LLTable, ref TreeNode productionTreeRoot)
    {
        LinkedList<TreeNode> stack = new LinkedList<TreeNode>();
        TreeNode stacktop;
        Token t;

        productionTreeRoot = new TreeNode(productions[0].lhs);  //set root to start production
        stack.AddLast(new TreeNode("$"));                       //add end symbol to start of stack
        stack.AddLast(productionTreeRoot);                      //add first start production

        if (tokens.Count == 0)
            throw new Exception("Nil input tokenized! Make sure the input file has content and can associate with the grammar");

        int inputIndex = 0;
        while (inputIndex <= tokens.Count && stack.Count > 0)
        {
            if (inputIndex == tokens.Count)
                t = new Token("$", "$", -1);
            else
                t = tokens[inputIndex];

            stacktop = stack.Last.Value;

            Console.WriteLine("Looking at T: {0}\tStacktop: ({1}, {2})", t, stacktop.Symbol, stacktop.Token != null? stacktop.Token.Lexeme : "null");
            if (productionDict.ContainsKey(stacktop.Symbol)) //top symbol is nonterminal
            {
                //get nonterminal, remove it and push production that starts on IF onto the stack backwards
                try
                {
                    Console.WriteLine("-------------------POP!!");
                    stack.RemoveLast();
                    if (LLTable[stacktop.Symbol][t.Symbol].ElementAt(0) != "lambda")
                    {
                        List<TreeNode> Children = new List<TreeNode>();
                        foreach (string sym in LLTable[stacktop.Symbol][t.Symbol])
                            Children.Add(new TreeNode(sym));
                        stacktop.Children.AddRange(Children);
                        Children.Reverse();
                        foreach (TreeNode child in Children)
                        {
                            Console.WriteLine("Adding: ({0}, {1})", child.Symbol, stacktop.Token != null ? stacktop.Token.Lexeme : "null");
                            stack.AddLast(child);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Stack contents:");
                    while (stack.Count > 0)
                    {
                        var p = stack.Last.Value;
                        stack.RemoveLast();
                        Console.WriteLine('\t' + p.Symbol);
                    }
                    throw new Exception("Syntax error: " + e.Message + " at line: " + t.line + "(working on " + stacktop.Symbol + " got " + t.Symbol + " )");
                }
            }
            else if (stacktop.Symbol == t.Symbol) //same symbol, pop
            {
                stacktop.Token = t;
                stack.RemoveLast();
                inputIndex++;
                Console.WriteLine("REMOVED! Token{0}:{1}\n\n------------------------------------------------",inputIndex, tokens.Count);
            }
            else
                throw new Exception("Error: Lexeme '" + t.Lexeme + "' does not match top symbol '" + stacktop.Symbol + "'!!!");
        }

        if (stack.Count == 0 && inputIndex - 1 == tokens.Count) //good
            return 1;
        else if (stack.Count == 0)
            throw new Exception("Trailing garbage");
        else
            throw new Exception("Early end of file");
    }
}