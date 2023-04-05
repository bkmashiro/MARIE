using MARIE;
VM.MEM mem = new();
VM.CPU cpu = new(mem);
VM vm = new(cpu, mem);
MARIEAssembly MARIE = new();
MARIE.runOn(cpu);

const int beg_pos = 1;

ASMComposer cp = new(mem, beg_pos);
cp.var("n",     50);
cp.var("x",     0);
cp.var("sum",   0);
cp.var("inc",   1);
cp.var("p_base", 200);
cp.var("p_top", 200);
cp.var("param1", 114);
cp.var("ret", -1);
cp.var("ret_shift", 11);
cp.Label("#1");
cp.emit(OpCodes.LOAD, "$n");
cp.emit(OpCodes.SUBT, "$x");
cp.emit(OpCodes.SKIPCOND, 0x800);
cp.emit(OpCodes.JUMP, "#2");
cp.emit(OpCodes.LOAD, "$sum");
cp.emit(OpCodes.ADD, "$x");
cp.emit(OpCodes.STORE, "$sum");
cp.emit(OpCodes.LOAD, "$x");
cp.emit(OpCodes.ADD, "$inc");
cp.emit(OpCodes.STORE, "$x");
cp.emit(OpCodes.JUMP, "#1");
cp.Label("#2");
cp.emit(OpCodes.OUTPUT, "$sum");

//begin to call .foo
cp.emit(OpCodes.LOADR, (int)RegisterNames.PC);   // load PC to AC
cp.emit(OpCodes.ADD, "$ret_shift");
cp.emit(OpCodes.STOREL, "$p_top");          //save AC to stack top (p_top is top pointer)

cp.emit(OpCodes.LOAD, "$p_top");    //
cp.emit(OpCodes.ADD, "$inc");       //++top
cp.emit(OpCodes.STORE, "$p_top");   //

//push param1 to stack top
cp.emit(OpCodes.LOAD, "$param1");  //load param1 to AC
cp.emit(OpCodes.STOREL, "$p_top"); //load ac to stack top

cp.emit(OpCodes.LOAD, "$p_top");    //
cp.emit(OpCodes.ADD, "$inc");       //++top
cp.emit(OpCodes.STORE, "$p_top");   //

cp.emit(OpCodes.JUMP, "#.foo");     //call .foo

cp.emit(OpCodes.OUTPUT, "$ret");

cp.emit(OpCodes.HALT);

cp.Label("#.foo");
//get param1
cp.emit(OpCodes.LOAD, "$p_top");    // stack.pop
cp.emit(OpCodes.SUBT, "$inc");      // --top
cp.emit(OpCodes.STORE, "$p_top");   //
//body ac += param1
cp.emit(OpCodes.LOADL, "$p_top");
cp.emit(OpCodes.ADDL, "$p_top");    // AC += *stk_top
//save return value
cp.emit(OpCodes.STORE, "$ret");     // store ac to ret
//pop
cp.emit(OpCodes.LOAD, "$p_top");    // stack.pop
cp.emit(OpCodes.SUBT, "$inc");      // --top
cp.emit(OpCodes.STORE, "$p_top");   //
//return
cp.emit(OpCodes.JUMPL, "$p_top");   //load return addr to ac

var asm_code = cp.GetMemCode();

var mem_code = MARIE.Assemble(asm_code);

mem.Set(mem_code, cp.beg_ins);
vm.DEBUG.UseSymbolMap(cp.GetSymbolMap());

vm.RunAt(cp.beg_ins);


////var v = MARIE.GetRTL(
////    "LOAD 1",
////    "ADD 2"
////);
//mem[105] = 1;  //increasement
//mem[106] = 0;  //SUM
//mem[107] = 0;  //X
//mem[108] = 50; //N

//var mem_code = MARIE.Assemble(
//    "LOAD 108",     //3
//    "SUBT 107",
//    "SKIPCOND 0x800",
//    "JUMP 14",      //if X>N, break
//    "LOAD 106",     //add sum
//    "ADD 107",
//    "STORE 106",    //save sum
//    "LOAD 107",
//    "ADD 105",
//    "STORE 107",    //++X
//    "JUMP 3",
//    "OUTPUT 106",
//    "HALT"
//);