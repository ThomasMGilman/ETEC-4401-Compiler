public static class GrammarData {
    public static string data = @"ADDOP -> [+]
AND -> \band\b
CLOSE -> \bclose\b
CMA -> ,
COMMENT -> //[^\n]*
DEF -> \bdef\b
ELSE -> \belse\b
EQ -> =(?!=)
IF -> \bif\b
INPUT -> \binput\b
LB -> \[
LBR -> [{]
LP -> [(]
MINUS -> -
MULOP -> [*/]
NOT -> \bnot\b
NUM -> -?(\d+|\d+\.\d*|\.\d+)([Ee][-+]?\d+)?
NUMBER -> \bnumber\b
OPEN -> \bopen\b
OR -> \bor\b
PRINT -> \bprint\b
RB -> \]
RBR -> [}]
READ -> \bread\b
RP -> [)]
RELOP -> >=|<=|!=|==|>|<
RETURN -> \breturn\b
SEMI -> ;
STRING -> \bstring\b
STRING-CONSTANT -> ""(\\""|[^""])*""|'(\\'|[^'])*'
VAR -> \bvar\b
WHILE -> \bwhile\b
WRITE -> \bwrite\b
ID -> [A-Za-z_]\w*


program -> var-decl-list braceblock
braceblock -> LBR var-decl-list stmts RBR
var-decl-list -> var-decl SEMI var-decl-list | lambda
var-decl -> VAR ID type
type -> non-array-type | non-array-type LB RB
non-array-type -> NUMBER | STRING
stmts -> stmt stmts | lambda
stmt -> cond | loop | return-stmt SEMI | assign SEMI | func-call SEMI
func-call -> builtin-func-call
builtin-func-call -> PRINT LP expr RP | INPUT LP RP | OPEN LP expr RP | READ LP expr RP | WRITE LP expr CMA expr RP | CLOSE LP expr RP
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
factor -> NUM | LP expr RP | STRING-CONSTANT | ID | func-call";
}
