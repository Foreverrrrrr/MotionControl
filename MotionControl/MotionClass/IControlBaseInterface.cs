using System;

namespace MotionControl.MotionClass
{
    interface IControlBaseInterface
    {
        /// <summary>
        /// 数字io输入
        /// </summary>
        bool[] IO_Intput { get; set; }

        /// <summary>
        /// 数字io输出
        /// </summary>
        bool[] IO_Output { get; set; }

        /// <summary>
        /// 板卡号
        /// </summary>
        ushort[] Card_Number { get; set; }

        /// <summary>
        /// 轴号
        /// </summary>
        ushort[] AxisNo { get; set; }

        /// <summary>
        /// 打开板卡
        /// </summary>
        /// <param name="Card_Number">板卡号</param>
        /// <returns></returns>
        bool OpenCard(ushort[] Card_Number);
       
      
    }
}
