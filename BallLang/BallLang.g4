grammar BallLang;

@lexer::namespace { BallLang }
@parser::namespace { BallLang }

// Lexer rules

WS: [ \t\r\n]+ -> skip;
LINE_COMMENT: '//' ~[\r\n]* -> skip;
BLOCK_COMMENT: '/*' .*? '*/' -> skip;

// Keywords

PLAYER: 'player'; // int
SCORE: 'score'; // double
SALARY: 'salary'; // float
TEAM: 'team'; // string 
BALL: 'ball'; // const
GOAL: 'goal'; // bool
TACTIC: 'tactic'; // void 
WHISTLE: 'whistle'; // if
VAR: 'var'; // else
MATCHDAY: 'matchday'; // for
IN_PLAY: 'in_play'; // while
FULL_TIME: 'full_time'; // return

// Operatori aritmetici (standard)

PLUS: '+';
MINUS: '-';
MULT: '*';
DIV: '/';
MOD: '%';

// Operatori relationali (standard)

LT: '<';
GT: '>';
LE: '<=';
GE: '>=';
EQ: '==';
NE: '!=';

// Operatori logici (standard)

AND: '&&';
OR: '||';
NOT: '!';

// Operatori de atribuire (standard)

ASSIGN: '=';
PLUS_ASSIGN: '+=';
MINUS_ASSIGN: '-=';
MULT_ASSIGN: '*=';
DIV_ASSIGN: '/=';
MOD_ASSIGN: '%=';

// Incrementare/decrementare (standard)

INC: '++';
DEC: '--';

// Delimitatori (standard)

LPAREN: '(';
RPAREN: ')';
LBRACE: '{';
RBRACE: '}';
COMMA: ',';
SEMI: ';';



// Constante numerice si literale 
// Numerice
INTEGER: [0-9]+;
FLOAT: [0-9]+ '.' [0-9]* | '.' [0-9]+;
DOUBLE: [0-9]+ '.' [0-9]* | '.' [0-9]+; // similar cu FLOAT dar pentru SCORE

// String literale
STRING_LITERAL: '"' (~["\\\r\n] | '\\' .)* '"';

// Valori booleene (pentru GOAL)
// Valori booleene (pentru GOAL) - MOVED UP BEFORE IDENTIFIER
TRUE: 'true';
FALSE: 'false';

// Identificatori 
IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;




// ===================================================
// PARSER RULES
// ===================================================

// Program principal
program: (globalDeclaration | functionDeclaration)* EOF;

// Declaratii globale 
globalDeclaration: 
    (BALL)? type varDeclarator (COMMA varDeclarator)* SEMI;

// Declarator pentru o singura variabila (nume optional cu initializare)
varDeclarator:
    IDENTIFIER (ASSIGN expression)?;

// Tipuri de date
type: 
    PLAYER    #playerType
    | SCORE   #scoreType
    | SALARY  #salaryType
    | TEAM    #teamType
    | GOAL    #goalType
    | TACTIC  #tacticType;

// Declaratii de functii 
functionDeclaration: 
    type IDENTIFIER LPAREN parameters? RPAREN block;

parameters: parameter (COMMA parameter)*;
parameter: type IDENTIFIER;

// Bloc de cod
block: LBRACE statement* RBRACE;

// Instructiuni
statement: 
      variableDeclaration    #varDeclStatement
    | assignment SEMI        #assignStatement
    | whistleStatement       #whistleStmt
    | matchdayStatement      #matchdayStmt
    | inPlayStatement        #inPlayStmt
    | fullTimeStatement SEMI #fullTimeStmt
    | expression SEMI        #exprStatement
    | block                  #blockStatement;

// Declaratie variabila (locala sau globala)
variableDeclaration: 
    (BALL)? type varDeclarator (COMMA varDeclarator)* SEMI;

// Atribuire
assignment: 
    IDENTIFIER assignmentOperator expression;

assignmentOperator: 
      ASSIGN 
    | PLUS_ASSIGN 
    | MINUS_ASSIGN 
    | MULT_ASSIGN 
    | DIV_ASSIGN 
    | MOD_ASSIGN;

// Structuri de control 

// WHISTLE = if
whistleStatement: 
    WHISTLE LPAREN expression RPAREN statement (VAR statement)?;

// MATCHDAY = for
    matchdayStatement: 
    MATCHDAY LPAREN 
    (variableDeclaration | assignment | SEMI)  // init
    expression? SEMI                           // condition
    (expression | assignment)?                 // increment
    RPAREN statement;

// IN_PLAY = while
inPlayStatement: 
    IN_PLAY LPAREN expression RPAREN statement;

// FULL_TIME = return
fullTimeStatement: 
    FULL_TIME expression?;

// Expresii 

// Expresii cu atribuire (de la dreapta la stanga)
expression: 
      logicalOrExpression
    | expression ASSIGN logicalOrExpression
    ;

// OR logic
logicalOrExpression: 
      logicalAndExpression (OR logicalAndExpression)*
    ;

// AND logic
logicalAndExpression: 
      equalityExpression (AND equalityExpression)*
    ;

// Egalitate
equalityExpression: 
      relationalExpression ((EQ | NE) relationalExpression)*
    ;

// Relationale
relationalExpression: 
      additiveExpression ((LT | GT | LE | GE) additiveExpression)*
    ;

// Aditive
additiveExpression: 
      multiplicativeExpression ((PLUS | MINUS) multiplicativeExpression)*
    ;

// Multiplicative
multiplicativeExpression: 
      unaryExpression ((MULT | DIV | MOD) unaryExpression)*
    ;

// Unare (includem ++ si -- aici)
unaryExpression: 
      (PLUS | MINUS | NOT | INC | DEC) unaryExpression
    | postfixExpression
    ;

// Postfix (pentru a++ si a--)
postfixExpression: 
      primaryExpression (INC | DEC)?
    ;

// Expresii primare
primaryExpression: 
      IDENTIFIER                              #identifierExpr
    | literal                                 #literalExpr
    | IDENTIFIER LPAREN arguments? RPAREN     #functionCallExpr
    | LPAREN expression RPAREN                #parenExpr
    ;

// Argumente la apel de functie
arguments: expression (COMMA expression)*;

// Literale (pentru constante)
literal: 
      INTEGER         #integerLiteral
    | FLOAT           #floatLiteral
    | DOUBLE          #doubleLiteral
    | STRING_LITERAL  #stringLiteral
    | TRUE            #trueLiteral
    | FALSE           #falseLiteral
    ;