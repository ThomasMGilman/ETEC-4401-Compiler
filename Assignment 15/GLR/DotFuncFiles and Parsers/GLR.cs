using System;
using System.Collections.Generic;
using System.Linq;

public class Node
{
    public State data = null;
    public int Value = 0;
    public Node nextNode = null;
    public Tuple<string, Node, int, string> how = null;
    public Node(State state, Tuple<string, Node, int, string> action, Node nxtNode)
    {
        data = state;
        Value = state.index;
        how = action;
        nextNode = nxtNode;
    }
    public void printNode()
    {
        Console.Write("Action: {0} -> ", how);
        data.printItems();
    }
}

public class StackAsList
{
    Node top;
    public StackAsList(Node t = null)
    { top = t; }
    public void push(State data, Tuple<string, Node, int, string> how)
    { top = new Node(data, how, this.top); }
    public void pop(int num = 1)
    { for (int i = 0; i < num; i++) top = top.nextNode; }
    public int getTopValue()                
        => top.Value;
    public Node getTop()                    
        => top;
    public StackAsList clone()           
        => new StackAsList(top);
    public KeyValuePair<Node, Node> key()   
        => new KeyValuePair<Node, Node>(top, top.nextNode);
}


public class GLR : CompilerFuncs
{
    List<Token> tokens;
    Dictionary<string, Production> productionDict;
    Dictionary<string, HashSet<string>> Follows;
    private Dictionary<HashSet<LR0Item>, State> seen;
    private List<State> states;
    public GLR(Dictionary<string, Production> prodDict, string firstProduction, Dictionary<string, HashSet<string>> follow, List<Token> t, ref List<Dictionary<string, List<Tuple<string, int, string>>>> parseTable, ref TreeNode productionTreeRoot, ref State startState, bool computeTree)
    {
        productionDict = prodDict;
        Follows = follow;
        tokens = t;

        //create Start State
        states          = new List<State>();
        startState      = new State(0);
        LR0Item start   = new LR0Item("S'", new List<string> { firstProduction }, 0);
        parseTable      = new List<Dictionary<string, List<Tuple<string, int, string>>>>();
        seen            = new Dictionary<HashSet<LR0Item>, State>(new EQ());
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

        printSeenMap(seen);
        computeGLRTable(ref parseTable);
        printGLRTable(parseTable);
        printTokens(t);
        LRdot dot = new LRdot(startState, "gFile.dot");
        if (computeTree)
            GLR_Parse(parseTable, ref productionTreeRoot);
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

    /// <summary>
    /// Check if dictionary row contains key value toCheck, if not, add it along with new Action List. Otherwise do nothing
    /// </summary>
    /// <param name="row"></param>
    /// <param name="toCheck"></param>
    private void checkIfContains(ref Dictionary<string, List<Tuple<string, int, string>>> row, string toCheck)
    {
        if (!row.ContainsKey(toCheck))
            row.Add(toCheck, new List<Tuple<string, int, string>>());
    }

    private void computeGLRTable(ref List<Dictionary<string, List<Tuple<string, int, string>>>> parseTable)
    {
        foreach (State s in states)
        {
            //s.printItems();
            Dictionary<string, List<Tuple<string, int, string>>> row = new Dictionary<string, List<Tuple<string, int, string>>>();
            Tuple<string, int, string> tuple;
            foreach (KeyValuePair<string, State> t in s.Transitions)
            {
                if (!productionDict.ContainsKey(t.Key)) //symbol is terminal
                    tuple = new Tuple<string, int, string>("S", t.Value.index, "");
                else
                    tuple = new Tuple<string, int, string>("T", t.Value.index, "");

                checkIfContains(ref row, t.Key);
                row[t.Key].Add(tuple);
            }
            foreach (LR0Item item in s.Items)
            {
                if (item.DposAtEnd()) //the item is at the end of rhs
                {
                    if (item.Lhs == "S'")
                    {
                        checkIfContains(ref row, "$");
                        tuple = new Tuple<string, int, string>("R", item.Rhs.Count, item.Lhs);
                        row["$"].Add(tuple);
                    }
                    else
                    {
                        foreach (string follow in Follows[item.Lhs])
                        {
                            checkIfContains(ref row, follow);
                            tuple = new Tuple<string, int, string>("R", item.Rhs.Count, item.Lhs);
                            row[follow].Add(tuple);
                        }
                    }
                }
            }
            parseTable.Add(row);
        }
    }

    private void printStackAsList(List<StackAsList> stack)
    {
        foreach (StackAsList s in stack)
        {
            Node node = s.getTop();
            while (node != null)
            {
                node.printNode();
                node = node.nextNode;
            }
        }
    }

    private void GLR_Parse(List<Dictionary<string, List<Tuple<string, int, string>>>> parseTable, ref TreeNode productionTreeRoot)
    {
        List<StackAsList> stacks        = new List<StackAsList>();
        List<StackAsList> nextStacks    = new List<StackAsList>();
        Dictionary<Node, Node> active   = new Dictionary<Node, Node>();
        StackAsList root = null;

        stacks.Add(new StackAsList(new Node(states[0], null, null))); //add start state
        int tokenIndex = 0;

        Stack<int> stateStack = new Stack<int>();
        Stack<TreeNode> nodeStack = new Stack<TreeNode>();
        stateStack.Push(0);

        bool notDone = true;
        try
        {
            while (notDone)
            {
                string sym;
                if (tokenIndex == tokens.Count)
                    sym = "$";
                else
                    sym = tokens[tokenIndex].Symbol;

                int stackIndex = 0;
                Node refrence;
                if (stacks.Count == 0)
                    throw new Exception("ERROR!! Stack is empty!!");
                Console.WriteLine("Token{0} out of {1}: {2}", tokenIndex, tokens.Count, sym);
                while (stackIndex < stacks.Count) //reduce
                {
                    StackAsList stk = stacks[stackIndex];
                    refrence = stk.getTop();
                    int stateNumber = stk.getTopValue();
                    foreach (Tuple<string, int, string> action in parseTable[stateNumber][sym])
                    {
                        if (action.Item1 == "R")
                        {
                            Console.WriteLine("Action: {0}", action);
                            int numPop = action.Item2;
                            string tSym = action.Item3;

                            StackAsList stk2 = stk.clone();
                            stk2.pop(numPop);                               //reduce number of items
                            stk2.push(states[stk2.getTopValue()], new Tuple<string, Node, int, string>("R", refrence, numPop, sym));
                            if (!active.Contains(stk2.key()))
                            {
                                if (action.Item3 == "S'" || action.Item3 == "program")
                                {
                                    if (tokenIndex == tokens.Count && sym == "$")
                                    {
                                        root = stk2;
                                        notDone = false;
                                        return;
                                    }
                                }
                                else
                                {
                                    active.Add(stk2.getTop(), stk2.getTop().nextNode);
                                    nextStacks.Add(stk2);
                                    stacks.Add(stk2);
                                }
                            }
                        }
                        else if(action.Item1 == "S")
                        {
                            if (!active.Contains(stk.key()))
                            {
                                nextStacks.Add(stk);
                                active.Add(stk.getTop(), stk.getTop().nextNode);
                            }
                        } 
                    }
                    stackIndex++;
                }
                stackIndex = 0;
                var newNextStack = new List<StackAsList>();
                while (stackIndex < nextStacks.Count) //shift
                {
                    StackAsList stk = nextStacks[stackIndex];
                    int stateNumber = stk.getTopValue();
                    foreach (Tuple<string, int, string> action in parseTable[stateNumber][sym])
                    {
                        if (action.Item1 == "S")//shift
                        {
                            refrence = stk.getTop();
                            Console.WriteLine("\tNextAction: {0}", action);
                            StackAsList stk2 = stk.clone();
                            stk2.push(states[action.Item2], new Tuple<string, Node, int, string>("S", refrence, tokenIndex, ""));
                            newNextStack.Add(stk2);
                        }
                    }
                    stackIndex++;
                    stacks = nextStacks;
                }
                stacks = newNextStack;
                nextStacks = new List<StackAsList>();
                tokenIndex++;
            }
        }
        catch(Exception e)
        {
            Console.WriteLine("--------------------------\nStacks:");
            printStackAsList(stacks);
            Console.WriteLine("--------------------------\nNextStacks:");
            printStackAsList(nextStacks);
            throw new Exception(e.Message);
        }
        
        List<Tuple<Tuple<string, Node, int, string>, Node>> actions = new List<Tuple<Tuple<string, Node, int, string>, Node>>();
        Node N = root.getTop();
        while(N.how != null)
        {
            actions.Add(new Tuple<Tuple<string, Node, int, string>, Node>(N.how, N));
            N = N.how.Item2;
        }

        Stack<TreeNode> treeNodeStack = new Stack<TreeNode>();
        for(int i = actions.Count - 1; i >= 0; i--)
        {
            
            Tuple<string, Node, int, string> how = actions[i].Item1;
            if(how.Item1 == "S")//shift
            {
                Token token = tokens[how.Item3];
                TreeNode newNode = new TreeNode(token.Symbol, token);
                treeNodeStack.Push(newNode);
            }
            else if(how.Item1 == "R")
            {
                int numPop = how.Item3;
                string tSym = how.Item4;
                TreeNode node = new TreeNode(tSym);
                while(numPop-- > 0)
                    node.Children.Insert(0, treeNodeStack.Pop());
                treeNodeStack.Push(node);
            }
        }
        productionTreeRoot = treeNodeStack.Peek();
    }
}