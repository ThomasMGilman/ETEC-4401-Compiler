extern fopen
extern fclose
extern fscanf
extern fprintf
extern scanf
extern printf
extern fflush
default rel
section .text
global main
main:
call theRealMain
movq xmm0, rax
cvtsd2si rax,xmm0
ret
theRealMain:
push rbp
mov rbp, rsp
mov rax, __float64__(456.00)
push rax
pop rdx
mov rcx, pctg
movq xmm0, rdx
mov rax, 1
mov rbx, rsp
and rsp, -16
sub rsp, 32
call printf
add rsp, 32
mov rsp, rbx
mov rcx, 0
mov rbx, rsp
and rsp, -16
sub rsp, 32
call fflush
add rsp, 32
mov rsp, rbx
mov rax, __float64__(123.00)
push rax
pop rax
mov rsp, rbp
pop rbp
ret
section .data
fopenRplus:
db "r+", 0
fopenA:
db "a",0
pcts:
db "%s", 0
pctg:
db "%g", 0
scanbuffer:
db 0
pctlf:
db "%lf",0
msg:
db 0
fmt:
db "%s", 10, 0