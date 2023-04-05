using System.Diagnostics;
using System.Text;

namespace MARIE
{
    delegate void clock();

    internal class VM
    {
        public VM(CPU c, MEM m)
        {
            this.cpu = c;
            this.mem = m;
            DEBUG = new _DEBUG_(c, m);
        }
        public CPU cpu;
        public MEM mem;
        public _DEBUG_ DEBUG;
        public class CPU
        {
            public int CPU_Cycles = 0;
            static int _core_id = 0;
            static int _ins_len = 1; // unit in address
            public int Core_id { get; }
            ALU aLU;
            public MEM m;
            int[] ips = new int[16];
            int ip { get { return ips[0]; } }
            public CPU(MEM m)
            {
                Core_id = _core_id++;
                aLU = new(this);
                this.m = m;
            }

            public int PC = 0;
            public int AC = 0;
            public int MAR = 0;
            public int MBR = 0;
            public int IR = 0;
            public int INPUT = 0;

            public class ALU
            {

                public CPU CPU;
                public ALU(CPU cpu)
                {
                    CPU = cpu;
                }

                public delegate void MicroCodeOp(CPU c);

                MicroCodeOp? curOp;

                public void Tick()
                {
                    int ins = (CPU.IR >> 28) & 0b1111;
                    SetOp(ASMLib.asm_to_str[ins]);
                    int param = CPU.IR & 0b0000_1111_1111_1111_1111_1111_1111_1111;
                    CPU.ips[0] = param;
                    if (curOp == null) throw new Exception("null operation in ALU");
                    curOp(this.CPU);
                }

                Dictionary<string, MicroCodeOp> QuickOps = new()
                {
                    { "ADD", (c) => {
                        c.AC += c.m[c.ip];
                    } },
                    { "SUBT", (c) => {
                        c.AC -= c.m[c.ip];
                    } },
                    { "ADDL", (c) => {
                        c.AC += c.m[c.m[c.ip]]; //TODO
                    } },
                    { "CLEAR", (c) => { c.AC = 0; } },
                    { "LOAD", (c) => {
                        c.AC = c.m[c.ip]; } },
                    { "STORE", (c) => {
                        c.m[c.ip] = c.AC;
                    } },
                    { "INPUT", (c) => {
                        Console.WriteLine("Please input one integer (32bit signed)");
                        string usr_input = Console.ReadLine() ?? "";
                        if (int.TryParse(usr_input, out int i))
                        {
                            c.INPUT = i;
                        }else throw new FormatException("number invalid");
                    } },
                    { "OUTPUT", (c) => {  Console.WriteLine(">>> " + c.m[c.ip]); } },
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
                    { "JUMPL", (c) => { 
                        c.PC = c.m[c.m[c.ip]];
                    } },
                    { "STOREL", (c) => { 
                        c.m[c.m[c.ip]] = c.AC;
                    } },
                    { "LOADL", (c) => { c.AC = c.m[c.m[c.ip]]; } },
                    { "HALT", (c) => {
                        throw new HaltException();
                    } },
                    {"LOADR",(c) => {
                        switch (c.ip)
                            {
                            case (int)RegisterNames.MBR:
                                c.AC = c.MBR;
                                break;

                            case (int)RegisterNames.MAR:
                                c.AC = c.MAR;
                                break;

                            case (int)RegisterNames.IR:
                                c.AC = c.IR;
                                break;

                            case (int)RegisterNames.PC:
                                c.AC = c.PC;
                                break;
                                 
                            default: throw new Exception("Unrecognized register name");
                            }
                    } }
                };

                public void SetOp(string opName)
                {
                    if (QuickOps.ContainsKey(opName))
                        this.curOp = QuickOps[opName];
                    else throw new Exception("Op Not Exist");
                }

                public class HaltException : Exception { }
            }

            public void Tick()
            {
                //fetch
                aLU.CPU.IR = m[PC];
                PC++;
                //decode
                //execute
                Console.WriteLine("(*)" + aLU.CPU);
                aLU.Tick();
                Console.WriteLine("(-)" + aLU.CPU);
                CPU_Cycles++;
            }

            public override string ToString()
            {
                string ins_name = ASMLib.asm_to_str[(IR >> 28) & 0b1111];
                string ins_param = Map_val_to_name(IR & 0b0000_1111_1111_1111_1111_1111_1111_1111);
                return $"PC@{PC:d4}\tAC={AC}\t\tIR=0x{IR:x8} [{ins_name} {ins_param}]";
            }
            Dictionary<int, string> symbol_map;
            private string Map_val_to_name(int addr)
            {
                if (symbol_map != null && symbol_map.ContainsKey(addr))
                {
                    return symbol_map[addr];
                }
                return "0x" + addr.ToString("x4");
            }
            public void UseSymbolMap(Dictionary<int, string> map)
            {
                symbol_map = map;
            }
        }
        public class MEM
        {
            private static readonly int _Lower_ = 0;
            private static readonly int _Higher_ = 131072;
            private static readonly int _LO_MEM_SIZE_ = 65536;
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

            public void Set(IEnumerable<int> vs, int beg)
            {
                foreach (var val in vs)
                {
                    this[beg] = val;
                    beg++;
                }
            }
        }
        public void RunAt(int pc_idx)
        {
            if (pc_idx < 0) throw new Exception("Program Counter out of range");
            this.cpu.PC = pc_idx;
            Run();
        }

        Stopwatch sw;
        public void Run()
        {
            sw = new();
            sw.Start();
            try
            {
                while (true)
                {
                    cpu.Tick();
                }
            }
            catch (CPU.ALU.HaltException)
            {
                // stop by halt
                sw.Stop();
                PrintStatics();
            }
        }

        void PrintStatics()
        {
            Console.WriteLine($"\n\nCPU Cycles: {cpu.CPU_Cycles}\n" +
                              $"Time Elapsed {sw.ElapsedMilliseconds}ms");
        }
        public void mount(List<MARIEAssembly.ASM> asms, int mem_idx)
        {
            List<MARIEAssembly.RTL> li = new();
            foreach (var item in asms)
            {
                li.AddRange(item.GetInstructions());
            }
        }

        public class _DEBUG_
        {
            CPU cpu;
            MEM mem;
            public _DEBUG_(CPU c, MEM m)
            {
                cpu = c;
                mem = m;
            }
            public void UseSymbolMap(Dictionary<int, string> map)
            {
                this.cpu.UseSymbolMap(map);
            }
        }
    }

    static class ASMLib
    {
        public static Dictionary<string, int> str_to_asm = new()
        {
            { "JNS", 0x0 },//Jumps and Store: Stores PC at address X and jumps to X+1
            { "LOAD", 0x1 },//Loads Contents of Address X into AC
            { "STORE", 0x2 },//Stores Contents of AC into Address X
            { "ADD", 0x3 },// Adds value in AC at address X into AC, AC ← AC + X
            { "SUBT", 0x4 },// Subtracts value in AC at address X into AC, AC ← AC - X
            { "INPUT", 0x5 },//Request user to input a value
            { "OUTPUT", 0x6 },//Prints value from AC
            { "HALT", 0x7 }, //End the program
            { "SKIPCOND", 0x8 },//Skips the next instruction based on C: if (C) is        
                                //- 000: Skips if AC < 0
                                //- 400: Skips if AC = 0
                                //- 800: Skips if AC > 0
            { "JUMP", 0x9 },//Jumps to Address X
            { "CLEAR", 0xA },//AC ← 0
            { "ADDL", 0xB },//Add Indirect: Use the value at X as the actual address
                            //of the data operand to add to AC
            { "JUMPL", 0xC },//Uses the value at X as the address to jump to
            { "LOADL", 0xD },//Loads value from indirect address into AC
                             //e.g.LoadI addresspointer
                             //Gets address value from addresspointer,
                             //loads value at the address into AC
            { "STOREL", 0xE },//Stores value in AC at the indirect address.
                              //e.g.StoreI addresspointer
                              //Gets value from addresspointer,
                              //stores the AC value into the address
            { "LOADR", 0xF }, //Load register value.
        };
        public static Dictionary<int, string> asm_to_str = Reverse(str_to_asm);

        public static Dictionary<V, K> Reverse<K, V>(this IDictionary<K, V> dict)
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

    static class Template
    {
        public static string MEM_X_ADD_MEM_Y_TO_MEM_Z(int x, int y, int z)
        {
            return $"LOAD {x}\n" +
                   $"ADD {y}\n" +
                   $"STORE {z}\n";
        }
        /*while(x<y){}
         #1:    LOAD Y
                SUBT X
                JUMP(#2)
                #body#
                JUMPL(#1)
         #2:
         */

        /*for(i=1 to N)
         #1:    X.val = 1
                LOAD N
                SUBT X
                SKIPCOND (#2)
                #body#
                LOAD X
                ADDL 1
                JUMPL(#1)
         #2:
         */
    }

    public enum OpCodes
    {
        Label,
        LOAD,
        STORE,
        ADD,
        ADDL,
        SKIPCOND,
        JUMP,
        HALT,
        OUTPUT,
        SUBT,
        STOREL,
        LOADL,
        LOADR,
        JUMPL,
    }

    public enum RegisterNames
    {
        MAR,
        MBR,
        AC,
        IR,
        PC
    }

    class ASMComposer
    {
        VM.MEM mem;
        public ASMComposer(VM.MEM mem, int beg = 0) { this.mem = mem; beg_addr = beg; cur_addr = beg_addr; }
        int beg_addr = 0;
        int cur_addr = 0;
        int var_addr_span = 0;
        Dictionary<string, int> symbol_map = new();
        Dictionary<int, string> abs_symbol_map = new();
        Dictionary<string, int> vars = new();
        List<(OpCodes, object[])> vs = new();
        public int beg_ins = -1;
        int emit_idx = 0;
        public void emit(OpCodes opc, params object[] args) //generate one 32-bit instruction
        {
            vs.Add((opc, args));
            ++emit_idx;
        }
        public void InitVars()
        {
            foreach (var v in vars)
            {
                symbol_map.Add(v.Key, var_addr_span);
                cur_addr++;
                var_addr_span++;
            }
            int p = beg_addr;
            foreach (var v in vars) // warning: make sure vars is ordered in two foreaches
            {
                mem[p] = v.Value;
                ++p;
            }
        }

        public string GetMemCode()
        {
            InitVars();
            StringBuilder sb = new();
            beg_ins = cur_addr;
            foreach (var item in vs)
            {
                (OpCodes o, object[] args) = item;
                if (args.Length == 0)
                {
                    sb.Append($"{GetEnumNameByKey((int)o)}\n");
                }
                else
                {
                    int rel_shift = 0;
                    if (args[0].ToString().StartsWith("#"))
                    {
                        rel_shift = symbol_map[args[0].ToString()];
                        rel_shift += var_addr_span + 1;
                        sb.Append($"{GetEnumNameByKey((int)o)} {rel_shift}\n");
                        abs_symbol_map[rel_shift] = args[0].ToString();
                        continue;
                    }
                    if (args[0].ToString().StartsWith("$"))
                    {
                        rel_shift = symbol_map[args[0].ToString().Substring(1)];
                        rel_shift += beg_addr;
                        sb.Append($"{GetEnumNameByKey((int)o)} {rel_shift}\n");
                        abs_symbol_map[rel_shift] = args[0].ToString();
                        continue;
                    }
                    sb.Append($"{GetEnumNameByKey((int)o)} {args[0]}\n");//immediate
                }
                cur_addr++;
            }
            return sb.ToString().Trim();
        }
        /// <summary>
        /// Make label for the last instruction
        /// </summary>
        /// <param name="lab"></param>
        /// <returns></returns>
        public _Label Label(string lab)
        {
            symbol_map.Add(lab, emit_idx);
            return new(cur_addr);
        }
        public void var(string name, int v)
        {
            vars.Add(name, v);
        }
        public class _Label
        {
            public int addr;
            public _Label(int addr) { this.addr = addr; }
        }

        public string GetEnumNameByKey(int key)
        {
            return Enum.GetName(typeof(OpCodes), key);
        }

        public Dictionary<int, string> GetSymbolMap()
        {
            return abs_symbol_map;
        }
    }
}
