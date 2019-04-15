using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Test {
   
    class MainClass
    {
    	[STAThread]
        public static void Main(string[] args)
        {
            string gfile;
            string ifile;
            
            if(args.Length == 0) {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "All stuff|*.*";
                dlg.ShowDialog();
                gfile = dlg.FileName;
                if(gfile.Trim().Length == 0)
                    return;
                dlg.ShowDialog();
                ifile = dlg.FileName;
                if(ifile.Trim().Length == 0)
                    return;
                dlg.Dispose();
            } else {
                gfile = args[0];
                ifile = args[1];
            }
            
            var root = Compiler.parseTree(gfile,ifile, 2);
            dumpTree("tree.dot",root);
        }

        static void dumpTree<T>(string fname, T root){

            var nmap = new Dictionary<T,int>();

            walk(root, (T n) => {
                int c = nmap.Count;
                nmap[n] = c;
            });

            using(StreamWriter wr = new StreamWriter(fname)) {
                wr.Write("digraph d{\n");
                wr.Write("node [shape=box];\n");
                walk(root, (T n1) => {
                    dynamic n = n1;
                    wr.Write("n" + nmap[n] + " [label=\"");
                    wr.Write(n.Symbol);
                    if(n.Token != null) {
                        string tmp = n.Token.ToString();
                        tmp = Regex.Replace(tmp,@"\s+"," ");
                        wr.Write("\\n" + tmp);
                    } 
                    wr.Write("\"];");   
                });


                walk(root, (T n1) => {
                    dynamic n = n1;
                    foreach( var c in n.Children ){
                        wr.Write( "n"+nmap[n]+"->n"+nmap[c]+";\n");
                    }
                });

                wr.Write("}\n");
            }
            Console.WriteLine("Wrote tree to " + fname);
        }

        static void walk<T>(T node, Action<T> callback){
            callback(node);
            dynamic noded = node;
            foreach(var c in noded.Children ){
                walk(c, callback);
            }
        }
    }
}