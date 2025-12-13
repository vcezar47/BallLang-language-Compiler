grammar BallLang;

@lexer::namespace { BallLangLexer }
@parser::namespace { BallLangParser }

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