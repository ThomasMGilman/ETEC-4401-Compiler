using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

class Producer
{
    private Regex middle;
    private int currentLineNum;
    private string[] grammarLines;
    private string[] inputLines;
    public Producer(string[] gLines, ref List<Terminal> terminals, ref List<Production> productions, ref Dictionary<string, Production> productionDict, ref List<Token> tokens, string[] iLines = null, bool rmvLeftRecursion = false)
    {
        middle = new Regex(@"->");
        currentLineNum = 0;
        grammarLines = gLines;
        inputLines = iLines;

        setTerminals(ref terminals);
        setProductions(ref productions, ref productionDict);
        if (rmvLeftRecursion)
            removeLeftRecursion(ref productions, ref productionDict);
        if (iLines != null)
            TokenizeInputFile(ref terminals, ref tokens);
    }
    private void setTerminals(ref List<Terminal> terminals)
    {
        int lineNum = 0;
        string line = grammarLines[lineNum];

        Regex grammarReg;

        while (line.Trim().Length != 0)
        {
            line = line.Trim();
            int index = middle.Match(line).Index;
            var mid = middle.Match(line);
            var rhs = line.Substring(index + mid.Length).Trim();
            var term = line.Substring(0, index).Trim();

            //Console.WriteLine("Reading into Terminal line:{0}, ' {1} '",lineNum, line);
            if (rhs.Length > 0 && term.Length > 0)
            {
                try
                {
                    grammarReg = new Regex(rhs);
                    if (terminals.Any(item => item.terminal == term))
                    {
                        throw new Exception("\nError at line "+lineNum+", Terminal already present in list lhs: "+term);
                    }
                    Terminal T = new Terminal();
                    T.terminal = term;
                    T.nonTerminal = grammarReg;
                    terminals.Add(T);
                }
                catch(Exception e)
                {
                    throw new Exception("\nError at line: "+lineNum+", Invalid Regex: "+rhs+"\nException: "+e.Message);
                }
                line = grammarLines[++lineNum];
            }
            else
                throw new Exception("\nERROR: '"+term+" -> "+rhs+"' has invalid (lhs or rhs)");
        }
        while (line.Length == 0)
            line = grammarLines[++lineNum];

        currentLineNum = lineNum;
        
    }
    private void setProductions(ref List<Production> productions, ref Dictionary<string, Production> productionDict)
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
                var rhs = line.Substring(midIndex + mid.Length).Trim();

                Production newP = new Production(terminal, rhs, currentLineNum);
                //Console.WriteLine("Line: {0} adding {1} -> {2}", currentLineNum, terminal, rhs);
                if (productionDict.ContainsKey(terminal))
                {
                    Console.WriteLine("Production Already exists!!! {0} -> {1}\ntrying to add production at line: {2} {3} -> {4}",
                        terminal, productionDict[terminal].rhs, currentLineNum, terminal, rhs);
                }
                productions.Add(newP);
                if (!productionDict.ContainsKey(terminal))
                    productionDict.Add(terminal, newP);
                else
                    Console.WriteLine("ERROR!!!Line:{0} '{1} -> {2}' already exists in dictionary '{3} -> {4}'",
                        currentLineNum, terminal, rhs, terminal, productionDict[terminal].rhs);
            }
            else
                break;
        }
    }
    private void removeLeftRecursion(ref List<Production> productions, ref Dictionary<string, Production> productionDict)
    {
        bool leftRecursionRemoved = false;
        string newLHS, newProduction;

        while (!leftRecursionRemoved)
        {
            leftRecursionRemoved = true;
            for (int pIndex = 0; pIndex < productions.Count; pIndex++)
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
    private void TokenizeInputFile(ref List<Terminal> terminals, ref List<Token> tokens)
    {
        //Tokenization here
        string input;
        int lineNum = 0, index = 0;
        bool tokenized = false;
        StringBuilder sb = new StringBuilder();

        foreach (string l in inputLines)
            sb.Append(l.Trim() + '\n');
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
                        index += lex.Length;
                        tokenized = true;
                        if (t.terminal != "COMMENT")
                        {
                            Token newT = new Token(t.terminal, lex.ToString(), lineNum);
                            tokens.Add(newT);
                        }
                    }
                }
                if (tokenized == false)
                    throw new Exception("\nERROR!! : failed to Tokenize! line "+lineNum+": '"+input+"' at index: "+index);
            }
        }
    }
}
