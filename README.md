# BallLang Compiler

## Overview
BallLang is a football-themed programming language designed to demonstrate the core principles of compiler construction, including lexical analysis, syntactic parsing, and semantic validation. This project implements a compiler for BallLang using ANTLR4 and C#, providing detailed reports on the program's structure and any identified errors.

The language maps traditional programming concepts to football terminology, creating a unique and consistent metaphorical environment for code.

## Language Specification

### Data Types and Keywords
BallLang uses a specialized vocabulary to represent standard programming constructs:

- **player**: Integer type (`int`)
- **score**: Double precision floating-point type (`double`)
- **salary**: Single precision floating-point type (`float`)
- **team**: String type (`string`)
- **ball**: Constant modifier (`const`)
- **goal**: Boolean type (`bool`)
- **tactic**: Void type (`void`)

### Control Structures
- **whistle**: Conditional branch (`if`)
- **var**: Alternative branch (`else`)
- **matchday**: Iterative loop (`for`)
- **in_play**: Conditional loop (`while`)
- **full_time**: Return statement (`return`)

## Technical Architecture

### 1. Lexical Analysis
The lexer identifies the basic building blocks of the language, such as keywords, identifiers, literals, and operators. It filters out whitespace and comments while maintaining track of line numbers for error reporting.

### 2. Syntactic Parsing
The parser verifies that the sequence of tokens conforms to the BallLang grammar rules defined in `BallLang.g4`. It constructs a Parse Tree that represents the hierarchical structure of the source code.

### 3. Semantic Analysis
The `SemanticVisitor` performs deep inspection of the program to ensure logic consistency:
- **Symbol Table Management**: Tracks global and local variables, functions, and parameters within their respective scopes.
- **Type Checking**: Validates that operations are performed on compatible types and that assignments respect type constraints.
- **Function Validation**: Ensures functions are defined before use, checks argument counts/types, and verifies return statements.
- **Recursion Detection**: Identifies whether functions are recursive or iterative based on their call graph.

## Output Files

The compiler generates several reports during execution to provide insights into the compilation process.

### errors.txt
Contains a comprehensive list of all Lexical, Syntactic, and Semantic errors.
- **General**: Provides detailed messages including line numbers and the nature of the violation. If the program is correct, it confirms no errors were found.
- **Current Example**: Reports "No lexical, syntactic, or semantic errors found" for the provided test file.

### tokens.txt
A log of every token identified by the lexer.
- **General**: Lists tokens in the format `<TYPE, 'value', line_number>`.
- **Current Example**: Shows tokens for `player`, `globalCounter`, `=`, `0`, etc., mapping them to their respective grammar rules.

### global_variables.txt
A summary of variables declared in the global scope.
- **General**: Lists the name, type, initialization status, and line number for each global entry.
- **Current Example**: 
  - `globalCounter` (Type: `player`, Line: 4)
  - `MAX_VALUE` (Type: `player`, Constant, Line: 5)

### functions.txt
A detailed report on all functions defined in the source code.
- **General**: Includes return type, parameters, local variables (with line numbers), and a list of internal control structures (`if`, `for`, `while`). It also labels functions as "recursive" or "iterative" and identifies the "main" entry point.
- **Current Example**:
  - `add`: Iterative, helper function with 2 parameters.
  - `fib`: Recursive, identifies the recursive calls and the `whistle` (if) structure.
  - `factorial`: Iterative, tracks the `matchday` (for) loop and local variables `result` and `i`.
  - `main`: The entry point, containing various local variables and control structures like `whistle` and `in_play`.

## Project Components
- **BallLang.g4**: The ANTLR4 grammar specification.
- **SemanticVisitor.cs**: The core logic for semantic validation and report generation.
- **SymbolTable.cs**: Implementation of scoped symbol management.
- **Program.cs**: The compiler driver that orchestrates the stages of compilation.
- **test.ball**: A sample program demonstrating recursion, loops, and global/local scope.

## Sample Program (test.ball)

Below is the example code used to demonstrate the compiler's capabilities:

```ball
// BALL = const, PLAYER = int, SCORE = double, SALARY = float, TEAM = string, GOAL = bool

// Global variables
player globalCounter = 0;
ball player MAX_VALUE = 100;

// Helper function
player add(player a, player b) {
    full_time a + b;
}

// Recursive fibonacci
player fib(player n) {
    whistle(n <= 1) {
        full_time n;
    }
    full_time fib(n - 1) + fib(n - 2);
}

// Iterative factorial
player factorial(player n) {
    player result = 1;
    matchday(player i = 1; i <= n; i++) {
        result = result * i;
    }
    full_time result;
}

// Main function
tactic main() {
    player x = 5, y = 10, sum;
    sum = add(x, y);
    
    player fibResult = fib(6);
    player factResult = factorial(5);
    
    whistle(sum > 10) {
        globalCounter = globalCounter + 1;
    } var {
        globalCounter = 0;
    }
    
    in_play(globalCounter < MAX_VALUE) {
        globalCounter = globalCounter + 1;
    }
}
```

## Requirements
- .NET SDK (Compatible with the provided solution)
- ANTLR4 Runtime
