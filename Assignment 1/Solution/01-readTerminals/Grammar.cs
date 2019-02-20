using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Grammar
{
    class Terminal{
        public string sym;
        public Regex rex;
        public Terminal(string sym, Regex rex){
            this.sym=sym;
            this.rex = rex;
        }
        public override string ToString(){
            return String.Format("[{0,-20} | {1,-20}]", this.sym, this.rex);
        }
    }

    List<Terminal> terminals = new List<Terminal>();
    HashSet<string> terminalSet = new HashSet<string>();
    
    public Grammar(string specfile, bool caseInsensitive)
    {
        string[] lines = System.IO.File.ReadAllLines(specfile);
        foreach(string untrimmedLine in lines) {
            string line = untrimmedLine.Trim();

            if(line.Length == 0)
                break;
            
            string[] tmp = line.Split( new string[]{"->"}, 2, StringSplitOptions.None);
            if(tmp.Length != 2) {
                throw new Exception("Bad grammar line: " + line);
            }
            string lhs = tmp[0].Trim();
            string rhs = tmp[1].Trim();

            if( lhs.Length == 0 || rhs.Length == 0 ){
                throw new Exception("Bad grammar line: " + line);
            }

            if( terminalSet.Contains(lhs) ){
                throw new Exception("Duplicate lhs: "+lhs);
            }

            terminalSet.Add(lhs);

            try{
                Regex rex;
                if( caseInsensitive )
                    rex = new Regex(rhs, RegexOptions.IgnoreCase);
                else
                    rex = new Regex(rhs);
                terminals.Add(new Terminal(lhs,rex));
            } catch(Exception){
                throw new Exception("Bad regular expression: "+rhs);
            }
        }
    }
    public override string ToString(){
        List<string> L = new List<string>();
        foreach(var v in this.terminals) {
            L.Add(v.ToString());
        }
        return string.Join("\n", L);
    }
}

