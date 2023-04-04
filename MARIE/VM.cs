namespace MARIE
{
    delegate void clock();

    internal class VM
    {
        class CPU
        {
            static int _core_id = 0;
            static int _ins_len = 1; // unit in address
            public int Core_id { get; }
            ALU aLU;
            MEM m;
            int[] ips;
            int ip { get { return ips[0]; } }
            public CPU(MEM m)
            {
                Core_id = _core_id++;
                aLU = new(this);
                this.m = m;
            }

            int PC = 0;
            int AC = 0;
            int MAR = 0;
            int MBR = 0;
            int IR = 0;

            class ALU
            {

                CPU CPU;
                public ALU(CPU cpu)
                {
                    CPU = cpu;
                }

                public delegate void MicroCodeOp(CPU c);

                MicroCodeOp curOp;

                public void Tick()
                {
                    if (curOp == null) throw new Exception("null operation in ALU");
                    SetOp(ASMLib.asm_to_str[CPU.IR]);
                    curOp(this.CPU);
                }

                Dictionary<string, MicroCodeOp> ops = new()
                {
                    { "ADD", (c) => { c.AC += c.ip; } },
                    { "SUBT", (c) => { c.AC -= c.ip; } },
                    { "ADDL", (c) => { c.AC += c.m[c.ip]; } },
                    { "CLEAR", (c) => { c.AC = 0; } },
                    { "LOAD", (c) => { c.AC = c.m[c.ip]; } },
                    { "STORE", (c) => { c.m[c.ip] = c.AC; } },
                    { "INPUT", (c) => { throw new NotImplementedException(); } },
                    { "OUTPUT", (c) => { throw new NotImplementedException(); } },
                    { "JUMP", (c) => { c.PC = c.ip; } },
                    {
                        "SKIPCOND",
                        (c) =>
                        {
                            if (c.ip == 0x000)
                            {
                                if (c.AC < 0) { c.PC += _ins_len; }
                            }
                            else if (c.ip == 0x400)
                            {
                                if (c.AC == 0) { c.PC += _ins_len; }
                            }
                            else if (c.ip == 0x800)
                            {
                                if (c.AC > 0) { c.PC += _ins_len; }
                            }
                            else throw new Exception("Invalid C");
                        }
                    },
                    { "JNS", (c) => { c.m[c.ip] = c.PC; c.PC = c.m[c.ip] + _ins_len; } },
                    { "JUMPL", (c) => { c.PC = c.m[c.ip]; } },
                    { "STOREL", (c) => { c.m[c.m[c.ip]] = c.AC; } },
                    { "LOADL", (c) => { c.AC = c.m[c.m[c.ip]]; } },
                    { "HALT", (c) => { throw new HaltException(); } },
                };
                
                void SetOp(string opName)
                {
                    if (ops.ContainsKey(opName))
                        this.curOp = ops[opName];
                    throw new Exception("Op Not Exist");
                }

                public class HaltException : Exception { }
            }

            public void Tick()
            {
                //fetch
                int ins = m[PC];
                PC++;
                //decode

                //execute

            }
        }
        class MEM
        {
            private static readonly int _Lower_ = 0;
            private static readonly int _Higher_ = 2048;
            private static readonly int _LO_MEM_SIZE_ = 1024;
            int[] _lo_mem = new int[_LO_MEM_SIZE_];
            Dictionary<int, int> _hi_mem = new();
            public int this[int index]
            {
                get
                {
                    if (index < _Lower_ || index >= _Higher_)
                    {
                        throw new IndexOutOfRangeException();
                    }
                    else if (index >= _LO_MEM_SIZE_)
                    {
                        if (!_hi_mem.ContainsKey(index)) { throw new IndexOutOfRangeException(); }
                        return _hi_mem[index];
                    }
                    else
                    {
                        return _lo_mem[index];
                    }
                }
                set
                {
                    if (index < _Lower_ || index >= _Higher_)
                    {
                        throw new IndexOutOfRangeException();
                    }
                    else if (index >= _LO_MEM_SIZE_)
                    {
                        if (!_hi_mem.ContainsKey(index)) { _hi_mem.Add(index, value); }
                        else _hi_mem[index] = value;
                    }
                    else
                    {
                        _lo_mem[index] = value;
                    }
                }
            }
        }
    }

    static class ASMLib
    {
        public static Dictionary<string, int> str_to_asm = new()
        {
            { "ADD", 0x3 },// Adds value in AC at address X into AC, AC ← AC + X
            { "SUBT", 0x4 },// Subtracts value in AC at address X into AC, AC ← AC - X
            { "ADDL", 0xB },//Add Indirect: Use the value at X as the actual address
                            //of the data operand to add to AC
            { "CLEAR", 0xA },//AC ← 0
            { "LOAD", 0x1 },//Loads Contents of Address X into AC
            { "STORE", 0x2 },//Stores Contents of AC into Address X
            { "INPUT", 0x5 },//Request user to input a value
            { "OUTPUT", 0x6 },//Prints value from AC
            { "JUMP", 0x9 },//Jumps to Address X
            { "SKIPCOND", 0x8 },//Skips the next instruction based on C: if (C) is        
                                //- 000: Skips if AC < 0
                                //- 400: Skips if AC = 0
                                //- 800: Skips if AC > 0
            { "JNS", 0x0 },//Jumps and Store: Stores PC at address X and jumps to X+1
            { "JUMPL", 0xC },//Uses the value at X as the address to jump to
            { "STOREL", 0xE },//Stores value in AC at the indirect address.
                              //e.g.StoreI addresspointer
                              //Gets value from addresspointer,
                              //stores the AC value into the address
            { "LOADL", 0xD },//Loads value from indirect address into AC
                             //e.g.LoadI addresspointer
                             //Gets address value from addresspointer,
                             //loads value at the address into AC
            { "HALT", 0x7 }, //End the program
        };
        public static Dictionary<int, string> asm_to_str = Reverse(str_to_asm);

        public static Dictionary<V, K> Reverse<K, V>(IDictionary<K, V> dict)
            where V : notnull
            where K : notnull
        {
            var inverseDict = new Dictionary<V, K>();
            foreach (var kvp in dict)
            {
                if (!inverseDict.ContainsKey(kvp.Value))
                {
                    inverseDict.Add(kvp.Value, kvp.Key);
                }
            }
            return inverseDict;
        }
    }
}
