using System;
using System.Linq;
using System.Collections.Generic;

class VarType
{
    public readonly string typeString;
    protected int size = 8;
    protected VarType(string t)
    {
        typeString = t;
    }
    public static readonly VarType NUMBER   = new VarType("$number");
    public static readonly VarType STRING   = new VarType("$string");
    public static readonly VarType VOID     = new VarType("$void");
    public static readonly VarType FUNCTION = new VarType("$function");
    public virtual int sizeOfThisVariable
    {
        get { return size; }
    }
    public static bool operator == (VarType v1, VarType v2)
    {
        if (object.ReferenceEquals(v1, null))
            return object.ReferenceEquals(v2, null);
        return v1.Equals(v2);
    }
    public static bool operator != (VarType v1, VarType v2)
    {
        return !(v1 == v2);
    }
    public override bool Equals(object obj)
    {
        VarType v2 = (obj as VarType);
        if (v2 == null)
            return false;
        return this.typeString == v2.typeString;
    }
    public override int GetHashCode()
    {
        return typeString.GetHashCode();
    }
}

class ArrayVarType : VarType
{
    public readonly VarType baseType;
    public readonly List<int> arrayDimensions;
    public ArrayVarType(VarType baseType, List<int> dims) : base("$array")
    {
        this.baseType = baseType;
        this.arrayDimensions = dims;
        int num = 1;
        if(arrayDimensions != null)
        {
            foreach (var i in arrayDimensions)
                num *= i;
        }
        this.size = baseType.sizeOfThisVariable * num;
    }
    public static bool operator == (ArrayVarType v1, ArrayVarType v2)
    {
        return v1.Equals(v2);
    }
    public static bool operator != (ArrayVarType v1, ArrayVarType v2)
    {
        return !(v1 == v2);
    }
    public override bool Equals(object obj)
    {
        ArrayVarType v2 = (obj as ArrayVarType);
        if (v2 == null)
            return false;
        return baseType.Equals(v2.baseType) && this.arrayDimensions.SequenceEqual(v2.arrayDimensions);
    }
    public override int GetHashCode()
    {
        int hashNum = 0;
        hashNum += baseType.GetHashCode();
        foreach (var i in arrayDimensions)
            hashNum += i.GetHashCode();
        return hashNum;
    }
}


class FuncVarType : VarType
{
    public readonly List<VarType> ArgTypes = new List<VarType>();
    public readonly VarType RetType;
    public FuncVarType( List<VarType> argtypes, VarType rettype) : base("$function")
    {
        this.RetType    = rettype;
        this.ArgTypes   = argtypes;
    }
    public static bool operator ==(FuncVarType v1, FuncVarType v2)
    {
        if (object.ReferenceEquals(v1, null))
            return object.ReferenceEquals(v2, null);
        return v1.Equals(v2);
    }
    public static bool operator !=(FuncVarType v1, FuncVarType v2)
    {
        return !(v1 == v2);
    }
    public override bool Equals(object obj)
    {
        FuncVarType v2 = (obj as FuncVarType);
        if (v2 == null)             return false;
        if (!base.Equals(v2))       return false;
        if (RetType != v2.RetType)  return false;

        return ArgTypes.SequenceEqual(v2.ArgTypes); //using System.Linq
    }
    public override int GetHashCode()
    {
        int hashVal = 0;
        foreach (VarType v in ArgTypes)
            hashVal += v.GetHashCode();
        return (hashVal += RetType.GetHashCode());
    }
}

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

class VarInfo
{
    public string Label; //assembly label for this var
    public bool isGlobal;
    public VarType VType; //"Type" is a builtin name
    public VarInfo(VarType t, string label, bool isGlobal)
    {
        this.VType      = t;
        this.Label      = label;
        this.isGlobal   = isGlobal;
    }
}

class Scope
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