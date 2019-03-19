using System;

internal class Token
{
    public string sym;
    public string lexeme;
    public int line;

    public Token(string sym, string lexeme, int line)
    {
        this.sym = sym;
        this.lexeme = lexeme;
        this.line = line;
    }
    public override string ToString(){
        return string.Format("[{0,10} {1,4} {2,25}]",this.sym,this.line,this.lexeme);
    }
        
}
