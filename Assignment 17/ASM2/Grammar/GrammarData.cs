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
STRING-CONSTANT -> ""(\\""|[^""])*"" | '(\\'|[^'])*'
VAR -> \bvar\b
WHILE -> \bwhile\b
ID -> [A-Za-z_]\w*

program -> braceblock
stmts -> stmt stmts | lambda
stmt -> cond | loop | return-stmt SEMI
loop -> WHILE LP expr RP braceblock
cond -> IF LP expr RP braceblock | IF LP expr RP braceblock ELSE braceblock
braceblock -> LBR stmts RBR
return-stmt -> RETURN expr
expr -> orexp
orexp -> orexp OR andexp | andexp
andexp -> andexp AND notexp | notexp
notexp -> NOT notexp | rel
rel -> sum RELOP sum | sum
sum -> sum ADDOP term | sum MINUS term | term
term -> term MULOP neg | neg
neg -> MINUS neg | factor
factor -> NUM | LP expr RP";
}
