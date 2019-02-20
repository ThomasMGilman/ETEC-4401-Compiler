//Thomas Gilman
//Jim Hudson
//ETEC 4401 Compiler
// 29th January, 2019
//Assignment 3 CFG intro
using System.Windows.Forms;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace CFG
{
    public class Production
    {
        public string lhs;
        public string rhs;
        public int line;
        public List<string[]> productions;
        public Production(string lhs, string rhs, int line)
        {
            this.lhs = lhs;
            this.rhs = rhs;
            this.line = line;
            this.productions = new List<string[]>();
            setProductions();
        }
        public override string ToString()
        {
            return string.Format("[{0,10} {1,4} {2,25}]",
                this.lhs, this.line, this.rhs);
        }
        public void setProductions()
        {
            string[] prods = rhs.Split('|');
            for(int i = 0; i <  prods.Length; i++)
            {
                prods[i] = prods[i].Trim();
                this.productions.Add(prods[i].Split(' '));
            }
        }
    }

    public struct Terminal
    {
        public string terminal;
        public Regex nonTerminal;
    }

    public struct longestProduction
    {
        public Production p;
        public int longestProdNum;
        public string[] longestProd;
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string grammarFile, line;
            int index = 0, lineNum = 0;
            String[] grammarLines;
            List<Production> cfg = new List<Production>();
            List<Terminal> terminals = new List<Terminal>();
            Regex grammarReg, middle = new Regex(@"->");

            if(args.Length == 0)
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "All files|*.*";
                dlg.ShowDialog();
                grammarFile = dlg.FileName;
                if (grammarFile.Trim().Length == 0)
                    return;
                dlg.Dispose();
            }
            else
                grammarFile = args[0];

            grammarLines = System.IO.File.ReadAllLines(@grammarFile);
            line = grammarLines[0];
            while(line.Length != 0)                 //grab terminals and nonterminals
            {
                line = line.Trim();
                index = middle.Match(line).Index;
                var mid = middle.Match(line);
                var rhs = line.Substring(index + mid.Length).Trim();
                var term = line.Substring(0, index).Trim();
                if (rhs.Length > 0 && term.Length > 0)
                {
                    try
                    {
                        grammarReg = new Regex(rhs);
                        if (terminals.Any(item => item.terminal == term))
                        {
                            Console.Write("\nError at line {0}, Terminal already present in list lhs: {1}", lineNum, term);
                            Console.Read();
                            System.Environment.Exit(-1);
                        }
                        Terminal T = new Terminal();
                        T.terminal = term;
                        T.nonTerminal = grammarReg;
                        terminals.Add(T);
                    }
                    catch
                    {
                        Console.Write("\nError at line: {0}, Invalid Regex: {1}", lineNum, rhs);
                        Console.Read();
                        System.Environment.Exit(-1);
                    }
                    line = grammarLines[++lineNum];
                }
                else
                {
                    Console.WriteLine("\nERROR: '{0} -> {1}' has invalid (lhs or rhs)", term, rhs);
                    Console.Read();
                    System.Environment.Exit(-1);
                }
            }
            //Console.WriteLine("List Contents of Grammar in order:");
            //foreach(Terminal t in terminals)                 //Print out grammar terminals and nonterminals
            //{
            //    Console.WriteLine("\tKey: {0}\tRegex: {1}", t.terminal, t.nonTerminal.ToString());
            //}
            //Console.WriteLine("\n");

            while(lineNum < grammarLines.Length)          //Deal with production tokens and add them to list after whitespace
            {
                line = grammarLines[lineNum++];
                line = line.Trim();
                index = middle.Match(line).Index;
                var mid = middle.Match(line);
                var rhs = line.Substring(index + mid.Length).Trim();
                var lhs = line.Substring(0, index).Trim();

                if (rhs.Length > 0 && lhs.Length > 0 && line.Length > 0)
                {
                    //Console.WriteLine("{0} {1} {2}", lhs, mid.ToString(), rhs);
                    Production newT = new Production(lhs, rhs, lineNum);
                    cfg.Add(newT);
                }
            }
            //Console.WriteLine("\n");

            longestProduction longProd = new longestProduction();
            bool setFirst = true;
            foreach(Production p in cfg)                            //find first longest production
            {
                Console.WriteLine("{0} {1}", p.lhs, p.productions.Count);
                if(setFirst)
                {
                    longProd.p = p;
                    longProd.longestProd = p.productions[0];
                    longProd.longestProdNum = p.productions[0].Length;
                    foreach(string[] prod in p.productions)
                    {
                        if(prod.Length > longProd.longestProdNum)
                        {
                            longProd.longestProd = prod;                //set longest production to longest production
                            longProd.longestProdNum = prod.Length;      //set longest production num to new longest prod num
                        }
                    }
                    setFirst = false;
                }
                else
                {
                    foreach(string[] prod in p.productions)
                    {
                        if (prod.Length > longProd.longestProdNum)
                        {
                            longProd.p = p;                             //set longest production to production with new longest production
                            longProd.longestProd = prod;                //set longest production to longest production
                            longProd.longestProdNum = prod.Length;      //set longest production num to new longest prod num
                        }
                    }
                }
            }
            Console.Write("{0} {1} -> ", longProd.longestProdNum, longProd.p.lhs);
            foreach (string s in longProd.longestProd)
                Console.Write("{0} ",s);
            Console.WriteLine("\n");
            Console.Read();
        }
    }
}