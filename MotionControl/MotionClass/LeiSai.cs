using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionControl.MotionClass
{
    internal class LeiSai : MotionBase
    {
        public override bool[] IO_Intput { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override bool[] IO_Output { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ushort[] Card_Number { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override ushort[] AxisNo { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override bool OpenCard(ushort[] Card_Number)
        {
            throw new NotImplementedException();
        }
    }
}
