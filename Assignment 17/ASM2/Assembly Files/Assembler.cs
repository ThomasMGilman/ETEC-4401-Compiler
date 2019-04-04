using System;
using System.Collections.Generic;

/// <summary>
/// Assembler, uses StateTreeNodeRoot from Compiler to generate assembly instructions per input.
/// Generates assembly for the specific grammar specified in GrammarData.cs
/// </summary>

public class Assembler
{
    static List<string> asmCode;
    static int labelCounter;
    int cType;
    enum VarType { NUMBER };

    public Assembler(TreeNode root, int compilerType)
    {
        asmCode = new List<string>();
        labelCounter = 0;
        cType = compilerType;
        programNodeCode(root);
    }
    public List<string> getASM()
    {
        return asmCode;
    }
    private void printChildren(TreeNode n, string nonterm)
    {
        Console.WriteLine("{0} Contents:", nonterm);
        foreach (TreeNode c in n.Children)
            Console.WriteLine("\t{0}", c.Symbol);
    }
    /// <summary>
    /// program -> stmts
    /// </summary>
    /// <param name="n"></param>
    private void programNodeCode(TreeNode n)
    {
        if (n.Symbol != "program")
            throw new Exception("ICE!!!! Not looking at the start of the program.\n" +
                "Symbol Recieved: ' " + n.Symbol + " ' Expected: ' program '");
        emit("default rel");
        emit("section .text");
        emit("global main");
        emit("main:");
        emit("call theRealMain");
        emit("movq xmm0, rax");
        emit("cvtsd2si rax,xmm0");
        emit("ret");
        emit("theRealMain:");
        braceblockNodeCode(n.Children[0]);
        emit("ret");
        emit("section .data");
    }

    /// <summary>
    /// braceblock -> LBR stmts RBR
    /// </summary>
    /// <param name="n"></param>
    private void braceblockNodeCode(TreeNode n)
    {
        stmtsNodeCode(n.Children[1]);
    }

    /// <summary>
    /// stmts -> stmt stmts | lambda
    /// </summary>
    /// <param name="n"></param>
    private void stmtsNodeCode(TreeNode n)
    {
        if (n.Children.Count == 0)
            return;
        stmtNodeCode(n.Children[0]);
        stmtsNodeCode(n.Children[1]);
    } 

    /// <summary>
    /// stmt -> cond | loop | return-stmt SEMI
    /// </summary>
    /// <param name="n"></param>
    private void stmtNodeCode(TreeNode n)
    {
        TreeNode c = n.Children[0];
        switch (c.Symbol)
        {
            case "cond":
                condNodeCode(c);
                break;
            case "loop":
                loopNodeCode(c);
                break;
            case "return-stmt":
                returnstmtNodeCode(c);
                break;
            default:
                throw new Exception("ICE!!!!! Symbol not recognized as a valid start to stmt!!\n" +
                    "Symbol Got: ' " + n.Symbol + " ' Expected: ' cond ', ' loop ', or ' return-stmt '");
        }
    }

    /// <summary>
    /// return-stmt -> RETURN expr
    /// </summary>
    /// <param name="n"></param>
    private void returnstmtNodeCode(TreeNode n)
    {
        VarType type;
        exprNodeCode(n.Children[1], out type);
        if (type != VarType.NUMBER)
            throw new Exception("ICE!!!Expression did not return back as type NUMBER");
        emit("pop rax");
        emit("ret");
    }

    /// <summary>
    /// cond -> IF LP expr RP braceblock | 
    /// IF LP expr RP braceblock ELSE braceblock
    /// </summary>
    /// <param name="n"></param>
    private void condNodeCode(TreeNode n)
    {
        VarType type;
        exprNodeCode(n.Children[2], out type);
        if (type != VarType.NUMBER)
            throw new Exception("ICE!!!Expression did not return back as type NUMBER");
        emit("pop rax");
        emit("cmp rax, 0");
        if (n.Children.Count == 5)
        {
            var endifLabel = label();
            emit("je {0}", endifLabel);
            braceblockNodeCode(n.Children[4]);
            emit("{0}:", endifLabel);
        }
        else if (n.Children.Count == 7)
        {
            var endifLabel = label();
            var elseLabel = label();
            emit("je {0}", elseLabel);
            braceblockNodeCode(n.Children[4]);
            emit("jmp {0}", endifLabel);
            emit("{0}:", elseLabel);
            braceblockNodeCode(n.Children[6]);
            emit("{0}:", endifLabel);
        }
        else
        {
            printChildren(n, "condNodeCode");
            throw new Exception("ICE!!!! Invalid cond!!! Number of cond: " + n.Children.Count);
        }
    }

    /// <summary>
    /// loop -> WHILE RP expr RP braceblock
    /// </summary>
    /// <param name="n"></param>
    private void loopNodeCode(TreeNode n)
    {
        if (n.Children.Count != 5)
        {
            printChildren(n, "loopNodeCode");
            throw new Exception("ICE!!! Expected loop condition, however recieved improper node!!!\n" +
                "Node Count: " + n.Children.Count);
        }
        var loopStartLabel = label();
        var loopEndLabel = label();
        VarType type;
        exprNodeCode(n.Children[2], out type);
        if (type != VarType.NUMBER)
            throw new Exception("ICE!!!Expression did not return back as type NUMBER");
        emit("pop rax");
        emit("cmp rax, 0");
        emit("je {0}", loopEndLabel);               //jmp equals 0
        emit("{0}:", loopStartLabel);               //fall into loopstart label

        braceblockNodeCode(n.Children[4]);          //do while stuff
        exprNodeCode(n.Children[2], out type);      //store value into rax
        if (type != VarType.NUMBER)
            throw new Exception("ICE!!!Expression did not return back as type NUMBER");

        emit("pop rax");
        emit("cmp rax, 0");
        emit("jne {0}", loopStartLabel);            //jump to loopstart if rax != 0
        emit("{0}:", loopEndLabel);

    }

    /// <summary>
    /// expr -> orexp
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void exprNodeCode(TreeNode n, out VarType type)
    {
        orexpNodeCode(n.Children[0], out type);
    }

    /// <summary>
    /// orexp -> orexp OR andexp | andexp
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void orexpNodeCode(TreeNode n, out VarType type)
    {
        if (n.Children.Count == 1)
            andexpNodeCode(n.Children[0], out type);
        else if (n.Children[0].Symbol == "orexp")
        {
            VarType t0;
            orexpNodeCode(n.Children[0], out t0);
            if (t0 != VarType.NUMBER)
                throw new Exception("ICE!!!! Symbol: " + n.Children[0].Symbol + " did not return back as a number!!");

            string lbl = label();
            emit("mov rax, [rsp]");
            emit("cmp rax, 0");
            emit("jne {0}", lbl);
            emit("add rsp, 8");

            VarType t1;
            andexpNodeCode(n.Children[2], out t1);
            if (t0 != VarType.NUMBER)
                throw new Exception("ICE!!!! Symbol: " + n.Children[2].Symbol + " did not return back as a number!!");
            emit("{0}:", lbl);
            type = VarType.NUMBER;
        }
        else
            throw new Exception("ICE!!!! Expected ' orexp ' or ' andexp ', instead Recieved: " + n.Children[0].Symbol);
    }

    /// <summary>
    /// andexp -> andexp AND notexp | notexp
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void andexpNodeCode(TreeNode n, out VarType type)
    {
        if (n.Children.Count == 1)
            notexpNodeCode(n.Children[0], out type);
        else if (n.Children[0].Symbol == "andexp")
        {
            VarType t0;
            andexpNodeCode(n.Children[0], out t0);
            if (t0 != VarType.NUMBER)
                throw new Exception("ICE!!!! Symbol: " + n.Children[0].Symbol + " did not return back as a number!!");

            string lbl = label();
            emit("mov rax, [rsp]");
            emit("cmp rax, 0");
            emit("je {0}", lbl);
            emit("add rsp, 8");

            VarType t1;
            notexpNodeCode(n.Children[2], out t1);
            if (t1 != VarType.NUMBER)
                throw new Exception("ICE!!!! Symbol: " + n.Children[2].Symbol + " did not return back as a number!!");
            emit("{0}:", lbl);
            type = VarType.NUMBER;
        }
        else
            throw new Exception("ICE!!!! Expected ' andexp ' or ' notexp ', instead Recieved: " + n.Children[0].Symbol);
    }

    /// <summary>
    /// notexp -> NOT notexp | rel
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void notexpNodeCode(TreeNode n, out VarType type)
    {
        if (n.Children.Count == 1 && n.Children[0].Symbol == "rel")
            relNodeCode(n.Children[0], out type);
        else if (n.Children[0].Symbol == "NOT")
        {
            notexpNodeCode(n.Children[1], out type);
            if (type != VarType.NUMBER)
                throw new Exception("ICE!!! notexp did not return as type NUMBER!!");
            string makeZero = label();
            string cont = label();
            emit("pop rax");
            emit("cmp rax, 0");
            emit("jne {0}", makeZero);
            makeDouble_and_push("1");
            emit("jmp {0}", cont);
            emit("{0}:", makeZero);
            makeDouble_and_push("0");
            emit("{0}:", cont);

        }
        else
            throw new Exception("ICE!!!! Expected ' rel ' or ' NOT ', instead Recieved: " + n.Children[0].Symbol);

    }

    /// <summary>
    /// rel -> sum RELOP sum | sum
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void relNodeCode(TreeNode n, out VarType type) //done
    {
        if (n.Children.Count == 1)
            sumNodeCode(n.Children[0], out type);
        else if (n.Children[0].Symbol == "sum")
        {
            VarType t0, t1;
            sumNodeCode(n.Children[0], out t0);
            sumNodeCode(n.Children[2], out t1);
            if (t0 != VarType.NUMBER || t1 != VarType.NUMBER)
                throw new Exception("ICE!!!! Symbol: " + n.Children[0].Symbol + " or Symbol: " + n.Children[2].Symbol + " did not return back as a number!!");
            movTwoVarsTo_xmmRegisters();

            string mnemonic;
            switch (n.Children[1].Token.Lexeme)
            {
                case "==": mnemonic = "cmpeqsd"; break;
                case "<": mnemonic = "cmpltsd"; break;
                case "<=": mnemonic = "cmplesd"; break;
                case "!=": mnemonic = "cmpneqsd"; break;
                case ">=": mnemonic = "cmpnltsd"; break;
                case ">": mnemonic = "cmpnlesd"; break;
                default:
                    throw new Exception("ICE!!! Expected " +
                        "' = ', ' < ', ' <= ', ' != ', ' >= ', or ' > ' instead Recieved: " + n.Children[1].Token.Lexeme);
            }
            emit("{0} xmm0,xmm1", mnemonic);

            //Convert NaN to 1.0 with bitwise AND
            emit("movq rax, xmm0");
            emit("mov rbx, __float64__(1.0)");
            emit("and rax, rbx");
            emit("push rax");
            type = VarType.NUMBER;
        }
        else
            throw new Exception("ICE!!!! Expected ' sum ', instead Recieved: " + n.Children[0].Symbol);
        return;
    }

    /// <summary>
    /// sum -> sum ADDOP term | sum MINUS term | term
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    void sumNodeCode(TreeNode n, out VarType type)
    {
        switch (cType)
        {
            case 0:
                VarType type1;
                termNodeCode(n.Children[1], out type1);
                sumLL_NodeCode(n, type1, out type);
                return;
            case 1:
                sumSLR_NodeCode(n, out type);
                return;
            default:
                throw new Exception("COMPILER ERROR!!!! Did not specify either LL'0' or SLR'1' parse style!!!");
        }

    }

    /// <summary>
    /// sum -> sum ADDOP term | sum MINUS term | term
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    void sumSLR_NodeCode(TreeNode n, out VarType type)
    {
        switch (n.Children[0].Symbol)
        {
            case "term":
                termNodeCode(n.Children[0], out type);
                return;
            case "sum":
                VarType t0, t1;
                sumNodeCode(n.Children[0], out t0);
                termNodeCode(n.Children[2], out t1);
                if (t0 != VarType.NUMBER || t1 != VarType.NUMBER)
                    throw new Exception("ICE!!!! Symbol: " + n.Children[0].Symbol + " or Symbol: " + n.Children[2].Symbol + " did not return back as a number!!");
                movTwoVarsTo_xmmRegisters();
                switch (n.Children[1].Token.Lexeme)
                {
                    case "+": emit("addsd xmm0, xmm1"); break;
                    case "-": emit("subsd xmm0, xmm1"); break;
                    default:
                        throw new Exception("ICE!!!! Expected ' - ' or ' + ', Recieved: " + n.Children[1].Token.Lexeme);
                }
                mov_xmm_To_rax_andPush();
                type = VarType.NUMBER;
                return;
            default:
                throw new Exception("ICE!!!! Expected ' sum ' or ' term ', instead Recieved: " + n.Children[0].Symbol);
        }
    }

    /// <summary>
    /// //sum' -> ADDOP term sum' | MINUS term sum' | lambda
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type1"></param>
    /// <param name="type"></param>
    void sumLL_NodeCode(TreeNode n, VarType type1, out VarType type)
    {
        if (n.Children.Count == 0)
        {
            type = type1;
            return;
        }
        VarType type2;
        termNodeCode(n.Children[1], out type2);
        if (type1 != type2)
            throw new Exception("ICE!!!Type1: " + type1.ToString() + " did not match Type2: " + type2.ToString());
        movTwoVarsTo_xmmRegisters();
        switch (n.Children[0].Token.Lexeme)
        {
            case "+":
                emit("addsd xmm0,xmm1");
                break;
            case "-":
                emit("subsd xmm0,xmm1");
                break;
            default:
                throw new Exception("ICE!!!! Expected ' - ' or ' + ', Recieved: " + n.Children[0].Token.Lexeme);
        }
        mov_xmm_To_rax_andPush();
        type = VarType.NUMBER;
        return;
    }

    /// <summary>
    /// term -> term MULOP neg | neg
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    void termNodeCode(TreeNode n, out VarType type)
    {
        switch (n.Children[0].Symbol)
        {
            case "term":
                VarType t0, t1;
                termNodeCode(n.Children[0], out t0);
                negNodeCode(n.Children[2], out t1);
                if (t0 != VarType.NUMBER || t1 != VarType.NUMBER)
                    throw new Exception("ICE!!!! Symbol: " + n.Children[0].Symbol + " or Symbol: " + n.Children[2].Symbol + " did not return back as a number!!");
                movTwoVarsTo_xmmRegisters();
                if (n.Children[1].Token.Symbol == "MULOP")
                    switch(n.Children[1].Token.Lexeme)
                    {
                        case "*": emit("mulsd xmm0, xmm1"); break;
                        case "/": emit("divsd xmm0, xmm1"); break;
                        default:
                            throw new Exception("ICE!!!! Expected MULOP : ' * ', Recieved: " + n.Children[1].Token.Lexeme);
                    }
                else
                    throw new Exception("ICE!!!! Expected MULOP : ' * ', Recieved: " + n.Children[1].Token.Lexeme);
                mov_xmm_To_rax_andPush();
                type = VarType.NUMBER;
                return;
            case "neg":
                negNodeCode(n.Children[0], out type);
                return;
            default:
                throw new Exception("ICE!!! Expected ' term ' or ' neg ', instead Recieved: " + n.Children[0].Symbol);
        }
    }

    /// <summary>
    /// neg -> MINUS neg | factor
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void negNodeCode(TreeNode n, out VarType type)
    {
        if (n.Children.Count == 1 && n.Children[0].Symbol == "factor")
            factorNodeCode(n.Children[0], out type);
        else if (n.Children[0].Symbol == "MINUS")
        {
            negNodeCode(n.Children[1], out type);
            movRaxTo_xmmRegister("xmm0");
            emit("mov rax, 8000000000000000h");
            emit("movq xmm1, rax");
            emit("XORPD xmm0, xmm1"); //xor with sign bit of 64bit double
            mov_xmm_To_rax_andPush();
            
        }
        else
            throw new Exception("ICE!!! Expected ' MINUS ' or ' factor ', instead Recieved: " + n.Children[0].Symbol);
    }

    /// <summary>
    /// factor -> NUM | LP expr RP
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void factorNodeCode(TreeNode n, out VarType type)
    {
        var child = n.Children[0];
        switch (child.Symbol)
        {
            case "NUM":
                makeDouble_and_push(child.Token.Lexeme);
                type = VarType.NUMBER;
                break;
            case "LP":
                exprNodeCode(n.Children[1], out type);
                break;
            default:
                throw new Exception("ICE!!! Expected NUM or LP Recieved:" + child.Symbol);
        }
    }

    private void makeDouble_and_push(string number)
    {
        double d = Convert.ToDouble(number);
        string ds = d.ToString("f");
        if (ds.IndexOf(".") == -1)
            ds += ".0";
        emit("mov rax, __float64__({0})", ds);
        emit("push rax");
    }
    private void movTwoVarsTo_xmmRegisters() //done
    {
        movRaxTo_xmmRegister("xmm1");//second operand
        movRaxTo_xmmRegister("xmm0");//first operand
    }
    private void movRaxTo_xmmRegister(string xmmNum) //done
    {
        emit("pop rax");        //get operand
        emit("movq {0}, rax", xmmNum);
    }
    private void mov_xmm_To_rax_andPush()
    {
        emit("movq rax, xmm0");
        emit("push rax");
    }
    private void throwError(string sym, string[] args)
    {
        string expectations = "";
        for(int i = 0; i < args.Length; i++)
        {
            expectations += "' " + args[i] + " '";
            if (i != args.Length - 1)
                expectations += ", ";
            if (i == args.Length - 2)
                expectations += "and ";
        }
        throw new Exception("ICE!!! Expected "+expectations+" Recieved:" + sym);
    }
    private static string label()
    {
        string s = "lbl" + labelCounter;
        labelCounter++;
        return s;
    }
    private static void emit(string fmt, params object[] p)
    {
        asmCode.Add(string.Format(fmt, p));
    }
}