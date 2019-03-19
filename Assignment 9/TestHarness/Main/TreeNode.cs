using System;
using System.Collections.Generic;

namespace Testsuite{
public class TreeNode
{
    public List<TreeNode> Children = new List<TreeNode>();
    public string Symbol;
    public Grammar.Token Token;

    public TreeNode(string sym)
    {
        Symbol = sym;
    }
}

}
