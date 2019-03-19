using System;
using System.Collections.Generic;
using System.Threading;

namespace Testsuite{
internal class LL1Parser
{
    public readonly Dictionary<string, Dictionary<string, Production>> table = 
        new Dictionary<string,Dictionary<string, Production>>();
    Grammar g;

    public LL1Parser(Grammar g)
    {
        this.g = g;
        computeLLTable();
    }

    public TreeNode parse(List<Grammar.Token> tokens){
        bool verbose = true;
        LinkedList<TreeNode> stack = new LinkedList<TreeNode>();
        TreeNode root = new TreeNode(g.startSymbol);
        stack.AddLast(root);
        int ti = 0;
        while(ti <= tokens.Count && stack.Count > 0) {
            string terminal;
            Grammar.Token token;
            int line;
            if(ti == tokens.Count) {
                token = new Grammar.Token("$","$",-1);
                terminal = "$";
                line = -1;
            } else {
                token = tokens[ti];
                terminal = token.sym;
                line = token.line;
            }

            string stacktop = stack.Last.Value.Symbol;

            if(g.terminalSet.Contains(stacktop)) {
                if(terminal == stacktop) {
                    if(verbose)
                        Console.WriteLine("Match and pop " + terminal);
                    stack.Last.Value.Token = token;
                    stack.RemoveLast();
                    ti++;
                } else {
                    throw new Exception("Syntax error at line " + line + " (expected " + stacktop + ")");
                }
            } else {
                if(table[stacktop].ContainsKey(terminal)) {
                    if( verbose )
                        Console.WriteLine("Expand " + stacktop);
                    TreeNode N = stack.Last.Value;
                    stack.RemoveLast();
                    var C = new List<TreeNode>();
                    foreach(var sym in table[stacktop][terminal]) {
                        C.Add(new TreeNode(sym));
                    }
                    N.Children.AddRange(C);
                    C.Reverse();
                    foreach(var c in C)
                        stack.AddLast(c);
                } else {
                    if(verbose)
                        dumpLLTable();
                    Console.WriteLine("Stack contents:");
                    while(stack.Count > 0 ){
                        var p = stack.Last.Value;
                        stack.RemoveLast();
                        Console.WriteLine(p.Symbol);
                    }
                    throw new Exception("Syntax error at line " + line + " (working on " + stacktop + " and got " + token+")");
                }
            }
        }

        if(ti == tokens.Count && stack.Count == 0)
            return root;
        else if(stack.Count == 0) {
            throw new Exception("Trailing garbage");
        } else {
            throw new Exception("Early end of file");
        }
    }

    private void computeLLTable(){
        foreach(string N in g.nonterminals.Keys ){
            table[N] = new Dictionary<string,Production>();
            foreach(var P in g.nonterminals[N]) {
                foreach( var s in g.findFirst(P,g.follow[N]) ){
                    if( table[N].ContainsKey(s) )
                        throw new Exception("Not LL(1)");
                    table[N][s] = P;
                }
            }
        }
    }
    
    public void dumpLLTable(){
        Console.WriteLine("--------------TABLE---------");
        foreach(string N in table.Keys){
            Console.WriteLine(N);
            foreach(string T in table[N].Keys){
                Console.WriteLine("\t"+T+": "+N+" -> "+table[N][T]);
            }
        }
        Console.WriteLine("--------------END TABLE---------");
    }
}

}
