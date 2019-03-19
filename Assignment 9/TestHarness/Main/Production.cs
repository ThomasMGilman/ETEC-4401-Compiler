using System;
using System.Collections.Generic;

public class Production : List<string> {
    public Production(string s){
        s = s.Trim();
        if( s == "lambda" )
            return;
        string[] tmp = s.Split( new string[]{" "}, StringSplitOptions.RemoveEmptyEntries );
        foreach( string x in tmp ){
            this.Add(x);
        }
    }
    public override string ToString(){
        return String.Join(" ", this);
    }
}
