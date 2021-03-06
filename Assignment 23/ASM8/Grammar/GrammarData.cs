public static class GrammarData {
    public static string data = @"ADDOP -> [+]
AND -> \band\b
CLOSE -> \bclose\b
CMA -> ,
COMMENT -> //[^\n]*
DEF -> \bdef\b
DOT -> \.
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
STRUCT -> \bstruct\b
VAR -> \bvar\b
WHILE -> \bwhile\b
WRITE -> \bwrite\b
ID -> [A-Za-z_]\w*

program -> struct-decl-list var-decl-list func-decl-list
struct-decl-list -> struct-decl struct-decl-list | lambda
struct-decl -> STRUCT ID LBR struct-member-decl-list RBR
struct-member-decl-list -> struct-member struct-member-decl-list | struct-member
struct-member -> ID type SEMI 
func-decl-list -> func-decl func-decl-list | lambda
func-decl -> DEF ID LP optional-type-list RP optional-return-spec braceblock
optional-return-spec -> RETURN type | lambda
optional-type-list -> type-list | lambda
type-list -> ID type type-list'
type-list' -> CMA type-list | lambda
func-call -> ID LP optional-expr-list RP | builtin-func-call
builtin-func-call -> PRINT LP expr RP | INPUT LP RP | OPEN LP expr RP | READ LP expr RP | WRITE LP expr CMA expr RP | CLOSE LP expr RP
optional-expr-list -> expr-list | lambda
expr-list -> expr expr-list'
expr-list' -> CMA expr-list | lambda
braceblock -> LBR var-decl-list stmts RBR
var-decl-list -> var-decl SEMI var-decl-list | lambda
var-decl -> VAR ID type
type -> non-array-type | non-array-type LB num-list RB
num-list -> NUM | NUM CMA num-list
non-array-type -> NUMBER | STRING | STRUCT ID
stmts -> stmt stmts | lambda
stmt -> cond | loop | return-stmt SEMI | assign SEMI | func-call SEMI
assign -> ID EQ expr | ID LB expr-list RB EQ expr | struct-member-access EQ expr
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
factor -> NUM | LP expr RP | STRING-CONSTANT | ID | func-call | array-access | struct-member-access
array-access -> ID LB expr-list RB
struct-member-access -> ID DOT struct-member-access | ID DOT ID";
}
