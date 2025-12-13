using System;
using System.Collections.Generic;
using System.Linq;

namespace Tema2_LFC
{
    public enum SymbolType
    {
        GlobalVariable,
        LocalVariable,
        Function,
        Parameter
    }

    public class Symbol
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public SymbolType Category { get; set; }
        public int Line { get; set; }
        public object? Value { get; set; }
        public bool IsConstant { get; set; } = false;
    }

    public class FunctionSymbol : Symbol
    {
        public string? ReturnType { get; set; }
        public List<Symbol> Parameters { get; set; } = new List<Symbol>();
        public List<Symbol> LocalVariables { get; set; } = new List<Symbol>();
        public List<(string Type, int Line)> ControlStructures { get; set; } = new List<(string, int)>();
        public HashSet<string> CallsFunctions { get; set; } = new HashSet<string>();
        public bool HasReturn { get; set; } = false;
        public bool IsRecursive { get; set; } = false;

        public bool IsMain => Name == "main";

        public FunctionSymbol()
        {
            Category = SymbolType.Function;
        }
    }

    public class Scope
    {
        public Scope? Parent { get; set; }
        public Dictionary<string, Symbol> Symbols { get; set; } = new Dictionary<string, Symbol>();

        public Scope(Scope? parent = null)
        {
            Parent = parent;
        }

        public void Define(Symbol symbol)
        {
            if (symbol.Name != null)
                Symbols[symbol.Name] = symbol;
        }

        public Symbol? Resolve(string name)
        {
            if (Symbols.ContainsKey(name))
            {
                return Symbols[name];
            }

            if (Parent != null)
            {
                return Parent.Resolve(name);
            }

            return null;
        }
    }

    public class SymbolTable
    {
        public Scope CurrentScope { get; private set; }
        public Scope GlobalScope { get; private set; }

        public SymbolTable()
        {
            GlobalScope = new Scope();
            CurrentScope = GlobalScope;
        }

        public void EnterScope()
        {
            CurrentScope = new Scope(CurrentScope);
        }

        public void ExitScope()
        {
            if (CurrentScope.Parent != null)
            {
                CurrentScope = CurrentScope.Parent;
            }
        }

        public void Define(Symbol symbol)
        {
            CurrentScope.Define(symbol);
        }

        public Symbol? Resolve(string name)
        {
            return CurrentScope.Resolve(name);
        }

        // Methods for reporting
        public List<Symbol> GetGlobalVariables()
        {
            return GlobalScope.Symbols.Values
                .Where(s => s.Category == SymbolType.GlobalVariable)
                .ToList();
        }
        
        // Helper to collect all functions from global scope
        public List<FunctionSymbol> GetFunctions()
        {
            return GlobalScope.Symbols.Values
                .OfType<FunctionSymbol>()
                .ToList();
        }
    }
}
