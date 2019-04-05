public static class GrammarData {
    public static string data = @"ADDOP -> [+]
AND -> \band\b
CMA -> ,
COMMENT -> //[^\n]*
DEF -> \bdef\b
ELSE -> \belse\b
EQ -> =(?!=)
IF -> \bif\b
LB -> \[
LBR -> [{]
LP -> [(]
MINUS -> -
MULOP -> [*/]
NOT -> \bnot\b
NUM -> -?(\d+|\d+\.\d*|\.\d+)([Ee][-+]?\d+)?
NUMBER -> \bnumber\b
OR -> \bor\b
RB -> \]
RBR -> [}]
RP -> [)]
RELOP -> >=|<=|!=|==|>|<
RETURN -> \breturn\b
SEMI -> ;
STRING -> \bstring\b
STRING-CONSTANT -> ""(\\""|[^""])*""|'(\\'|[^'])*'
VAR -> \bvar\b
WHILE -> \bwhile\b
ID -> [A-Za-z_]\w*

program -> var-decl-list braceblock
braceblock -> LBR stmts RBR
var-decl-list -> var-decl SEMI var-decl-list | lambda
var-decl -> VAR ID type
type -> non-array-type | non-array-type LB NUM RB
non-array-type -> NUMBER | STRING
stmts -> stmt stmts | lambda
stmt -> cond | loop | return-stmt SEMI | assign SEMI
assign -> ID EQ expr
loop -> WHILE LP expr RP braceblock
cond -> IF LP expr RP braceblock | IF LP expr RP braceblock ELSE braceblock
return-stmt -> RETURN expr
expr -> orexp
orexp -> orexp OR andexp | andexp
andexp -> andexp AND notexp | notexp
notexp -> NOT notexp | rel
rel -> sum RELOP sum | sum
sum -> sum ADDOP term | sum MINUS term | term
term -> term MULOP neg | neg
neg -> MINUS neg | factor
factor -> NUM | LP expr RP | STRING-CONSTANT | ID";
}
