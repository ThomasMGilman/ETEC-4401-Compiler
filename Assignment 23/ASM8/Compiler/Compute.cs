using System;
using System.Collections.Generic;
using System.Linq;

public class Compute
{
    public Compute(ref Dictionary<string, Production> productionDict, ref List<Production> productions, ref HashSet<string> nullables, ref Dictionary<string, HashSet<string>> Follows)
    {
        computeNullables(ref nullables, ref productions);
        computeAllFirsts(ref productionDict, ref productions, ref nullables);
        computeFollows(ref productionDict, ref productions, ref nullables, ref Follows);
    }
    private void computeNullables(ref HashSet<string> nullables, ref List<Production> productions)
    {
        bool nonNullabel = false, allNullablesFound = false;

        while (!allNullablesFound)
        {
            allNullablesFound = true;

            foreach (Production p in productions)
            {
                foreach (string production in p.productions)
                {
                    string[] prod = production.Split(' ');
                    nonNullabel = false;
                    foreach (string ss in prod)               //Check through the production, make sure there is no non nullable with a nullable
                    {
                        if (!nullables.Contains(ss) && ss.ToLower() != "lambda")
                        {
                            nonNullabel = true;
                        }
                    }
                    if (!nonNullabel && !nullables.Contains(p.lhs))
                    {
                        nullables.Add(p.lhs);
                        allNullablesFound = false;
                    }
                }
            }
        }
    }
    private Production getProduction(ref Dictionary<string, Production> productionDict, string lhs)
    {
        if (productionDict.ContainsKey(lhs.Trim()))
            return productionDict[lhs.Trim()];
        else
            return null;
    }
    /// <summary>
    /// adds the firsts of production2 to the firsts list of the first production
    /// </summary>
    private bool addFirsts(ref Dictionary<string, Production> productionDict, Production p1, Production p2)
    {
        bool Different = false;
        foreach (string f in p2.Firsts)
        {
            if (!p1.Firsts.Contains(f))
            {
                Different = true;
                p1.Firsts.Add(f);
                p1.FirstDict.Add(f, p2.FirstDict[f]);
                productionDict[p1.lhs].Firsts.Add(f);
            }
        }
        return Different;
    }
    /// <summary>
    /// adds the firsts of the second production to follow list of the first production
    /// </summary>
    private bool addFirstsToFollows(ref Dictionary<string, Production> productionDict, Production p1, Production p2)
    {
        bool Different = false;
        foreach (string f in p2.Firsts)
        {
            if (!p1.Follow.Contains(f))
            {
                Different = true;
                p1.Follow.Add(f);
                productionDict[p1.lhs].Follow.Add(f);
            }
        }
        return Different;
    }
    /// <summary>
    /// adds the follows of the second production to follow list of the first production
    /// </summary>
    private bool addFollows(ref Dictionary<string, Production> productionDict, Production p1, Production p2)
    {
        bool Different = false;
        foreach (string f in p2.Follow)
        {
            if (!p1.Follow.Contains(f))
            {
                Different = true;
                p1.Follow.Add(f);
                productionDict[p1.lhs].Follow.Add(f);
            }
        }
        return Different;
    }
    /// <summary>
    /// add all first terminals in each nonterminals production to their firsts set
    /// </summary>
    private void computeFirsts(ref Dictionary<string, Production> productionDict, ref List<Production> productions)
    {
        foreach (Production p in productions)
        {
            foreach (string production in p.productions)
            {
                string[] terms = production.Trim().Split(' ');
                string term = terms[0];
                if (!productionDict.ContainsKey(term) && !term.ToLower().Trim().Equals("lambda"))
                {
                    p.Firsts.Add(term);
                    if (!p.FirstDict.ContainsKey(term))
                        p.FirstDict.Add(term, production);
                    productionDict[p.lhs].Firsts.Add(term);
                }
            }
        }
    }
    /// <summary>
    /// Goes through each production and adds all the firsts of the nonterminals productions to their firsts set.
    /// Calls ComputeFirsts() first to set each nonterminals firsts that are terminals as long as any nonterminal preceding it is nullable.
    /// Adds the Firsts of each first nonterminal in each production to the productions nonterminal.
    /// </summary>
    private void computeAllFirsts(ref Dictionary<string, Production> productionDict, ref List<Production> productions, ref HashSet<string> nullables)
    {
        bool noChanges, onNullable, allFirstsFound = false;
        //adds the first nonTerminals of each production to their nonterminals firsts Set
        computeFirsts(ref productionDict, ref productions);
        int index = 0;

        while (!allFirstsFound)
        {
            Production p2 = null;
            noChanges = true;
            foreach (Production p in productions)
            {
                foreach (string production in p.productions)
                {
                    index = 0;
                    string[] Terms = production.Trim().Split(' ');
                    string term = Terms[index];
                    onNullable = true;
                    p2 = null;

                    while (onNullable)
                    {
                        if ((p2 = getProduction(ref productionDict, term)) != null)
                        {
                            if (addFirsts(ref productionDict, p, p2) == true && noChanges == true)   //check each firsts list of nonTerms and union if lists contain differences
                                noChanges = false;
                        }
                        else
                        {
                            if (!term.ToLower().Equals("lambda") && !p.Firsts.Contains(term))
                            {
                                p.Firsts.Add(term);
                                p.FirstDict.Add(term, production);
                                productionDict[p.lhs].Firsts.Add(term);
                                noChanges = false;
                            }
                        }
                        if (!nullables.Contains(term) || term.ToLower().Equals("lambda") || index >= Terms.Length - 1)
                            onNullable = false;
                        else
                            term = Terms[index++];
                    }
                }
            }
            if (noChanges == true)
                allFirstsFound = true;
        }
    }
    private void computeFollows(ref Dictionary<string, Production> productionDict, ref List<Production> productions, ref HashSet<string> nullables, ref Dictionary<string, HashSet<string>> Follows)
    {
        bool allFollowsFound = false, changes, isNullable;
        string[] terms;
        Production curNonTerm, p1, p2;

        while (!allFollowsFound)
        {
            changes = false;
            for (int i = 0; i < productions.Count(); i++)
            {
                curNonTerm = productions[i];
                foreach (string production in curNonTerm.productions)
                {
                    terms = production.Split(' ');
                    for (int ii = 0; ii < terms.Length; ii++)
                    {
                        isNullable = true;
                        if (i == 0)                     //starting nonterminal
                        {
                            if (!curNonTerm.Follow.Contains("$"))
                            {
                                curNonTerm.Follow.Add("$");
                                productionDict[curNonTerm.lhs].Follow.Add("$");
                                changes = true;
                            }
                        }
                        if ((p1 = getProduction(ref productionDict, terms[ii])) != null) //get nonterminal production
                        {
                            int checkToEnd = 1;
                            while (isNullable)
                            {
                                int followingTermIndex = ii + checkToEnd;
                                if (followingTermIndex < terms.Length) //next item is not past length of production
                                {
                                    if ((p2 = getProduction(ref productionDict, terms[followingTermIndex])) != null) //nonterminal
                                    {
                                        if (!nullables.Contains(p2.lhs)) //non nullable
                                        {
                                            isNullable = false;
                                            if (addFirstsToFollows(ref productionDict, p1, p2) == true)
                                                changes = true;
                                        }
                                        else                            //nonterminal is nullable
                                        {
                                            if (addFirstsToFollows(ref productionDict, p1, p2) == true)
                                                changes = true;
                                        }
                                    }
                                    else                                                    //terminal
                                    {
                                        if (!p1.Follow.Contains(terms[followingTermIndex]))
                                        {
                                            p1.Follow.Add(terms[followingTermIndex]);
                                            changes = true;
                                        }
                                        isNullable = false;
                                    }
                                }
                                else                //nonterminal is at end of production
                                {
                                    if (addFollows(ref productionDict, p1, curNonTerm) == true)
                                        changes = true;
                                    isNullable = false;
                                }
                                checkToEnd++;
                            }
                        }
                    }
                }
            }
            if (changes == false)
                allFollowsFound = true;
        }
        foreach (Production p in productions)
            Follows.Add(p.lhs, p.Follow);
    }
    private void setProductionDict(ref Dictionary<string, Production> productionDict, ref List<Production> productions)
    {
        foreach (Production p in productions)
        {
            if (productionDict.ContainsKey(p.lhs))
                productionDict[p.lhs] = p;
            else
                throw new Exception("Production '"+p.lhs+" -> "+ p.rhs + "' does not exist in Dictionary!!!");
        }
    }
}
