using System;
using System.Collections.Generic;

enum VarType { NUMBER, STRING , VOID};

class SymbolTable //done
{
    public List<Scope> scopes = new List<Scope>();
    public SymbolTable()
    {
        this.AddScope();
    }
    public VarInfo this[string varname]
    {
        get //inner most locals can use locals in scopes above their own, but not other way around
        {
            for (int i = scopes.Count - 1; i >= 0; i--)
            {
                var tmp = scopes[i][varname];
                if (tmp != null)
                    return tmp;
            }
            return null;
        }
        set
        { scopes[scopes.Count - 1][varname] = value; }
    }
    public int ScopeCount
    {
        get { return scopes.Count; }
    }
    public bool ContainsInCurrentScope(string varname)
    {
        return scopes[scopes.Count - 1][varname] != null;
    }
    public bool ContainsInCurrentScopes(string varname)
    {
        for(int i = scopes.Count - 1; i >= 0; i--)
        {
            if(scopes[i][varname] != null)
                return true;
        }
        return false;
        
    }
    public void AddScope()
    {
        scopes.Add(new Scope());
    }
    public void DeleteScope()
    {
        scopes.RemoveAt(scopes.Count - 1);
    }
    public void printScopes()
    {
        Console.WriteLine("SymbolTable:");
        for (int i = 0; i < scopes.Count; i++)
        {
            Console.WriteLine("\tScope{0}:", i);
            foreach (KeyValuePair<string, VarInfo> pair in scopes[i].data)
            {
                Console.WriteLine("\t\t{0} : ({1}, {2})", pair.Key, pair.Value.Label, pair.Value.VType.ToString());
            }
        }
    }
}

class VarInfo //done
{
    public string Label; //assembly label for this var
    public VarType VType; //"Type" is a builtin name
    public VarInfo(VarType t, string label)
    {
        this.VType = t;
        this.Label = label;
    }
}

class Scope //done
{
    public Dictionary<string, VarInfo> data = new Dictionary<string, VarInfo>();
    public VarInfo this[string varname]
    {
        get //returns Varinfo if varname exists, otherwise returns null
        {
            return data.ContainsKey(varname) ? data[varname] : null;
        }
        set
        {
            data[varname] = value;
        }
    }
}