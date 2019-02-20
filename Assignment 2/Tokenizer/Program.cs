//Thomas Gilman
//Jim Hudson
//ETEC 4401 Compiler
// 22nd January, 2019
//Assignment 2 Tokenization
using System.Windows.Forms;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Tokenizer
{
    public struct Terminal
    {
        public string terminal;
        public Regex nonTerminal;
    }

    public class Token
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
        public override string ToString()
        {
            return string.Format("[{0,10} {1,4} {2,25}]",
                this.sym, this.line, this.lexeme);
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string grammarFile, tokenFile, line;
            int index = 0, lineNum = 0, numError = 0;
            bool tokenized = false;
            String[] grammarLines, tokenLines;
            List<Token> tokens = new List<Token>();
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

                dlg = new OpenFileDialog();
                dlg.Filter = "All files|*.*";
                dlg.ShowDialog();
                tokenFile = dlg.FileName;
                if (tokenFile.Trim().Length == 0)
                    return;
                dlg.Dispose();
            }
            else
            {
                grammarFile = args[0];
                tokenFile = args[1];
            }

            grammarLines = System.IO.File.ReadAllLines(@grammarFile);
            tokenLines = System.IO.File.ReadAllLines(@tokenFile);
            line = grammarLines[0];
            while(line.Length != 0)
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
                        Terminal T;
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
            Console.WriteLine("List Contents of Grammar in order:");
            foreach(Terminal t in terminals)
            {
                Console.WriteLine("\tKey: {0}\tRegex: {1}", t.terminal, t.nonTerminal.ToString());
            }

            //Tokenization here
            lineNum = 0;
            index = 0;
            StringBuilder sb = new StringBuilder();
            foreach (string l in tokenLines)
                sb.Append(l.Trim());
            line = sb.ToString();

            Console.WriteLine("\nTokenized file:");
            while (index < line.Length)
            {
                tokenized = false;
                if (line[index] == '\n')
                {
                    lineNum++;
                    index++;
                    Console.Write('\n');
                }
                else if (line[index] == ' ')
                    index++;
                else
                {
                    foreach (Terminal t in terminals)
                    {
                        if (tokenized)
                            break;
                        var sym = t.nonTerminal.Match(line, index);
                        if (sym.Success && sym.Index == index)
                        {
                            Token newT = new Token(sym.ToString(), t.terminal, lineNum);
                            index += sym.Length;
                            tokenized = true;
                            tokens.Add(newT);
                            Console.Write("{0} ",t.terminal);
                        }
                    }
                    if (tokenized == false)
                    {
                        Console.WriteLine("\nERROR!! : failed to Tokenize! line {0}: '{1}' at index: {2}", lineNum, line, index);
                        Console.Read();
                        System.Environment.Exit(-1);
                    }
                }
            }
            Console.WriteLine("\n\nSuccessfully tokenized!!!");
            Console.Read();
        }
    }
}
