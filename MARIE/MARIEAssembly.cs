
using System.ComponentModel.Design.Serialization;
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

        //AC MAR MBR

        public class microInstruction
        {
            IMicroCodeObject left, right;
            public microInstruction(string s)
            {
                //if is assignment:  xx <- yy
                if (s.Contains("<-"))
                {
                    string[] lr = s.Split("<-");
                    var l = lr[0];
                    var r = lr[1];
                    left = MicroCodeObjectFactory.GetMicroCodeObjectByStr(l);
                    right = MicroCodeObjectFactory.GetMicroCodeObjectByStr(r);
                }
            }
            public override string ToString()
            {
                return $"{left}<-{right}";
            }
        }
        class MicroCodeObjectFactory
        {
            //M[MBR]
            public static IMicroCodeObject GetMicroCodeObjectByStr(string s)
            {
                //if is a number
                if (int.TryParse(s, out int num))
                {
                    return new Registers.Immediate(num);
                }


                //if has params
                if (s.Contains('['))
                {
                    if (s[0]=='M') //is memory
                    {
                        string param = s.Split('[', ']')[1];
                        return new Registers.M(GetMicroCodeObjectByStr(param));
                    }
                }
                else
                {
                    //MAR MBR PC
                    switch (s)
                    {
                        case "MAR":return new Registers.MAR();
                        case "MBR":return new Registers.MBR();
                        case "IR":return new Registers.IR();
                        case "PC":return new Registers.PC();
                        case "AC":return new Registers.AC();
                        default:
                            break;
                    }
                }
                return null;
            }
        }

        public interface IMicroCodeObject
        {
            public int GetValue();
            public void SetValue(Dictionary<string, int>? env = null);
        }

        abstract class MCO : IMicroCodeObject
        {
            public MCO(string s)
            {

            }

            public int GetValue()
            {
                throw new NotImplementedException();
            }

            public void SetValue(Dictionary<string, int>? env = null)
            {
                throw new NotImplementedException();
            }
        }
        class ASMFactory
        {
            public static ASM GetASM(string s)
            {
                string[] ss = s.Split(' ');
                string ins = ss[0], param = ss[1];

                switch (ins)
                {
                    case "LOAD": return new LOAD(param);
                    default:
                        break;
                }

                throw new InvalidDataException();
            }
        }
        public class ASM
        {
            List<microInstruction> microInstructions = new();
            protected void init(params string[] s)
            {
                microInstructions.Clear();
                foreach (var item in s)
                {
                    microInstructions.Add(new(item));
                }
            }
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

        public List<ASM> Assemble(string asmc)
        {
            string[] asmc_lines = asmc.Split('\n');
            List<ASM> microInstructions = new();
            foreach (string asmc_line in asmc_lines)
            {
                microInstructions.Add(ASMFactory.GetASM(asmc));
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

        public int GetValue()
        {
            throw new NotImplementedException();
        }

        public void SetValue(Dictionary<string, int>? env = null)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"DEC{this.GetValue()}";
        }
    }
    class IR : IMicroCodeObject
    {
        int value = -1;

        public int GetValue()
        {
            throw new NotImplementedException();
        }

        public void SetValue(Dictionary<string, int>? env = null)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"IR({this.GetValue()})";
        }


    }
    class PC : IMicroCodeObject
    {
        int value = -1;

        public int GetValue()
        {
            return value;
        }

        public void SetValue(Dictionary<string, int>? env = null)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"PC({GetValue()})";
        }
    }
    class MBR : IMicroCodeObject
    {
        int value = -1;

        public int GetValue()
        {
            throw new NotImplementedException();
        }

        public void SetValue(Dictionary<string, int>? env = null)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"MBR({GetValue()})";
        }
    }
    class MAR : IMicroCodeObject
    {
        int value = -1;

        public int GetValue()
        {
            return value;
        }

        public void SetValue(Dictionary<string, int>? env = null)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"MAR({GetValue()})";
        }
    }
    class M : IMicroCodeObject//memory
    {
        private IMicroCodeObject microCodeObject;

        public M(IMicroCodeObject proxy) {
            this.microCodeObject = proxy;
        }

        int value = -1;
        public override string ToString()
        {
            return $"IR({GetValue()})";
        }

        public int GetValue()
        {
            return this.value;
        }

        public void SetValue(Dictionary<string, int>? env = null)
        {
            throw new NotImplementedException();
        }
    }

    class AC : IMicroCodeObject
    {

        int value = -1;

        public int GetValue()
        {
            return value;
        }

        public void SetValue(Dictionary<string, int>? env = null)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"AC({GetValue()})";
        }
    }
    class Compositonal : IMicroCodeObject
    {
        int value = -1;

        public int GetValue()
        {
            throw new NotImplementedException();
        }

        public void SetValue(Dictionary<string, int>? env = null)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"IR({value})";
        }
    }
}