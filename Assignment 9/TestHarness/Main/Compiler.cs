using System.Collections.Generic;
using System;

namespace Testsuite{
    
internal static class Compiler{
    internal static TreeNode parse(string gfile, string ifile){
        var g = new Grammar(gfile,false);
        LL1Parser p = new LL1Parser(g);
        var tokens = g.tokenize(ifile);
        return p.parse(tokens);
    }
}

public static class TestsuiteCompiler{
    public static TreeNode parse(string gfile, string ifile){
        var g = new Grammar(gfile,false);
        LL1Parser p = new LL1Parser(g);
        var tokens = g.tokenize(ifile);
        return p.parse(tokens);
    }
}

}
