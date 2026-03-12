using Antlr4.Runtime.Misc;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tema2_LFC
{
    public class SemanticVisitor : BallLangBaseVisitor<object>
    {
        public SymbolTable SymbolTable { get; } = new SymbolTable();
        public List<string> Errors { get; } = new List<string>();
        
        private FunctionSymbol? currentFunction = null;
        private int mainCount = 0;
        private HashSet<string> calledFunctions = new HashSet<string>();

        // Type compatibility mapping
        private static readonly Dictionary<string, HashSet<string>> TypeCompatibility = new Dictionary<string, HashSet<string>>
        {
            { "player", new HashSet<string> { "player" } },  // int
            { "score", new HashSet<string> { "score", "player", "salary" } },  // double accepts int, float, double
            { "salary", new HashSet<string> { "salary", "player" } },  // float accepts int, float
            { "team", new HashSet<string> { "team" } },  // string
            { "goal", new HashSet<string> { "goal" } },  // bool
            { "tactic", new HashSet<string> { "tactic" } }  // void
        };

        public override object VisitProgram([NotNull] BallLangParser.ProgramContext context)
        {
            // First pass: collect all function declarations
            base.VisitProgram(context);
            
            // After visiting all, check for main uniqueness
            if (mainCount == 0)
            {
                Errors.Add("Error: Program must contain exactly one 'main' function.");
            }
            else if (mainCount > 1)
            {
                Errors.Add($"Error: Program contains {mainCount} 'main' functions, but exactly one is required.");
            }

            // Check for function call validity after all functions are defined
            foreach (var func in SymbolTable.GetFunctions())
            {
                // Determine if function is recursive or iterative
                func.IsRecursive = func.CallsFunctions.Contains(func.Name);
            }

            return null;
        }

        public override object VisitGlobalDeclaration([NotNull] BallLangParser.GlobalDeclarationContext context)
        {
            string type = context.type().GetText();
            int line = context.Start.Line;
            bool isConstant = context.BALL() != null;

            // Iterate over all variable declarators
            foreach (var declarator in context.varDeclarator())
            {
                string name = declarator.IDENTIFIER().GetText();

                // Check Uniqueness
                if (SymbolTable.Resolve(name) != null)
                {
                    Errors.Add($"Error at line {line}: Global variable '{name}' is already defined.");
                }
                else
                {
                    object? initValue = null;
                    string? initType = null;
                    
                    if (declarator.expression() != null)
                    {
                        initValue = Visit(declarator.expression());
                        initType = GetExpressionType(declarator.expression());
                        
                        // Type compatibility check
                        if (initType != null && !IsTypeCompatible(type, initType))
                        {
                            Errors.Add($"Error at line {line}: Cannot initialize variable '{name}' of type '{type}' with value of type '{initType}'.");
                        }
                    }

                    var symbol = new Symbol
                    {
                        Name = name,
                        Type = type,
                        Category = SymbolType.GlobalVariable,
                        Line = line,
                        Value = initValue,
                        IsConstant = isConstant
                    };
                    SymbolTable.Define(symbol);
                }
            }

            return null;
        }

        public override object VisitFunctionDeclaration([NotNull] BallLangParser.FunctionDeclarationContext context)
        {
            string returnType = context.type().GetText();
            string name = context.IDENTIFIER().GetText();
            int line = context.Start.Line;

            // Check for main function
            if (name == "main")
            {
                mainCount++;
            }

            if (SymbolTable.Resolve(name) != null)
            {
                Errors.Add($"Error at line {line}: Function '{name}' is already defined.");
                return null;
            }

            var funcSymbol = new FunctionSymbol
            {
                Name = name,
                ReturnType = returnType,
                Category = SymbolType.Function,
                Line = line
            };
            SymbolTable.Define(funcSymbol);

            currentFunction = funcSymbol;
            SymbolTable.EnterScope();

            // Parameters
            if (context.parameters() != null)
            {
                foreach (var param in context.parameters().parameter())
                {
                    string pType = param.type().GetText();
                    string pName = param.IDENTIFIER().GetText();
                    
                    var pSymbol = new Symbol { Name = pName, Type = pType, Category = SymbolType.Parameter, Line = param.Start.Line };
                    
                    if (SymbolTable.CurrentScope.Symbols.ContainsKey(pName))
                    {
                        Errors.Add($"Error at line {param.Start.Line}: Duplicate parameter name '{pName}'.");
                    }
                    else
                    {
                        SymbolTable.Define(pSymbol);
                        funcSymbol.Parameters.Add(pSymbol);
                    }
                }
            }

            // Visit Block
            Visit(context.block());

            // Check return statement for non-void functions
            if (returnType != "tactic" && !funcSymbol.HasReturn)
            {
                Errors.Add($"Error at line {line}: Function '{name}' with return type '{returnType}' must have a return statement.");
            }

            SymbolTable.ExitScope();
            currentFunction = null;
            return null;
        }

        public override object VisitVariableDeclaration([NotNull] BallLangParser.VariableDeclarationContext context)
        {
            string type = context.type().GetText();
            int line = context.Start.Line;
            bool isConstant = context.BALL() != null;

            // Iterate over all variable declarators
            foreach (var declarator in context.varDeclarator())
            {
                string name = declarator.IDENTIFIER().GetText();

                // Check Local Uniqueness (includes parameters)
                if (SymbolTable.CurrentScope.Symbols.ContainsKey(name))
                {
                    Errors.Add($"Error at line {line}: Variable '{name}' is already defined in this scope.");
                }
                else
                {
                    // Type compatibility check for initialization
                    if (declarator.expression() != null)
                    {
                        string? initType = GetExpressionType(declarator.expression());
                        if (initType != null && !IsTypeCompatible(type, initType))
                        {
                            Errors.Add($"Error at line {line}: Cannot initialize variable '{name}' of type '{type}' with value of type '{initType}'.");
                        }
                    }

                    var symbol = new Symbol
                    {
                        Name = name,
                        Type = type,
                        Category = SymbolType.LocalVariable,
                        Line = line,
                        IsConstant = isConstant
                    };
                    SymbolTable.Define(symbol);
                    
                    if (currentFunction != null)
                    {
                        currentFunction.LocalVariables.Add(symbol);
                    }
                }
            }

            return null;
        }

        public override object VisitBlock([NotNull] BallLangParser.BlockContext context)
        {
            bool newScope = context.Parent is not BallLangParser.FunctionDeclarationContext;
            
            if (newScope) SymbolTable.EnterScope();
            base.VisitBlock(context);
            if (newScope) SymbolTable.ExitScope();

            return null;
        }

        // Control Structures
        public override object VisitWhistleStatement([NotNull] BallLangParser.WhistleStatementContext context)
        {
            if (currentFunction != null)
            {
                currentFunction.ControlStructures.Add(("if", context.Start.Line));
            }
            return base.VisitWhistleStatement(context);
        }

        public override object VisitMatchdayStatement([NotNull] BallLangParser.MatchdayStatementContext context)
        {
            if (currentFunction != null)
            {
                currentFunction.ControlStructures.Add(("for", context.Start.Line));
            }
            
            SymbolTable.EnterScope();
            base.VisitMatchdayStatement(context);
            SymbolTable.ExitScope();
            
            return null;
        }

        public override object VisitInPlayStatement([NotNull] BallLangParser.InPlayStatementContext context)
        {
            if (currentFunction != null)
            {
                currentFunction.ControlStructures.Add(("while", context.Start.Line));
            }
            return base.VisitInPlayStatement(context);
        }

        // Return Statement
        public override object VisitFullTimeStatement([NotNull] BallLangParser.FullTimeStatementContext context)
        {
            if (currentFunction != null)
            {
                currentFunction.HasReturn = true;
                
                // Check return type compatibility
                if (context.expression() != null)
                {
                    string? returnExprType = GetExpressionType(context.expression());
                    if (returnExprType != null && currentFunction.ReturnType != "tactic")
                    {
                        if (!IsTypeCompatible(currentFunction.ReturnType!, returnExprType))
                        {
                            Errors.Add($"Error at line {context.Start.Line}: Return type mismatch. Function '{currentFunction.Name}' expects '{currentFunction.ReturnType}' but got '{returnExprType}'.");
                        }
                    }
                }
                else if (currentFunction.ReturnType != "tactic")
                {
                    Errors.Add($"Error at line {context.Start.Line}: Function '{currentFunction.Name}' must return a value of type '{currentFunction.ReturnType}'.");
                }
            }
            
            return base.VisitFullTimeStatement(context);
        }

        // Assignment Validation
        public override object VisitAssignment([NotNull] BallLangParser.AssignmentContext context)
        {
            string name = context.IDENTIFIER().GetText();
            var symbol = SymbolTable.Resolve(name);

            if (symbol == null)
            {
                Errors.Add($"Error at line {context.Start.Line}: Variable '{name}' used before declaration.");
            }
            else
            {
                // Check if constant
                if (symbol.IsConstant)
                {
                    Errors.Add($"Error at line {context.Start.Line}: Cannot assign to constant '{name}'.");
                }
                
                // Type compatibility for assignment
                string? exprType = GetExpressionType(context.expression());
                if (exprType != null && symbol.Type != null && !IsTypeCompatible(symbol.Type, exprType))
                {
                    Errors.Add($"Error at line {context.Start.Line}: Cannot assign value of type '{exprType}' to variable '{name}' of type '{symbol.Type}'.");
                }
            }
            
            return base.VisitAssignment(context);
        }

        // Identifier Usage Check
        public override object VisitIdentifierExpr([NotNull] BallLangParser.IdentifierExprContext context)
        {
            if (context.IDENTIFIER() != null)
            {
                string name = context.IDENTIFIER().GetText();
                if (SymbolTable.Resolve(name) == null)
                {
                    Errors.Add($"Error at line {context.Start.Line}: Variable '{name}' used but not defined.");
                }
            }
            return base.VisitIdentifierExpr(context);
        }

        // Function Call Validation
        public override object VisitFunctionCallExpr([NotNull] BallLangParser.FunctionCallExprContext context)
        {
            string funcName = context.IDENTIFIER().GetText();
            int line = context.Start.Line;

            // Check if calling main
            if (funcName == "main")
            {
                Errors.Add($"Error at line {line}: Function 'main' cannot be called.");
            }

            // Track function calls for recursion detection
            if (currentFunction != null)
            {
                currentFunction.CallsFunctions.Add(funcName);
            }

            var symbol = SymbolTable.Resolve(funcName);
            
            if (symbol == null)
            {
                Errors.Add($"Error at line {line}: Function '{funcName}' is not defined.");
            }
            else if (symbol is FunctionSymbol func)
            {
                // Check argument count
                int expectedArgs = func.Parameters.Count;
                int actualArgs = context.arguments()?.expression()?.Length ?? 0;

                if (expectedArgs != actualArgs)
                {
                    Errors.Add($"Error at line {line}: Function '{funcName}' expects {expectedArgs} argument(s) but got {actualArgs}.");
                }
                else if (context.arguments() != null)
                {
                    // Check argument types
                    var expressions = context.arguments().expression();
                    for (int i = 0; i < expectedArgs; i++)
                    {
                        string? argType = GetExpressionType(expressions[i]);
                        string? paramType = func.Parameters[i].Type;
                        
                        if (argType != null && paramType != null && !IsTypeCompatible(paramType, argType))
                        {
                            Errors.Add($"Error at line {line}: Argument {i + 1} of function '{funcName}' expects type '{paramType}' but got '{argType}'.");
                        }
                    }
                }
            }
            else
            {
                Errors.Add($"Error at line {line}: '{funcName}' is not a function.");
            }

            return base.VisitFunctionCallExpr(context);
        }

        // Helper: Check type compatibility
        private bool IsTypeCompatible(string targetType, string sourceType)
        {
            if (targetType == sourceType) return true;
            
            if (TypeCompatibility.TryGetValue(targetType, out var compatibleTypes))
            {
                return compatibleTypes.Contains(sourceType);
            }
            
            return false;
        }

        // Helper: Get expression type
        private string? GetExpressionType(BallLangParser.ExpressionContext context)
        {
            return GetExpressionTypeFromLogicalOr(context.logicalOrExpression());
        }

        private string? GetExpressionTypeFromLogicalOr(BallLangParser.LogicalOrExpressionContext context)
        {
            if (context == null) return null;
            
            // If there's OR operation, result is bool (goal)
            if (context.logicalAndExpression().Length > 1)
                return "goal";
            
            return GetExpressionTypeFromLogicalAnd(context.logicalAndExpression(0));
        }

        private string? GetExpressionTypeFromLogicalAnd(BallLangParser.LogicalAndExpressionContext context)
        {
            if (context == null) return null;
            
            // If there's AND operation, result is bool (goal)
            if (context.equalityExpression().Length > 1)
                return "goal";
            
            return GetExpressionTypeFromEquality(context.equalityExpression(0));
        }

        private string? GetExpressionTypeFromEquality(BallLangParser.EqualityExpressionContext context)
        {
            if (context == null) return null;
            
            // If there's equality comparison, result is bool (goal)
            if (context.relationalExpression().Length > 1)
                return "goal";
            
            return GetExpressionTypeFromRelational(context.relationalExpression(0));
        }

        private string? GetExpressionTypeFromRelational(BallLangParser.RelationalExpressionContext context)
        {
            if (context == null) return null;
            
            // If there's relational comparison, result is bool (goal)
            if (context.additiveExpression().Length > 1)
                return "goal";
            
            return GetExpressionTypeFromAdditive(context.additiveExpression(0));
        }

        private string? GetExpressionTypeFromAdditive(BallLangParser.AdditiveExpressionContext context)
        {
            if (context == null) return null;
            
            // For numeric operations, determine result type
            var types = context.multiplicativeExpression()
                .Select(GetExpressionTypeFromMultiplicative)
                .Where(t => t != null)
                .ToList();
            
            if (types.Count == 0) return null;
            
            // Return the "widest" type
            if (types.Contains("score")) return "score";
            if (types.Contains("salary")) return "salary";
            if (types.Contains("player")) return "player";
            
            return types.FirstOrDefault();
        }

        private string? GetExpressionTypeFromMultiplicative(BallLangParser.MultiplicativeExpressionContext context)
        {
            if (context == null) return null;
            
            var types = context.unaryExpression()
                .Select(GetExpressionTypeFromUnary)
                .Where(t => t != null)
                .ToList();
            
            if (types.Count == 0) return null;
            
            if (types.Contains("score")) return "score";
            if (types.Contains("salary")) return "salary";
            if (types.Contains("player")) return "player";
            
            return types.FirstOrDefault();
        }

        private string? GetExpressionTypeFromUnary(BallLangParser.UnaryExpressionContext context)
        {
            if (context == null) return null;
            
            if (context.NOT() != null) return "goal";
            
            if (context.postfixExpression() != null)
                return GetExpressionTypeFromPostfix(context.postfixExpression());
            
            if (context.unaryExpression() != null)
                return GetExpressionTypeFromUnary(context.unaryExpression());
            
            return null;
        }

        private string? GetExpressionTypeFromPostfix(BallLangParser.PostfixExpressionContext context)
        {
            if (context == null) return null;
            return GetExpressionTypeFromPrimary(context.primaryExpression());
        }

        private string? GetExpressionTypeFromPrimary(BallLangParser.PrimaryExpressionContext context)
        {
            if (context == null) return null;
            
            if (context is BallLangParser.IdentifierExprContext idExpr)
            {
                var symbol = SymbolTable.Resolve(idExpr.IDENTIFIER().GetText());
                return symbol?.Type;
            }
            
            if (context is BallLangParser.LiteralExprContext litExpr)
            {
                return GetLiteralType(litExpr.literal());
            }
            
            if (context is BallLangParser.FunctionCallExprContext funcExpr)
            {
                var symbol = SymbolTable.Resolve(funcExpr.IDENTIFIER().GetText());
                if (symbol is FunctionSymbol func)
                    return func.ReturnType;
            }
            
            if (context is BallLangParser.ParenExprContext parenExpr)
            {
                return GetExpressionType(parenExpr.expression());
            }
            
            return null;
        }

        private string? GetLiteralType(BallLangParser.LiteralContext context)
        {
            if (context is BallLangParser.IntegerLiteralContext) return "player";
            if (context is BallLangParser.FloatLiteralContext) return "salary";
            if (context is BallLangParser.DoubleLiteralContext) return "score";
            if (context is BallLangParser.StringLiteralContext) return "team";
            if (context is BallLangParser.TrueLiteralContext) return "goal";
            if (context is BallLangParser.FalseLiteralContext) return "goal";
            
            return null;
        }
    }
}
