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
}