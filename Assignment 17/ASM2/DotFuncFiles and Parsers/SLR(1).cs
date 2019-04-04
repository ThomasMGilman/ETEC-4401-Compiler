using System;
using System.Collections.Generic;
using System.Linq;

public class SLR_1_ : CompilerFuncs
{
    List<Production> productions;
    HashSet<string> nullables;
    Dictionary<string, Production> productionDict;
    Dictionary<string, HashSet<string>> Follows;
    List<Token> tokens;
    private List<State> states;
    private State startState;
    public SLR_1_(Dictionary<string, Production> prodDict, List<Production> prods, HashSet<string> nulls, Dictionary<string, HashSet<string>> follow, List<Token> t, ref List<Dictionary<string, Tuple<string, int, string>>> LRTable, ref TreeNode productionTreeRoot, bool computeTree)
    {
        productions = prods;
        productionDict = prodDict;
        nullables = nulls;
        Follows = follow;
        tokens = t;

        //create Start State
        states = new List<State>();
        startState = new State(0);
        LR0Item start = new LR0Item("S'", new List<string> { "program" }, 0);
        LRTable = new List<Dictionary<string, Tuple<string, int, string>>>();
        Dictionary<HashSet<LR0Item>, State> seen = new Dictionary<HashSet<LR0Item>, State>(new EQ());
        Stack<State> todo = new Stack<State>();

        startState.Items.Add(start);
        computeClosure(startState.Items);

        states.Add(startState);
        seen.Add(startState.Items, startState);
        todo.Push(startState);

        while (todo.Count > 0)
        {
            State Q = todo.Pop();
            Dictionary<string, HashSet<LR0Item>> transitions = computeTransitions(Q);
            addStates(Q, transitions, seen, todo);
        }
        
        //printSeenMap(seen);
        computeSLRTable(ref LRTable);
        //printLRTable(LRTable);
        //printTokens(t);
        //LRdot dot = new LRdot(startState, grammarFile);
        if (computeTree)
            SLR_Parse(LRTable, ref productionTreeRoot);
    }
    private Dictionary<string, HashSet<LR0Item>> computeTransitions(State State)
    {
        //make copy of transitions dictionary in State
        Dictionary<string, HashSet<LR0Item>> transitions = new Dictionary<string, HashSet<LR0Item>>();

        //add new transitions if not in dict
        foreach (LR0Item item in State.Items)
        {
            if (!item.DposAtEnd())
            {
                string sym = item.Rhs[item.Dpos];
                if (!transitions.ContainsKey(sym))
                    transitions.Add(sym, new HashSet<LR0Item>());
                LR0Item newItem = new LR0Item(item.Lhs, item.Rhs, item.Dpos + 1);
                //Console.WriteLine("adding production rhsLen:" + newItem.Rhs.Count + " " + newItem.ToString());
                transitions[sym].Add(newItem);
            }
        }
        //return copy as new Dict
        return transitions;
    }
    private bool checkSeen(HashSet<LR0Item> hash, Dictionary<HashSet<LR0Item>, State> seenMap)
    {
        EQ checker = new EQ();
        foreach (KeyValuePair<HashSet<LR0Item>, State> pair in seenMap)
        {
            if (checker.Equals(hash, pair.Key))
            {
                return true;
            }
        }
        return false;
    }
    private void printSeenMap(Dictionary<HashSet<LR0Item>, State> seenMap)
    {
        Console.WriteLine("Seen map contains:");
        foreach (KeyValuePair<HashSet<LR0Item>, State> keyPair in seenMap)
        {
            Console.WriteLine("\tHashSet Contains Items: "); keyPair.Value.printHashSet();
        }
        Console.WriteLine("------------------------------------");
    }
    private void computeClosure(HashSet<LR0Item> stateItems)
    {
        int stateIndex = 0;
        List<LR0Item> toConsider = stateItems.ToList();

        while (stateIndex < toConsider.Count)
        {
            LR0Item item = toConsider[stateIndex];
            stateIndex++;
            if (!item.DposAtEnd())
            {
                string sym = item.Rhs[item.Dpos];
                if (productionDict.ContainsKey(sym)) //nonterminal
                {
                    foreach (string p in productionDict[sym].productions)
                    {
                        LR0Item item2 = new LR0Item(sym, getProductionAsList(p), 0);
                        if (!stateItems.Contains(item2))
                        {
                            stateItems.Add(item2);
                            toConsider.Add(item2);
                        }
                    }
                }
            }
        }
    }
    private void addStates(State state, Dictionary<string, HashSet<LR0Item>> transitions, Dictionary<HashSet<LR0Item>, State> seen, Stack<State> todo)
    {
        foreach (KeyValuePair<string, HashSet<LR0Item>> key in transitions)
        {
            computeClosure(key.Value);
            if (!checkSeen(key.Value, seen))
            {
                State newState = new State();
                newState.Items = key.Value;

                states.Add(newState);
                seen.Add(key.Value, newState);
                todo.Push(newState);
            }
            state.Transitions[key.Key] = seen[key.Value];
        }
    }

    private void computeSLRTable(ref List<Dictionary<string, Tuple<string, int, string>>> LRTable)
    {
        foreach (State s in states)
        {
            //s.printItems();
            Dictionary<string, Tuple<string, int, string>> row = new Dictionary<string, Tuple<string, int, string>>();
            Tuple<string, int, string> tuple;
            foreach (KeyValuePair<string, State> t in s.Transitions)
            {
                if (!productionDict.ContainsKey(t.Key)) //symbol is terminal
                    tuple = new Tuple<string, int, string>("S", t.Value.index, "");
                else
                    tuple = new Tuple<string, int, string>("T", t.Value.index, "");

                row.Add(t.Key, tuple);
            }
            foreach (LR0Item item in s.Items)
            {
                if (item.DposAtEnd()) //the item is at the end of rhs
                {
                    if (item.Lhs == "S'")
                    {
                        tuple = new Tuple<string, int, string>("R", item.Rhs.Count, item.Lhs);
                        row.Add("$", tuple);
                    }
                    else
                    {
                        foreach (string follow in Follows[item.Lhs])
                        {
                            tuple = new Tuple<string, int, string>("R", item.Rhs.Count, item.Lhs);
                            row.Add(follow, tuple);
                        }
                    }
                }
            }
            LRTable.Add(row);
        }
    }
    private void SLR_Parse(List<Dictionary<string, Tuple<string, int, string>>> LRTable, ref TreeNode productionTreeRoot)
    {
        Stack<int> stateStack = new Stack<int>();
        Stack<TreeNode> nodeStack = new Stack<TreeNode>();
        int tokenIndex = 0;

        stateStack.Push(0);

        try
        {
            while (true)
            {
                int s = stateStack.Peek();
                string t;
                if (tokenIndex == tokens.Count)
                    t = "$";
                else
                    t = tokens[tokenIndex].Symbol;
                //Console.WriteLine("\nToken{0}: ' {1} ' out of {2} Tokens", tokenIndex, t, tokens.Count);
                if (!LRTable[s].ContainsKey(t))
                    throw new Exception("Syntax Error!! State:\n'" + states[s].ToString() + "'\nNo Entry for Token:'" + t + "'");
                else
                {
                    Tuple<string, int, string> action = LRTable[s][t];
                    //Console.WriteLine("S:{3} T:{4} \tAction: {0}, {1}, {2}", action.Item1, action.Item2, action.Item3, s, t);

                    if (action.Item1 == "S") //Shift
                    {
                        stateStack.Push(action.Item2);
                        nodeStack.Push(new TreeNode(t, tokens[tokenIndex]));
                        //if (tokenIndex < tokens.Count())
                            //Console.WriteLine("\tShift Item2:'{0}' Node:'{1}' TokenLex:'{2}'", action.Item2, t, tokens[tokenIndex].Lexeme);
                        tokenIndex++;
                    }
                    else                    //Reduce
                    {
                        TreeNode n = new TreeNode(action.Item3); //Reduce to Symbol

                        //Console.WriteLine("Popping {0} items:", action.Item2);
                        for (int popNum = 0; popNum < action.Item2; popNum++)
                        {
                            //Console.WriteLine("\tPop: {0}\t: {1}, {2}", stateStack.Peek(), nodeStack.Peek().Symbol, nodeStack.Peek().Token == null ? "null" : nodeStack.Peek().Token.Lexeme);
                            stateStack.Pop();
                            n.Children.Insert(0, nodeStack.Pop());
                        }
                        //Console.WriteLine("Reduced To: {0}, {1}", action.Item3, LRTable[stateStack.Peek()][action.Item3].Item2);
                        if (action.Item3 == "program" || action.Item3 == "S'")
                        {
                            if (tokenIndex == tokens.Count && t == "$")
                            {
                                //Console.WriteLine("ROOT: {0}", n.Symbol);
                                productionTreeRoot = n;
                                return;
                            }
                            else
                                throw new Exception("Compiler Error!!! " +
                                    "Token is either not at the end or symbol is not '$'\n" +
                                    "Token index: '" + tokenIndex + "', Token:'" + t + "'");
                        }
                        stateStack.Push(LRTable[stateStack.Peek()][action.Item3].Item2);
                        nodeStack.Push(n);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Line:{0} ERROR: " + e.Message, e.Source);
            int stackCount = stateStack.Count;
            Console.WriteLine("\tStateStack Contents:");
            while (stackCount-- > 0)
                Console.WriteLine("\t\t" + stateStack.Pop().ToString());
            stackCount = nodeStack.Count;
            Console.WriteLine("\tNodeStack Contents:");
            while (stackCount-- >= 0)
            {
                TreeNode node = nodeStack.Pop();
                Console.Write("\t\t{0}", node.Symbol);
                if (node.Token != null)
                    Console.WriteLine(": {1} {2}", node.Token.Symbol, node.Token.Lexeme);
            }
            throw new Exception(e.Message);
        }
    }
}