using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tema2_LFC;


namespace BallLangCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // 1. Setup Input
                string input = File.ReadAllText("test.ball");
                var inputStream = new AntlrInputStream(input);

                // 2. Lexical Analysis
                var lexer = new BallLangLexer(inputStream);
                
                // Custom Error Listener for Lexical Errors
                var errorListener = new FileErrorListener();
                lexer.RemoveErrorListeners();
                lexer.AddErrorListener(errorListener);

                var tokenStream = new CommonTokenStream(lexer);
                tokenStream.Fill();

                // Requirement II.2: Save tokens to file
                var tokenList = tokenStream.GetTokens();
                var tokenOutput = new StringBuilder();
                foreach (var token in tokenList)
                {
                    if (token.Type != TokenConstants.EOF)
                    {
                        string name = lexer.Vocabulary.GetSymbolicName(token.Type);
                        string text = token.Text.Replace("\r", "").Replace("\n", "\\n");
                        tokenOutput.AppendLine($"<{name}, '{text}', {token.Line}>");
                    }
                }
                File.WriteAllText("tokens.txt", tokenOutput.ToString());
                Console.WriteLine("Generated tokens.txt");

                // 3. Parsing
                var parser = new BallLangParser(tokenStream);
                
                // Custom Error Listener for Syntactic Errors
                parser.RemoveErrorListeners();
                parser.AddErrorListener(errorListener);

                var tree = parser.program();

                // 4. Semantic Analysis & Data Collection
                var visitor = new SemanticVisitor();
                visitor.Visit(tree);

                // Requirement III.3.a: Global Variables Report
                var globalVarOutput = new StringBuilder();
                globalVarOutput.AppendLine("Global Variables:");
                foreach (var symbol in visitor.SymbolTable.GetGlobalVariables())
                {
                    globalVarOutput.AppendLine($"Name: {symbol.Name}, Type: {symbol.Type}, Initialized: {symbol.Value ?? "null"}, Line: {symbol.Line}");
                }
                File.WriteAllText("global_variables.txt", globalVarOutput.ToString());
                Console.WriteLine("Generated global_variables.txt");

                // Requirement III.3.b: Functions Report
                var funcOutput = new StringBuilder();
                funcOutput.AppendLine("Functions:");
                foreach (var func in visitor.SymbolTable.GetFunctions())
                {
                    funcOutput.AppendLine($"Function: {func.Name}");
                    string funcType = func.IsRecursive ? "recursive" : "iterative";
                    string mainType = func.IsMain ? "main" : "non-main";
                    funcOutput.AppendLine($"  Type: {funcType}, {mainType}");
                    funcOutput.AppendLine($"  Return Type: {func.ReturnType}");
                    funcOutput.AppendLine($"  Parameters: {string.Join(", ", func.Parameters.Select(p => $"{p.Type} {p.Name}"))}");
                    
                    funcOutput.AppendLine("  Local Variables:");
                    if (func.LocalVariables.Any())
                    {
                        foreach (var local in func.LocalVariables)
                        {
                            funcOutput.AppendLine($"    {local.Type} {local.Name} (Line {local.Line})");
                        }
                    }
                    else
                    {
                        funcOutput.AppendLine("    (None)");
                    }

                    funcOutput.AppendLine("  Control Structures:");
                    if (func.ControlStructures.Any())
                    {
                        foreach (var cs in func.ControlStructures)
                        {
                            funcOutput.AppendLine($"    {cs.Type} at line {cs.Line}");
                        }
                    }
                    else
                    {
                        funcOutput.AppendLine("    (None)");
                    }
                    funcOutput.AppendLine(new string('-', 20));
                }
                File.WriteAllText("functions.txt", funcOutput.ToString());
                Console.WriteLine("Generated functions.txt");

                // Requirement III.4: Errors Report
                // Combine Lexical/Syntactic errors with Semantic errors
                var allErrors = errorListener.Errors.Concat(visitor.Errors).ToList();
                var errorOutput = new StringBuilder();
                
                if (allErrors.Any())
                {
                    Console.WriteLine("Errors found:");
                    foreach (var error in allErrors)
                    {
                        errorOutput.AppendLine(error);
                        Console.WriteLine(error);
                    }
                }
                else
                {
                    errorOutput.AppendLine("No lexical, syntactic, or semantic errors found.");
                    Console.WriteLine("No errors found.");
                }
                File.WriteAllText("errors.txt", errorOutput.ToString());
                Console.WriteLine("Generated errors.txt");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }

    public class FileErrorListener : BaseErrorListener
    {
        public List<string> Errors { get; } = new List<string>();

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            string errorType = (recognizer is BallLangLexer) ? "Lexical Error" : "Syntactic Error";
            Errors.Add($"{errorType} at line {line}:{charPositionInLine} - {msg}");
        }
    }
}