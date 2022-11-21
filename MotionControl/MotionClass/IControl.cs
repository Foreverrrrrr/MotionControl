namespace MotionControl.MotionClass
{
    interface IControlBaseInterface
    {
        bool[] Io_Intput { get; set; }

        bool[] Io_Output { get; set; }

        ushort[] Card_Number { get; set; }

        ushort[] AxisNo { get; set; }

        
    }
}
