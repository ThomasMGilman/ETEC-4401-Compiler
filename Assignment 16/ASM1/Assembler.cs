using System;
using System.Collections.Generic;

public class Assembler
{
    static List<string> asmCode;
    static int labelCounter;
    public Assembler(TreeNode root)
    {
        asmCode = new List<string>();
        labelCounter = 0;
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
    //program -> stmts
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
    //braceblock -> LBR stmts RBR
    private void braceblockNodeCode(TreeNode n) 
    {
        stmtsNodeCode(n.Children[1]);
    }
    //stmts -> stmt stmts | lambda
    private void stmtsNodeCode(TreeNode n) 
    {
        if (n.Children.Count == 0)
            return;
        stmtNodeCode(n.Children[0]);
        stmtsNodeCode(n.Children[1]);
    }
    //stmt -> cond | loop | return-stmt SEMI
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
    //return-stmt -> RETURN expr
    private void returnstmtNodeCode(TreeNode n)
    {
        exprNodeCode(n.Children[1]);
        emit("ret");
    }
    //expr -> NUM
    private void exprNodeCode(TreeNode n)
    {
        double d = Convert.ToDouble(n.Children[0].Token.Lexeme);
        string ds = d.ToString("f");
        if (ds.IndexOf(".") == -1)
            ds += ".0"; //nasm requirment
        emit("mov rax, __float64__({0})", ds);
    }
    private void addressNodeCode(TreeNode n)
    {
        emit("mov rax, [{0}]", n.Children[0].Token.Lexeme);
    }
    //cond -> IF LP expr RP braceblock | 
    //IF LP expr RP braceblock ELSE braceblock
    private void condNodeCode(TreeNode n)
    {
        
        exprNodeCode(n.Children[2]);
        emit("cmp rax,0");
        if(n.Children.Count == 5)
        {
            var endifLabel = label();
            emit("je {0}", endifLabel);
            braceblockNodeCode(n.Children[4]);
            emit("{0}:", endifLabel);
        }
        else if(n.Children.Count == 7)
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
    //loop -> WHILE RP expr RP braceblock
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
        exprNodeCode(n.Children[2]);
        emit("cmp rax, 0");
        emit("je {0}", loopEndLabel);       //jmp equals 0
        emit("{0}:", loopStartLabel);       //fall into loopstart label
        braceblockNodeCode(n.Children[4]);  //do while stuff
        exprNodeCode(n.Children[2]);        //store value into rax
        emit("cmp rax, 0");                 //check rax == 0;
        emit("jne {0}", loopStartLabel);    //jump to loopstart if rax != 0
        emit("{0}:", loopEndLabel);

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