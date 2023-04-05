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
cp.Label("#1");
cp.emit(OpCodes.LOAD,   "$n");  
cp.emit(OpCodes.SUBT,   "$x");
cp.emit(OpCodes.SKIPCOND, 0x800);
cp.emit(OpCodes.JUMP,   "#2");
cp.emit(OpCodes.LOAD,   "$sum");
cp.emit(OpCodes.ADD,    "$x");
cp.emit(OpCodes.STORE,  "$sum");
cp.emit(OpCodes.LOAD,   "$x");
cp.emit(OpCodes.ADD,    "$inc");
cp.emit(OpCodes.STORE,  "$x");
cp.emit(OpCodes.JUMP,   "#1");
cp.Label("#2");
cp.emit(OpCodes.OUTPUT, "$sum");
cp.emit(OpCodes.HALT);

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