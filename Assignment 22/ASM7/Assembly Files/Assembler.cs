using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Assembler, uses StateTreeNodeRoot from Compiler to generate assembly instructions per input.
/// Generates assembly for the specific grammar specified in GrammarData.cs
/// </summary>
public class Assembler
{
    static Dictionary<string, string> stringPool;
    static List<string> asmCode;
    static SymbolTable symtable;
    static int labelCounter;
    int cType;
    static string[] externs = { "fopen", "fclose", "fscanf", "fprintf", "printf", "scanf", "fflush", "memcpy", "abort"};

    public Assembler(TreeNode root, int compilerType)
    {
        asmCode = new List<string>();
        symtable = new SymbolTable();
        stringPool = new Dictionary<string, string>();
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

    private void outputMagicConstants()
    {
        //fopen
        emit("fopenRplus:");
        emit("db \"r+\", 0");
        emit("fopenA:");
        emit("db \"a\",0");
        //Write
        emit("pcts:");
        emit("db \"%s\", 0");
        emit("pctg:");
        emit("db \"%g\", 0");
        //Input
        emit("scanbuffer:");
        emit("db 0");
        emit("pctlf:");
        emit("db \"%lf\",0");
        //printf
        emit("msg:");
        emit("db 0");
        emit("fmt:");
        emit("db \"%s\", 10, 0");
        emit("arrayMismatch:");
        emit("db \"Array's Sizes dont match!!\", 0");
    }

    private void prologueCode()
    {
        emit("push rbp");
        emit("mov rbp, rsp");
    }

    private void epilogueCode(int sizeOfVariableInThisBlock = 0)
    {
        if (sizeOfVariableInThisBlock > 0)
            emit("add rsp, {0}", sizeOfVariableInThisBlock);
        emit("mov rsp, rbp");
        emit("pop rbp");
        emit("ret");
    }

    private string argRegister(int num)
    {
        string[] win = new string[] { "rcx", "rdx", "r8", "r9"};
        string[] lin = new string[] { "rdi", "rsi", "rdx", "rcx"};
        switch(ExeTools.ExeTools.OperatingSystem)
        {
            case ExeTools.ExeTools.OS.WIN:
                return win[num];
            case ExeTools.ExeTools.OS.LINUX:
            case ExeTools.ExeTools.OS.MAC:
                return lin[num];
            default:
                throw new Exception("ERROR!! Using OS other than Windows, Linux, or Mac!! Or calling register outside of range 4 numGiven:"+num);
        }
    }

    private void doFuncCall(string foreignFunction)
    {
        emit("mov rbx, rsp");   //save val of rsp
        emit("and rsp, -16");   //force rsp to a 16-byte aligned location

        if (ExeTools.ExeTools.OperatingSystem == ExeTools.ExeTools.OS.WIN)
        {
            emit("sub rsp, 32");
            emit("call {0}", foreignFunction);
            emit("add rsp, 32");
        }
        emit("mov rsp, rbx");
    }

    private void outputExterns()
    {
        foreach (string ext in externs)
            emit("extern {0}", ext.Trim('"'));
    }

    /// <summary>
    /// program -> var-decl-list braceblock
    /// </summary>
    /// <param name="n"></param>
    private void programNodeCode(TreeNode n)
    {
        if (n.Symbol != "program")
            throw new Exception("ICE!!!! Not looking at the start of the program.\n" +
                "Symbol Recieved: ' " + n.Symbol + " ' Expected: ' program '");
        outputExterns();

        emit("default rel");
        emit("section .text");
        emit("global main");
        int sizeOfVars;
        vardecllistNodeCode(n.Children[0], 0, out sizeOfVars);
        funcdecllistNodeCode(n.Children[1], true);
        funcdecllistNodeCode(n.Children[1], false);
        emit("main:");
        if (symtable["main"] == null || symtable["main"].VType.typeString != "$function")
            throw new Exception("ERROR!! no Main Function!!!");
        emit("call {0}", symtable["main"].Label);
        emit("movq xmm0, rax");
        emit("cvtsd2si rax,xmm0");
        emit("ret");
        emit("section .data");
        outputMagicConstants();
        outputStringPoolInfo();
        emit("section .bss");   //swap to bss for symbol table
        outputSymbolTableInfo();
    }

    /// <summary>
    /// func-decl-list -> func-decl func-decl-list | lambda
    /// </summary>
    /// <param name="n"></param>
    /// <param name="justPutFunctionNameInSymtable"></param>
    private void funcdecllistNodeCode(TreeNode n, bool justPutFunctionNameInSymtable)
    {
        if (n.Children.Count == 2)
        {
            funcdeclNodeCode(n.Children[0], justPutFunctionNameInSymtable);
            funcdecllistNodeCode(n.Children[1], justPutFunctionNameInSymtable);
        }
        else if (n.Children.Count != 0)
            throw new Exception("ERROR!!! Invalid number of children for func-decl-list");
    }

    /// <summary>
    /// func-decl -> DEF ID LP type-list RP optional-return-spec brace-block
    /// </summary>
    /// <param name="n"></param>
    private void funcdeclNodeCode(TreeNode n, bool justPutFunctionNameInSymtable)
    {
        var fname = n.Children[1].Token.Lexeme;
        VarType retType;
        optionalreturnspecNodeCode(n.Children[5], out retType);

        List<VarType> argTypes;
        List<string> argNames;
        optionaltypelistNodeCode(n.Children[3], out argNames, out argTypes);

        VarType funcType = new FuncVarType(argTypes, retType);
        if(justPutFunctionNameInSymtable)
        {
            if(symtable.ContainsInCurrentScope(fname))
                throw new Exception("Error!! duplicate function decleration in scope!! DupFuncName: " + fname);

            symtable[fname] = new VarInfo(funcType, label(), symtable.ScopeCount == 1 ? true : false);
        }
        else
        {
            symtable.AddScope();
            addParametersToSymbolTable(argNames, argTypes);
            emit("{0}:  ;{1}", symtable[fname].Label, fname);
            prologueCode();
            emit("; braceblock for {0}", fname);
            braceblockNodeCode(n.Children[6], 0);
            emit("; final epilogue for {0}", fname);
            epilogueCode();
            symtable.DeleteScope();
        }
    }

    /// <summary>
    /// optional-return-spec -> RETURN type | lambda
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void optionalreturnspecNodeCode(TreeNode n, out VarType type)
    {
        if (n.Children.Count == 0)
            type = VarType.VOID;
        else
            typeNodeCode(n.Children[1], out type);
    }

    /// <summary>
    /// optional-type-list -> lambda | type-list
    /// </summary>
    /// <param name="n"></param>
    /// <param name="nameL"></param>
    /// <param name="typeL"></param>
    private void optionaltypelistNodeCode(TreeNode n, out List<string> nameL, out List<VarType> typeL)
    {
        if (n.Children.Count == 0)
        {
            nameL = new List<string>();
            typeL = new List<VarType>();
        }
        else
            typelistNodeCode(n.Children[0], out nameL, out typeL);
    }

    /// <summary>
    /// type-list -> ID type typelist'
    /// </summary>
    /// <param name="n"></param>
    /// <param name="nameL"></param>
    /// <param name="typeL"></param>
    private void typelistNodeCode(TreeNode n, out List<string> nameL, out List<VarType> typeL)
    {
        typeL = new List<VarType>();
        nameL = new List<string>();
        nameL.Add(n.Children[0].Token.Lexeme);
        VarType ptype;
        typeNodeCode(n.Children[1], out ptype);
        typeL.Add(ptype);

        List<VarType> typeL2;
        List<string> nameL2;
        typelistprimeNodeCode(n.Children[2], out nameL2, out typeL2);
        nameL.AddRange(nameL2);
        typeL.AddRange(typeL2);
    }

    /// <summary>
    /// type-list' -> CMA type-list | lambda
    /// </summary>
    /// <param name="n"></param>
    /// <param name="nameL"></param>
    /// <param name="typeL"></param>
    private void typelistprimeNodeCode(TreeNode n, out List<string> nameL, out List<VarType> typeL)
    {
        if (n.Children.Count > 0)
            typelistNodeCode(n.Children[1], out nameL, out typeL);
        else
        {
            nameL = new List<string>();
            typeL = new List<VarType>();
        }
    }

    /// <summary>
    /// func-call -> ID LP optional-expr-list RP | builtin-func-call
    /// </summary>
    /// <param name="n"></param>
    private void funccallNodeCode(TreeNode n, out VarType type)
    {
        if (n.Children.Count == 1)
            builtin_func_call(n.Children[0], out type);
        else if(n.Children.Count == 4)
        {
            var fname = n.Children[0].Token.Lexeme;
            var info = symtable[fname];
            if (info == null)
                throw new Exception("ERROR!!! Function does not exist or exist within Scope!!! Requested func: " + fname);

            var funcType = symtable[fname].VType as FuncVarType;
            if (funcType == null)
                throw new Exception("ERROR!!! Can't call a non-function!!! Requested func: " + fname);

            List<VarType> actualTypes;
            optionalexprListNodeCode(n.Children[2], out actualTypes);

            if (!funcType.ArgTypes.SequenceEqual(actualTypes))
            {
                printTypesListInfo(actualTypes);
                throw new Exception("Error!! Function types or count mismatch!!! FuncName: " + fname + " Expected type: " + info.VType.typeString);
            }
            emit("call {0}", symtable[fname].Label);

            //pop parameters from the stack
            int sz = 8 * actualTypes.Count;
            if (sz > 0)
                emit("add rsp, {0}", sz);

            type = (symtable[fname].VType as FuncVarType).RetType;
        }
        else
            throw new Exception("ERROR!!! Expected Child Count of 1 and builtin-func-call.\nInstead got Count:"+ n.Children.Count +" Symbol: "+ n.Children[0].Symbol);
        
    }

    /// <summary>
    /// builtin-func-call -> PRINT LP expr RP | 
    ///     INPUT LP RP | 
    ///     OPEN LP expr RP | 
    ///     READ LP expr RP | 
    ///     WRITE LP expr CMA expr RP | 
    ///     CLOSE LP expr RP
    /// </summary>
    private void builtin_func_call(TreeNode n, out VarType type)
    {
        var child = n.Children[0];
        switch (child.Symbol)
        {
            case "PRINT":
                printNodeCode(n, out type);
                break;
            case "INPUT":
                inputNodeCode(n, out type);
                break;
            case "OPEN":
                openNodeCode(n, out type);
                break;
            case "READ":
                readNodeCode(n, out type);
                break;
            case "WRITE":
                writeNodeCode(n, out type);
                break;
            case "CLOSE":
                closeNodeCode(n, out type);
                break;
            default:
                throw new Exception("ICE!!! Invalid function call!!" +
                    "\nExpected ' PRINT ', ' INPUT ', ' OPEN ', ' READ ', ' WRITE ', or ' CLOSE '. Recieved: " + child.Symbol);
        }
    }

    /// <summary>
    /// builtin-func-call -> PRINT LP expr RP
    /// int printf(const char *format, ...)
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void printNodeCode(TreeNode n, out VarType type)
    {
        VarType t;
        exprNodeCode(n.Children[2], out t);
        if (t == VarType.STRING || t == VarType.NUMBER)
        {
            emit("pop {0}", argRegister(1));
            if (t == VarType.NUMBER)
            {
                emit("mov {0}, pctg", argRegister(0));
                emit("movq xmm0, {0}", argRegister(1));
                emit("mov rax, 1");
            }
            else if (t == VarType.STRING)
            {
                emit("mov {0}, pcts", argRegister(0));
                emit("mov rax, 1");
            }
            else
                throw new Exception("Error!!! Expected to print type STRING or NUM, instead recieved: " + t);
            doFuncCall("printf");
            emit("mov {0}, 0", argRegister(0));
            doFuncCall("fflush");
            type = VarType.VOID;
        }
        else
            throw new Exception("ERROR!! Expected to print type STRING or NUMBER. Instead Recieved: " + t);

    }

    /// <summary>
    /// builtin-func-call -> INPUT LP RP
    /// int scanf ( const char * format, ... );
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void inputNodeCode(TreeNode n, out VarType type)
    {
        emit("mov {0}, pctlf", argRegister(0));         //create pctf too
        emit("mov {0}, scanbuffer", argRegister(1));
        emit("mov rax, 1");                             //num fp variadic args
        doFuncCall("scanf");
        emit("mov rax, [scanbuffer]");                 //leave result in rax
        type = VarType.NUMBER;
    }

    /// <summary>
    /// builtin-func-call -> OPEN LP expr RP
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void openNodeCode(TreeNode n, out VarType type)
    {
        VarType t;
        exprNodeCode(n.Children[2], out t);
        if (t != VarType.STRING)
            throw new Exception("ICE!!! Can't open a non string expression, got type: " + t);
        emit("mov {0}, [rsp]", argRegister(0)); //string data
        emit("mov {0}, fopenA", argRegister(1));
        doFuncCall("fopen");                    //resulting handle is in rax
        emit("pop {0}", argRegister(0));        //address of string data
        emit("mov {0}, fopenRplus", argRegister(1));
        doFuncCall("fopen");
        type = VarType.NUMBER;
    }

    /// <summary>
    /// builtin-func-call -> READ LP expr RP
    /// int fscanf ( FILE * stream, const char * format, ... );
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void readNodeCode(TreeNode n, out VarType type)
    {
        VarType t;
        exprNodeCode(n.Children[2], out t);
        if (t == VarType.NUMBER)
        {
            emit("pop {0}", argRegister(0));                //fileName
            emit("mov {0}, pctlf", argRegister(1));         //format
            emit("mov {0}, scanbuffer", argRegister(2));    //buffer
            emit("mov rax, 1");                             //num var args
            doFuncCall("fscanf");
            emit("mov rax, [scanbuffer]");
            type = VarType.NUMBER;
        }
        else
            throw new Exception("ERROR!!! expected VarType NUMBER to Read file, instead Recieved: " + t);

    }

    /// <summary>
    /// builtin-func-call -> CLOSE LP expr RP
    /// int fclose(FILE *stream)
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void closeNodeCode(TreeNode n, out VarType type)
    {
        VarType t;
        exprNodeCode(n.Children[2], out t);
        if (t != VarType.NUMBER)
            throw new Exception("ICE!!! Expected type NUMBER for close, got type: " + t);
        emit("pop {0}", argRegister(0));
        doFuncCall("fclose");
        type = VarType.VOID;
    }

    /// <summary>
    /// builtin-func-call -> WRITE LP expr CMA expr RP,
    /// int fprintf ( FILE * stream, const char * format, ... );
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void writeNodeCode(TreeNode n, out VarType type)
    {
        VarType t1, t2;
        exprNodeCode(n.Children[2], out t2);    //handle
        exprNodeCode(n.Children[4], out t1);    //data to write
        if (t2 != VarType.NUMBER)
            throw new Exception("Error!!! Expected type ' NUMBER ' for write handle, Recieved: " + t2);
        emit("pop {0}", argRegister(2));            //get data to write
        if (t1 == VarType.NUMBER)
        {
            emit("mov {0}, pctg", argRegister(1));  //format
            emit("movq xmm0, {0}", argRegister(2)); //number to xmm too
            emit("mov rax,1");                      //num fp variadic args
        }
        else if (t1 == VarType.STRING)
        {
            emit("mov {0}, pcts", argRegister(1));  //format
            emit("mov rax,1");                      //num fp variadic args
        }
        else
            throw new Exception("Expected to write type ' NUMBER ', or ' STRING ' instead Recieved: " + t1);
        emit("pop {0}", argRegister(0));            //get filepointer
        doFuncCall("fprintf");
        emit("mov {0}, 0", argRegister(0));
        doFuncCall("fflush");
        type = VarType.VOID;
    }

    /// <summary>
    /// optional-expr-list -> lambda | expr-list
    /// </summary>
    /// <param name="n"></param>
    /// <param name="types"></param>
    private void optionalexprListNodeCode(TreeNode n, out List<VarType> types)
    {
        if (n.Children.Count == 0)
            types = new List<VarType>();
        else
            exprlistNodeCode(n.Children[0], out types);
    }

    /// <summary>
    /// expr-list -> expr expr-list'
    /// </summary>
    /// <param name="n"></param>
    /// <param name="types"></param>
    private void exprlistNodeCode(TreeNode n, out List<VarType> types)
    {
        VarType t0;
        List<VarType> lst;

        exprlistprimeNodeCode(n.Children[1], out lst);
        exprNodeCode(n.Children[0], out t0);

        types = new List<VarType>();
        types.Add(t0);
        types.AddRange(lst);
    }

    /// <summary>
    /// expr-list' -> CMA expr-list | lambda
    /// </summary>
    /// <param name="n"></param>
    /// <param name="types"></param>
    private void exprlistprimeNodeCode(TreeNode n, out List<VarType> types)
    {
        if (n.Children.Count > 0)
            exprlistNodeCode(n.Children[1], out types);
        else
            types = new List<VarType>();
    }

    /// <summary>
    /// braceblock -> LBR var-decl-list stmts RBR
    /// </summary>
    /// <param name="n"></param>
    private void braceblockNodeCode(TreeNode n, int sizeOfVariableInEnclosingBlocks)
    {
        int sizeOfVariableInThisBlock;
        vardecllistNodeCode(n.Children[1], sizeOfVariableInEnclosingBlocks, out sizeOfVariableInThisBlock);
        if (sizeOfVariableInThisBlock > 0)
            emit("sub rsp, {0}", sizeOfVariableInThisBlock);

        stmtsNodeCode(n.Children[2], sizeOfVariableInEnclosingBlocks + sizeOfVariableInThisBlock);
        if (sizeOfVariableInThisBlock > 0)
            emit("add rsp, {0}", sizeOfVariableInThisBlock);
    }

    /// <summary>
    /// var-decl-list -> var-decl SEMI var-decl-list | lambda
    /// </summary>
    /// <param name="n"></param>
    private void vardecllistNodeCode(TreeNode n, int sizeOfVariablesDeclaredSoFar, out int sizeOfVariablesInThisDeclaration)
    {
        if (n.Children.Count == 0)
        {
            sizeOfVariablesInThisDeclaration = 0;
            return;
        }
        else if (n.Children.Count == 3 && n.Children[0].Symbol == "var-decl")
        {
            int sz;
            vardeclNodeCode(n.Children[0], sizeOfVariablesDeclaredSoFar, out sz);
            int sz2;
            vardecllistNodeCode(n.Children[2], sizeOfVariablesDeclaredSoFar + sz, out sz2);
            sizeOfVariablesInThisDeclaration = sz + sz2;
        }
        else
        {
            symtable.printScopes();
            throw new Exception("ICE!!! unrecognized production Expected var-decl or lambda but got size: "
                + n.Children.Count + " sym: " + (n.Children.Count > 0 ? n.Children[0].Symbol : "null"));
        }
    }

    /// <summary>
    /// var-decl -> VAR ID type
    /// </summary>
    /// <param name="n"></param>
    private void vardeclNodeCode(TreeNode n, int sizeOfVariablesDeclaredSoFar, out int sizeOfThisVariable)
    {
        string vname = n.Children[1].Token.Lexeme;
        VarType type;
        typeNodeCode(n.Children[2], out type);
        if (symtable.ContainsInCurrentScope(vname))     //duplicate Variable
            throw new Exception("ERROR!!! Duplicate Decleration!! Symtable already contains: " + vname + " in this scope!!!");

        sizeOfThisVariable = type.sizeOfThisVariable;   //get size of synthesized attribute
        
        if (symtable.ScopeCount == 1) //global
            symtable[vname] = new VarInfo(type, label(), true);
        else //the first local is at rbp-sizeof(var)
        {
            int offset = sizeOfVariablesDeclaredSoFar + sizeOfThisVariable;
            symtable[vname] = new VarInfo(type, "rbp-" + offset, false);
        }
    }

    /// <summary>
    /// type -> non-array-type | non-array-type LB num-list RB
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void typeNodeCode(TreeNode n, out VarType type)
    {
        VarType t1;
        nonarraytypeNodeCode(n.Children[0], out t1);        //Get type
        if (n.Children.Count == 4)
        {
            List<int> dims;
            numlistNodeCode(n.Children[2], out dims);
            foreach(var i in dims)
            {
                if (i < 0)
                    throw new Exception("Error!! Value is not positive!! i: "+i.ToString());
            }
            type = new ArrayVarType(t1, dims);
        }
        else
            type = t1;
    }

    /// <summary>
    /// num-list -> NUM | NUM CMA num-list
    /// </summary>
    private void numlistNodeCode(TreeNode n, out List<int> L)
    {
        L = new List<int>();
        while(true)
        {
            int num = Int32.Parse(n.Children[0].Token.Lexeme);
            L.Add(num);
            if (n.Children.Count == 1)  //recursion break
                break;
            n = n.Children[2];  //go to next child/numlist
        }
    }

    /// <summary>
    /// non-array-type -> NUMBER | STRING
    /// </summary>
    /// <param name="n"></param>
    /// <param name="type"></param>
    private void nonarraytypeNodeCode(TreeNode n, out VarType type)
    {
        string sym = n.Children[0].Symbol;
        switch (sym)
        {
            case "NUMBER":
                type = VarType.NUMBER;
                break;
            case "STRING":
                type = VarType.STRING;
                break;
            default:
                throw new Exception("ERROR!! Expected type NUMBER, or STRING for non-array-type!! Recieved: " + sym);
        }
    }

    /// <summary>
    /// stmts -> stmt stmts | lambda
    /// </summary>
    /// <param name="n"></param>
    private void stmtsNodeCode(TreeNode n, int sizeOfVariablesDeclaredSoFar)
    {
        if (n.Children.Count == 0)
            return;
        stmtNodeCode(n.Children[0], sizeOfVariablesDeclaredSoFar);
        stmtsNodeCode(n.Children[1], sizeOfVariablesDeclaredSoFar);
    }

    /// <summary>
    /// stmt -> cond | loop | return-stmt SEMI | assign SEMI | func-call SEMI
    /// </summary>
    /// <param name="n"></param>
    private void stmtNodeCode(TreeNode n, int sizeOfVariablesDeclaredSoFar)
    {
        TreeNode c = n.Children[0];
        switch (c.Symbol)
        {
            case "cond":
                condNodeCode(c, sizeOfVariablesDeclaredSoFar);
                break;
            case "loop":
                loopNodeCode(c, sizeOfVariablesDeclaredSoFar);
                break;
            case "return-stmt":
                returnstmtNodeCode(c, sizeOfVariablesDeclaredSoFar);
                break;
            case "assign":
                assignNodeCode(c);
                break;
            case "func-call":
                VarType type;
                funccallNodeCode(c, out type);
                break;
            default:
                throw new Exception("ICE!!!!! Symbol not recognized as a valid start to stmt!!\n" +
                    "Symbol Got: ' " + n.Symbol + " ' Expected: ' cond ', ' loop ', ' return-stmt ', or ' assign '");
        }
    }

    /// <summary>
    /// assign -> ID EQ expr | ID LB expr-list RB EQ expr
    /// </summary>
    /// <param name="n"></param>
    private void assignNodeCode(TreeNode n)
    {
        VarType t;
        int childrenCount = n.Children.Count;
        if (childrenCount == 3)
            exprNodeCode(n.Children[2], out t);
        else if (childrenCount == 6)
            exprNodeCode(n.Children[5], out t);
        else
            throw new Exception("Error!!! assignment invalid number of nodes!! Children Count: "+n.Children.Count);

        
        string vname = n.Children[0].Token.Lexeme;
        if (vname == null || !symtable.ContainsInCurrentScopes(vname))
        {
            symtable.printScopes();
            throw new Exception("ERROR!!! Undeclared Variable or Variable is null: " + vname != null ? vname : "null");
        }

        var vinfo = symtable[vname];
        if (childrenCount == 3)         //Scalar assignment
        {
            var atyp = t as ArrayVarType;
            var btyp = vinfo.VType as ArrayVarType;
            if ((btyp != null && atyp == null) || (atyp != null && btyp == null))
                throw new Exception("Error!! Can not assign array to val!!");

            if (atyp != null && btyp != null)
            {
                if (atyp.sizeOfThisVariable == btyp.sizeOfThisVariable)
                {
                    emit("pop {0} ", argRegister(1)); //src
                    if (vinfo.isGlobal)
                        emit("mov {0}, {1}", argRegister(0), vinfo.Label); //dst
                    else
                    {
                        emit("lea rax, [{0}]", vinfo.Label);
                        emit("mov {0}, rax ", argRegister(0)); //dst
                    }
                    emit("mov {0}, {1} ", argRegister(2), atyp.sizeOfThisVariable); //size
                    Console.WriteLine("{0} size:{1}, mem:{2}", "A", atyp.sizeOfThisVariable / 8, atyp.sizeOfThisVariable);
                    Console.WriteLine("{0} size:{1}, mem:{2}", "B", btyp.sizeOfThisVariable / 8, btyp.sizeOfThisVariable);
                    doFuncCall("memcpy");
                }
                else
                {
                    emit("mov {0}, pcts", argRegister(0));
                    emit("mov rax, 1");
                    emit("mov {0}, {1}", argRegister(1), "arrayMismatch");
                    doFuncCall("printf");
                    emit("mov {0}, 0", argRegister(0));
                    doFuncCall("fflush");
                }
                //throw new Exception("ERROR!! Cannot Assign to a array of different size!!");

            }
            else
            {
                if (vinfo.VType != t)
                    throw new Exception("Error!! Type Mistmatch: " + vinfo.VType + "!=" + t);

                emit("pop rax");    //pop from expr eval earlier
                emit("mov [{0}], rax", vinfo.Label);
            }
        }
        else                            //Array assignment
        {
            var typ = vinfo.VType as ArrayVarType;
            if (typ == null)
                throw new Exception("Error!! Variable is not an array or is null!! Vtype: "+vinfo.VType == null ? "null" : vinfo.VType.typeString);
            if (typ.baseType != t)
                throw new Exception("Error!! Type mismatch!! "+typ.baseType+"!="+t);

            putArrayAddressInRcx(vinfo, n);
            emit("pop rax");          //get val from rhs
            emit("mov [rcx], rax");
        }
    }

    /// <summary>
    /// loop -> WHILE RP expr RP braceblock
    /// </summary>
    /// <param name="n"></param>
    private void loopNodeCode(TreeNode n, int sizeOfVariablesDeclaredSoFar)
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

        braceblockNodeCode(n.Children[4], sizeOfVariablesDeclaredSoFar);
        exprNodeCode(n.Children[2], out type);      //store value into rax
        if (type != VarType.NUMBER)
            throw new Exception("ICE!!!Expression did not return back as type NUMBER");

        emit("pop rax");
        emit("cmp rax, 0");
        emit("jne {0}", loopStartLabel);            //jump to loopstart if rax != 0
        emit("{0}:", loopEndLabel);

    }

    /// <summary>
    /// cond -> IF LP expr RP braceblock | 
    /// IF LP expr RP braceblock ELSE braceblock
    /// </summary>
    /// <param name="n"></param>
    private void condNodeCode(TreeNode n, int sizeOfVariablesDeclaredSoFar)
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
            braceblockNodeCode(n.Children[4], sizeOfVariablesDeclaredSoFar);
            emit("{0}:", endifLabel);
        }
        else if (n.Children.Count == 7)
        {
            var endifLabel = label();
            var elseLabel = label();
            emit("je {0}", elseLabel);
            braceblockNodeCode(n.Children[4], sizeOfVariablesDeclaredSoFar);
            emit("jmp {0}", endifLabel);
            emit("{0}:", elseLabel);
            braceblockNodeCode(n.Children[6], sizeOfVariablesDeclaredSoFar);
            emit("{0}:", endifLabel);
        }
        else
        {
            printChildren(n, "condNodeCode");
            throw new Exception("ICE!!!! Invalid cond!!! Number of cond: " + n.Children.Count);
        }
    }

    /// <summary>
    /// return-stmt -> RETURN expr
    /// </summary>
    /// <param name="n"></param>
    private void returnstmtNodeCode(TreeNode n, int sizeOfVariableInThisBlock)
    {
        VarType type;
        exprNodeCode(n.Children[1], out type);
        if (type != VarType.NUMBER)
            throw new Exception("ICE!!!Expression did not return back as type NUMBER");
        emit("pop rax");
        epilogueCode(sizeOfVariableInThisBlock);
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
    private void relNodeCode(TreeNode n, out VarType type)
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
    /// factor -> NUM | LP expr RP | STRING-CONSTANT | ID | func-call
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
            case "STRING-CONSTANT": //Stores the address of the string data on stack
                string lbl;
                stringconstantNodeCode(child, out lbl);
                emit("mov rax, {0}", lbl);
                emit("push rax");
                type = VarType.STRING;
                break;
            case "ID":
                string vname = n.Children[0].Token.Lexeme;
                if (!symtable.ContainsInCurrentScopes(vname))
                {
                    symtable.printScopes();
                    throw new Exception("ERROR!!! Undeclared Variable: " + vname);
                }
                VarInfo vi = symtable[vname];
                ArrayVarType atyp = vi.VType as ArrayVarType;
                if (vi.VType == VarType.NUMBER || vi.VType == VarType.STRING || atyp != null)
                {
                    if (atyp != null)
                    {
                        Console.WriteLine("pushing {0} size:{1}, mem:{2}", vname, atyp.sizeOfThisVariable / 8, atyp.sizeOfThisVariable);
                        string label = symtable[vname].Label;
                        if (symtable[vname].isGlobal)
                            emit("mov rax,{0}", label);
                        else
                            emit("lea rax,[{0}]", label);
                    }
                    else
                        emit("mov rax,[{0}]", symtable[vname].Label);   //push value
                    emit("push rax");
                    type = vi.VType;
                }
                else
                    throw new Exception("ICE!!! Expected type NUMBER, STRING, or ARRAY. Recieved: " + vi.VType);
                break;
            case "func-call":
                funccallNodeCode(child, out type);
                if (type == VarType.VOID)
                    throw new Exception("ICE!!! Can't use VOID in math expressions");
                emit("push rax");
                break;
            case "array-access":
                vname = child.Children[0].Token.Lexeme;
                var vinfo = symtable[vname];
                if (vinfo == null)
                    throw new Exception("Error!!! Trying to access undeclared variable: "+vname);

                string lbl1 = label(), lbl2 = label();
                putArrayAddressInRcx(vinfo, n.Children[0]);
                emit("mov rax, [rcx]");                                             //get address of access
                emit("lea rbx, [{0}]", vinfo.Label);                                //get address start
                emit("sub rax, rbx");                                               //get array access
                emit("mov rdx, {0}", vinfo.VType.sizeOfThisVariable);
                emit("movq xmm0, rdx");
                emit("movq xmm1, rax");
                emit("cmpnltsd xmm1, xmm0");                                        //check array access >= arraySize
                emit("movq rax, xmm1");
                emit("cmp rax, 0");                                                 //is it >= returns 0 as in it is >=
                emit("jne {0}", lbl1);                                              //do next check
                exit();
                emit("{0}:", lbl1);
                emit("mov rax, [rcx]");                                             //get address of access
                emit("lea rbx, [{0}]", vinfo.Label);                                //get address start
                emit("sub rax, rbx");                                               //get array access
                emit("test rax, rax");                                              //Test if value is signed
                emit("jns {0}",lbl2);
                exit();
                emit("{0}:", lbl2);
                emit("mov rax, [rcx]");
                emit("push rax");
                type = (vinfo.VType as ArrayVarType).baseType;
                break;
            default:
                throw new Exception("ICE!!! Expected NUM, LP, STRING-CONSTANT, or ID Recieved:" + child.Symbol);
        }
    }

    /// <summary>
    /// array-access -> ID LB expr-list RB
    /// </summary>
    /// <param name="vinfo"></param>
    /// <param name="exprListNode"></param>
    private void putArrayAddressInRcx(VarInfo vinfo, TreeNode exprListNode)
    {
        ArrayVarType typ = vinfo.VType as ArrayVarType;
        if (typ == null)
            throw new Exception("ICE!!! Arraytype cannot be null or typ not Array type!! tpy: " + typ == null ? "null" : typ.typeString);

        List<VarType> types;
        exprlistNodeCode(exprListNode.Children[2], out types); //expr-list -> expr expr-list'
        if (types.Count != typ.arrayDimensions.Count)
            throw new Exception("Error!! Arrays dimension mismatch!!");

        foreach (var t in types)
        {
            if (t != VarType.NUMBER)
                throw new Exception("Error!! only numbers are valid as array indices!! type: " + t.typeString);
        }

        emit("mov rcx, 0");
        for (int i = 0; i < typ.arrayDimensions.Count; i++)
        {
            int product = 1;
            for (int j = i + 1; j < typ.arrayDimensions.Count; j++)
                product *= typ.arrayDimensions[j];
            emit("pop rax");                    //get next from expr-list
            emit("movq xmm0, rax");             //convert to double
            emit("cvtsd2si rax, xmm0");         //convert to int
            emit("imul rax, rax,{0}", product); //dest, op1, op2
            emit("add rcx, rax");
        }

        if (vinfo.isGlobal)
        {
            emit("shl rcx, 3"); //same as imul rcx, 8 but faster
            emit("add rcx, {0}", vinfo.Label);
        }
        else
            emit("lea rcx, [rcx*8+{0}]", vinfo.Label);
    }

    private void addParametersToSymbolTable(List<string> argNames, List<VarType> argTypes)
    {
        int offs = 16;
        for(int i = 0; i < argNames.Count; i++)
        {
            symtable[argNames[i]] = new VarInfo(argTypes[i], "rbp+" + offs, symtable[argNames[i]].isGlobal);
            offs += 8;
        }
    }

    private void stringconstantNodeCode(TreeNode n, out string lbl)
    {
        //Remove Leading and Trailing " and replace escaped newlines with newline
        string s = n.Token.Lexeme;
        s = s.Substring(1, s.Length-2).Replace("\\n", "\n");
        if (!stringPool.ContainsKey(s))
            stringPool.Add(s, label());
        lbl = stringPool[s];
    }

    private void printTypesListInfo(List<VarType> types)
    {
        Console.WriteLine("Count:{0}, VarTypes: ", types.Count);
        foreach(VarType v in types)
            Console.WriteLine("\tType:{0}", v.typeString);
    }
    private void outputSymbolTableInfo()
    {
        //output only globals
        foreach(var vname in symtable.scopes[0].data.Keys)
        {
            var vinfo = symtable[vname];
            if(vinfo.VType as FuncVarType == null)
            {
                emit("{0}: ;{1}", vinfo.Label, vname);
                emit("resb {0}", vinfo.VType.sizeOfThisVariable);
            }
        }
    }
    private void outputStringPoolInfo()
    {
        foreach(var tmp in stringPool)
        {
            string theString = tmp.Key;
            string lbl = tmp.Value;
            emit(lbl + ":");
            byte[] stringBytes = System.Text.Encoding.ASCII.GetBytes(theString);
            foreach(byte b in stringBytes)
                emit("db {0}", b);
            emit("db 0"); //null terminator
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
    private void movTwoVarsTo_xmmRegisters()
    {
        movRaxTo_xmmRegister("xmm1");//second operand
        movRaxTo_xmmRegister("xmm0");//first operand
    }
    private void movRaxTo_xmmRegister(string xmmNum)
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
    private void exit()
    {
        doFuncCall("abort");
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