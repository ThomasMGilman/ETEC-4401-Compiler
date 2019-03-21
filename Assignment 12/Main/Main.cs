using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Test {
   
    class MainClass
    {
    	[STAThread]
        public static void Main(string[] args)
        {
            string gfile;
            if(args.Length == 0) {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "All stuff|*.*";
                dlg.ShowDialog();
                gfile = dlg.FileName;
                if(gfile.Trim().Length == 0)
                    return;
                dlg.Dispose();
            } else {
                gfile = args[0];
            }
            
            var startState = Compiler.makelr0dfa(gfile);

            Dictionary<dynamic,int> nmap = new Dictionary<dynamic,int>();
            walk(startState, (n) => {
                int c = nmap.Count;
                nmap[n] = c;
            });

            var f = System.IO.Path.GetFileName(gfile);

            dumpText(f.Replace(".txt","-dfa.txt"), startState,nmap );
            dumpDot(f.Replace(".txt","-dfa.dot"),startState, nmap ); 
        }

        static void dumpText<T>(string fname, T startState, Dictionary<dynamic,int> nmap){
            using(StreamWriter wr = new StreamWriter(fname)) {
                walk(startState, (T n) => {
                    dynamic d = n;
                    wr.WriteLine("State "+nmap[d]);
                    wr.WriteLine(d.Items.Count+" items");


                    List<dynamic> tmp = new List<dynamic>();
                    foreach(var item in d.Items){
                        tmp.Add(item);
                    }

                    shuffle(tmp);

                    foreach(var item in tmp){
                        wr.WriteLine("\t"+item.Lhs+" "+item.Dpos+" "+String.Join(" ",item.Rhs));
                    }
                    wr.WriteLine(d.Transitions.Count+" transitions");
                    foreach( var keyvalue in d.Transitions){
                        string sym = keyvalue.Key;
                        dynamic node2 = keyvalue.Value;
                        wr.WriteLine("\t"+sym+" "+nmap[node2]);
                    }
                });
            }
            Console.WriteLine("Wrote DFA to "+fname);
        }
            
        static void dumpDot<T>(string fname, T startState, Dictionary<dynamic,int> nmap){
            using(StreamWriter wr = new StreamWriter(fname)) {
                wr.Write("digraph d{\n");
                wr.Write("node [shape=box];\n");
                walk(startState, (T n) => {
                    wr.Write("n" + nmap[n] + " [label=<");
                    wr.Write(nmap[n] + "<br/>");
                    dynamic nd = n;
                    foreach(dynamic item in nd.Items) {
                        string lhs = item.Lhs;
                        var rhs = item.Rhs;
                        int dpos = item.Dpos;
                        wr.Write(lhs);
                        wr.Write("&rarr;");
                        for(int i = 0; i < dpos; i++)
                            wr.Write(rhs[i] + " ");
                        wr.Write("&bull; ");
                        for(int i = dpos; i < rhs.Count; ++i)
                            wr.Write(rhs[i] + " ");
                        wr.Write("<br/>");
                    }
                    wr.Write(">];");
                });

                walk(startState, (T n) => {
                    dynamic nd = n;
                    foreach( var keyvalue in nd.Transitions){
                        string sym = keyvalue.Key;
                        dynamic node2 = keyvalue.Value;
                        wr.Write( "n"+nmap[n]+"->n"+nmap[node2]+" [label=\""+sym+"\"];\n");
                    }
                });

                wr.Write("}\n");
            }

            Console.WriteLine("Wrote DFA to " + fname);
        }

        static Random R = new Random();
        static void shuffle<T>( List<T> tmp){
            for(int i = 0; i < tmp.Count; ++i) {
                int j = R.Next(tmp.Count);
                var x = tmp[i];
                tmp[i] = tmp[j];
                tmp[j] = x;
            }
        }

        static void walk<T>(T node, Action<T> callback, HashSet<T> visited=null){
            if(visited == null)
                walk(node, callback, new HashSet<T>());
            else{
                if( !visited.Contains(node) ){
                    visited.Add(node);
                    callback(node);
                    dynamic noded = node;
                    List<dynamic> tmp = new List<dynamic>();
                    foreach(var keyvalue in noded.Transitions) {
                        tmp.Add(keyvalue);
                    }
                    shuffle(tmp);
                    foreach(var keyvalue in tmp){
                        //string sym = keyvalue.Key;
                        dynamic node2 = keyvalue.Value;
                        walk(node2, callback, visited);
                    }
                }
            }
        }
    }
}