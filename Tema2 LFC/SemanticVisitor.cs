using Antlr4.Runtime.Misc;
using System.Collections.Generic;
using System.IO;


namespace Tema2_LFC
{
    public class SemanticVisitor : BallLangBaseVisitor<object>
    {
        public SymbolTable SymbolTable { get; } = new SymbolTable();
        public List<string> Errors { get; } = new List<string>();
        
        private FunctionSymbol? currentFunction = null;

        public override object VisitProgram([NotNull] BallLangParser.ProgramContext context)
        {
            return base.VisitProgram(context);
        }

        public override object VisitGlobalDeclaration([NotNull] BallLangParser.GlobalDeclarationContext context)
        {
            string type = context.type().GetText();
            string name = context.IDENTIFIER().GetText();
            int line = context.Start.Line;

            // Check Uniqueness
            if (SymbolTable.Resolve(name) != null)
            {
                Errors.Add($"Error at line {line}: Global variable '{name}' is already defined.");
            }
            else
            {
                object initValue = null;
                if (context.expression() != null)
                {
                    initValue = Visit(context.expression()); // Evaluate init if possible, or just type check
                    // TODO: Type compatibility check for init
                }

                var symbol = new Symbol
                {
                    Name = name,
                    Type = type,
                    Category = SymbolType.GlobalVariable,
                    Line = line,
                    Value = initValue // Store init value as string or object representation
                };
                SymbolTable.Define(symbol);
            }

            return null;
        }

        public override object VisitFunctionDeclaration([NotNull] BallLangParser.FunctionDeclarationContext context)
        {
            string returnType = context.type().GetText();
            string name = context.IDENTIFIER().GetText();
            int line = context.Start.Line;

            if (SymbolTable.Resolve(name) != null)
            {
                Errors.Add($"Error at line {line}: Function '{name}' is already defined.");
                return null; // Skip body to avoid cascading errors? Or continue?
            }

            var funcSymbol = new FunctionSymbol
            {
                Name = name,
                ReturnType = returnType,
                Category = SymbolType.Function,
                Line = line
            };
            SymbolTable.Define(funcSymbol);

            currentFunction = funcSymbol; // Set context
            SymbolTable.EnterScope(); // Function Scope

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

            SymbolTable.ExitScope();
            currentFunction = null;
            return null;
        }

        public override object VisitVariableDeclaration([NotNull] BallLangParser.VariableDeclarationContext context)
        {
            string type = context.type().GetText();
            string name = context.IDENTIFIER().GetText();
            int line = context.Start.Line;

            // Check Local Uniqueness
            if (SymbolTable.CurrentScope.Symbols.ContainsKey(name))
            {
                Errors.Add($"Error at line {line}: Variable '{name}' is already defined in this scope.");
            }
            else
            {
                var symbol = new Symbol
                {
                    Name = name,
                    Type = type,
                    Category = SymbolType.LocalVariable,
                    Line = line
                };
                SymbolTable.Define(symbol);
                
                // Add to current function's local vars list for reporting
                if (currentFunction != null)
                {
                    currentFunction.LocalVariables.Add(symbol);
                }
                
                // TODO: Init type check
                if (context.expression() != null)
                {
                     // Type verification would go here
                }
            }

            return null;
        }

        public override object VisitBlock([NotNull] BallLangParser.BlockContext context)
        {
            // If checking strict nested scopes for variables?
            // Usually function body block overlaps with function scope, 
            // but internal blocks (if/while) need new scopes.
            // For now, let's assume just one scope push for function is handled by VisitFunctionDeclaration
            // if this is a standalone block (like in if), we should push scope.
            
            bool newScope = context.Parent is not BallLangParser.FunctionDeclarationContext;
            
            if (newScope) SymbolTable.EnterScope();
            base.VisitBlock(context);
            if (newScope) SymbolTable.ExitScope();

            return null;
        }

        // Control Structures logic for reporting
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
            
            // For loop often has its own scope for loop variable
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
                if (symbol.Type == "ball") // ball is const in comments
                {
                     Errors.Add($"Error at line {context.Start.Line}: Cannot assign to constant '{name}'.");
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
    }
}
