using System.Text.RegularExpressions;
using static MARIE.MARIEAssembly;

namespace MARIE
{
    class MARIEAssembly
    {
        Dictionary<string, int> assembly_code = new()
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
        VM.CPU? CPU;
        VM.MEM? MEM;
        //AC MAR MBR
        public void runOn(VM.CPU cpu) { this.CPU = cpu; if (cpu.m != null) MEM = cpu.m; }
        public class RTL
        {
            IMicroCodeObject left, right;
            public RTL(string s)
            {
                //if is assignment:  xx <- yy
                if (s.Contains("<-"))
                {
                    string[] lr = s.Split("<-");
                    var l = lr[0];
                    var r = lr[1];
                    left = MicroCodeObjectFactory.GetMicroCodeObjectByStr(l);
                    right = MicroCodeObjectFactory.GetMicroCodeObjectByStr(r);
                    return;
                }
                throw new Exception("Invalid assignment");
            }
            public override string ToString()
            {
                return $"{left}<-{right}";
            }

            public void Exec()
            {
                left.SetValue(right.GetValue());
            }
        }
        class MicroCodeObjectFactory
        {
            static MARIE.VM.CPU CPU;
            public static void UseCPU(VM.CPU cpu) { CPU = cpu; }
            public static IMicroCodeObject GetMicroCodeObjectByStr(string s)
            {
                IMicroCodeObject? ret = null;
                //if is a number
                if (int.TryParse(s, out int num))
                {
                    return new Registers.Immediate(num);
                }
                //if is comp (binary op)
                if (Regex.IsMatch(s, ".+[-,+,*,/,%,^].+")) // 匹配二元运算+-*/%^
                {
                    string[] param = s.Split('-', '+', '*', '/', '%', '^');
                    string op = Regex.Match(s, "[-,+,*,/,%,^]").Value;
                    return new Registers.BinaryCompositonal(
                        GetMicroCodeObjectByStr(param[0]),
                        GetMicroCodeObjectByStr(param[1]),
                        op
                    );
                }

                //if has params
                if (s.Contains('['))
                {
                    if (s[0] == 'M') //is memory
                    {
                        string param = s.Split('[', ']')[1];
                        ret = new Registers.M(GetMicroCodeObjectByStr(param));
                    }
                }
                else
                {
                    //MAR MBR PC
                    switch (s)
                    {
                        case "MAR": ret = new Registers.MAR(); break;
                        case "MBR": ret = new Registers.MBR(); break;
                        case "IR": ret = new Registers.IR(); break;
                        case "PC": ret = new Registers.PC(); break;
                        case "AC": ret = new Registers.AC(); break;
                        default:
                            break;
                    }
                }
                if (ret != null)
                {
                    ret.BindCPU(CPU);
                    return ret;
                }

                throw new Exception("Unrecognized Register");
            }
        }

        public abstract class IMicroCodeObject
        {
            protected MARIE.VM.CPU CPU;
            public abstract int GetValue();
            public abstract void SetValue(int env);

            public void BindCPU(MARIE.VM.CPU cpu)
            {
                this.CPU = cpu;
            }
        }

        abstract class MCO : IMicroCodeObject
        {
            public MCO(string s)
            {

            }

            public override int GetValue()
            {
                throw new NotImplementedException();
            }

            public override void SetValue(int env)
            {
                throw new NotImplementedException();
            }
        }
        class ASMFactory
        {
            static VM.CPU CPU;
            public static void useCPU(VM.CPU cpu) { CPU = cpu; }
            public static ASM GetASM(string s)
            {
                string[] ss = s.Split(' ');
                string ins = ss[0], param = ss[1];
                MicroCodeObjectFactory.UseCPU(CPU);
                ASM ret;
                switch (ins)
                {
                    case "LOAD":
                        ret = new LOAD(param);
                        break;
                    case "ADDL":
                        ret = new ADDL(param);
                        break;
                    default:
                        throw new InvalidDataException();
                }
                return ret;
            }
        }
        public class ASM
        {
            List<RTL> RTLs = new();
            protected void init(params string[] s)
            {
                RTLs.Clear();
                foreach (var item in s)
                {
                    RTLs.Add(new(item));
                }
            }

            public List<RTL> GetInstructions() => RTLs;
        }

        class LOAD : ASM
        {
            public LOAD(string X)
            {
                init(
                    $"MAR<-{X}",
                    $"MBR<-M[MAR]",
                    $"AC<-MBR"
                );
            }
        }

        class ADDL : ASM
        {
            public ADDL(string X)
            {
                init(
                    $"MAR<-{X}",
                    $"MBR<-M[MAR]",
                    $"AC<-MBR+AC"
                );
            }
        }

        class SUBT : ASM
        {
            public SUBT(string X)
            {
                init(
                    $"MAR<-{X}",
                    $"MBR<-M[MAR]",
                    $"AC<-MBR-AC"
                );
            }
        }

        class JNS : ASM
        {
            public JNS(string X)
            {
                init(
                    $"MBR<-PC",
                    $"MAR<-{X}",
                    $"M[MBR]<-MBR",
                    $"MBR<-{X}",
                    $"AC<-1",
                    $"AC<-AC+MBR",
                    $"PC<-AC"
                );
            }

        }
        /*
            32-bit length
            Hi                                        Lo
            [0000]  [0000 0000 0000 0000 0000 0000 0000]
            op-code     params (address/immediate)
            4 bits      28 bits
         */
        public List<int> Assemble(string asmc)
        {
            List<int> ret = new();
            string[] mls = asmc.Split('\n');
            foreach (var item in mls)
            {
                int ins = 0;
                string[] args = item.Split(' ');
                if (args.Length == 1)
                {
                    ins = (ASMLib.str_to_asm[args[0]] << 28) | (0);
                }
                else if (args.Length == 2)
                {
                    int param;
                    if (args[1].StartsWith("0x")) param = Convert.ToInt32(args[1].Substring(2), 16);
                    else param = int.Parse(args[1]);
                    if ((param & (0b1111 << 24)) != 0) 
                        throw new Exception($"Param {args[1]} overflow! This may cause truncation!");

                    ins = (ASMLib.str_to_asm[args[0]] << 28) | (param);
                }
                else
                {
                    throw new Exception("Unspported ASM");
                }
                ret.Add(ins);
            }
            return ret;
        }
        public List<int> Assemble(params string[] asmcs)
        {
            string asmc = string.Join('\n', asmcs);
            return Assemble(asmc);
        }
        public List<ASM> GetRTL(params string[] asmcs)
        {
            string asmc = string.Join('\n', asmcs);
            return GetRTL(asmc);
        }

        public List<ASM> GetRTL(string asmc)
        {
            ASMFactory.useCPU(CPU);
            if (CPU == null) throw new Exception("CPU is undefined.");
            string[] asmc_lines = asmc.Split('\n');
            List<ASM> microInstructions = new();
            foreach (string asmc_line in asmc_lines)
            {
                microInstructions.Add(ASMFactory.GetASM(asmc_line));
            }
            return microInstructions;
        }
    }
}


namespace Registers
{
    class Immediate : IMicroCodeObject //立即数
    {

        int value = -1;
        public Immediate(int value)
        {
            this.value = value;
        }

        public override int GetValue()
        {
            return value;
        }

        public override void SetValue(int value)
        {
            throw new Exception("Invalid Operation: Assignment to an immediate");
        }

        public override string ToString()
        {
            return $"0x{this.GetValue():x4}";
        }
    }
    class IR : IMicroCodeObject
    {
        public override int GetValue()
        {
            return CPU.IR;
        }

        public override void SetValue(int value)
        {
            CPU.IR = value;
        }

        public override string ToString()
        {
            return $"IR({this.GetValue()})";
        }


    }
    class PC : IMicroCodeObject
    {
        public override int GetValue()
        {
            return CPU.PC;
        }

        public override void SetValue(int value)
        {
            CPU.PC = value;
        }

        public override string ToString()
        {
            return $"PC({GetValue()})";
        }
    }
    class MBR : IMicroCodeObject
    {
        public override int GetValue()
        {
            return CPU.MBR;
        }

        public override void SetValue(int value)
        {
            CPU.MBR = value;
        }

        public override string ToString()
        {
            return $"MBR({GetValue()})";
        }
    }
    class MAR : IMicroCodeObject
    {
        public override int GetValue()
        {
            return CPU.MAR;
        }

        public override void SetValue(int value)
        {
            CPU.MAR = value;
        }

        public override string ToString()
        {
            return $"MAR({GetValue()})";
        }
    }
    class M : IMicroCodeObject //memory
    {
        private IMicroCodeObject microCodeObject;

        public M(IMicroCodeObject proxy)
        {
            this.microCodeObject = proxy;
        }

        public override string ToString()
        {
            return $"MEM({GetValue()})";
        }

        public override int GetValue()
        {
            return CPU.m[microCodeObject.GetValue()];
        }

        public override void SetValue(int value)
        {
            CPU.m[microCodeObject.GetValue()] = value;
        }
    }
    class AC : IMicroCodeObject
    {
        public override int GetValue()
        {
            return CPU.AC;
        }

        public override void SetValue(int value)
        {
            CPU.AC = value;
        }

        public override string ToString()
        {
            return $"AC({GetValue()})";
        }
    }
    delegate int BinaryOperation(int l, int r);
    class BinaryCompositonal : IMicroCodeObject
    {
        static Dictionary<string, BinaryOperation> ops = new Dictionary<string, BinaryOperation>
        {
            {"+", (a,b)=> a+b },
            {"-", (a,b)=> a-b },
            {"*", (a,b)=> a*b },
            {"/", (a,b)=> a/b },
            {"%", (a,b)=> a%b },
            {"^", (a,b)=> (int)Math.Pow(a,b) },
        };
        public BinaryCompositonal(IMicroCodeObject l, IMicroCodeObject r, string op)
        {
            lhs = l;
            rhs = r;
            this.op = ops[op];
        }
        IMicroCodeObject lhs;
        BinaryOperation op;
        IMicroCodeObject rhs;
        public override int GetValue()
        {
            return op(lhs.GetValue(), rhs.GetValue());
        }

        public override void SetValue(int value)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"Comp";
        }
    }
}