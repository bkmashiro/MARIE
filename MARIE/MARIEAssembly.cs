
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

        class microInstruction
        {
            public microInstruction(string s)
            {
                //if is assignment:  xx <- yy
                if (s.Contains("<-"))
                {
                    string[] lr = s.Split("<-");
                    var l = lr[0];
                    var r = lr[1];
                }

            }
        }
        static class MicroCodeObjectFactory
        {
            //M[MBR]
            IMicroCodeObject GetMicroCodeObjectByStr(string s)
            {
                //if has params
                if (s.Contains('['))
                {
                    if (s[0]=='M') //is memory
                    {
                        string param = s.Split('[');
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
                        default:
                            break;
                    }
                }
                return null;
            }
        }

        public interface IMicroCodeObject
        {
            int GetValue() { return 0; }
            void SetValue(Dictionary<string, int>? env = null) { }
        }

        class MCO : IMicroCodeObject
        {
            public MCO(string s)
            {

            }
        }

        class ASM
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

        class JNS : ASM
        {
            public JNS(int X)
            {
                init(
                    "MBR<-PC",
                    "MAR<-X",
                    "M[MBR]<-MBR",
                    "MBR<-X",
                    "AC<-1",
                    "AC<-AC + MBR",
                    "PC<-AC"
                );
            }

        }

        string assemble(string asmc)
        {
            return "";
        }
    }
}


namespace Registers
{
    class IR : IMicroCodeObject
    {

    }
    class PC : IMicroCodeObject
    {

    }
    class MBR : IMicroCodeObject
    {

    }
    class MAR : IMicroCodeObject
    {

    }
    class M : IMicroCodeObject//memory
    {
        public M(string val) { }
    }
}