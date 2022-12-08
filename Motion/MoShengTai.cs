using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotionControl
{
    public sealed class MoShengTai : MotionBase
    {
        /// <summary>
        /// 连接站号类型
        /// </summary>
        public enum ModelType : ushort
        {
            /// <summary>
            /// 40/32/16点IO模块
            /// </summary>
            NIO2416_161R_0808R,
            /// <summary>
            /// 80/64点IO模块
            /// </summary>
            NIO4832_3232,
            /// <summary>
            /// 4/4/4/2/1轴网络总线运动控制卡
            /// </summary>
            NMC3400_3401_1400_1200_1100R,
            /// <summary>
            /// 6/8轴网络总线控制卡
            /// </summary>
            NMC5800_5600_1800_1600R,
            /// <summary>
            /// 12/16轴网络总线运动控制卡
            /// </summary>
            NMC5160_5120,
            /// <summary>
            /// 网络运动控制激光卡
            /// </summary>
            LMC3400R_LMC3100R,
            /// <summary>
            /// 暂未开放
            /// </summary>
            EIO0840
        }
        /// <summary>
        /// 总线轴总数
        /// </summary>
        public override int Axisquantity { get; set; } = -1;

        public override bool[] IO_Input { get; set; }
        public override bool[] IO_Output { get; set; }
        public override ushort[] Card_Number { get; set; }
        public override ushort[] Axis { get; set; }
        public override int[] EtherCATStates { get; set; }
        public override double[][] AxisStates { get; set; }
        public override Thread Read_t1 { get; set; }
        public override ManualResetEvent AutoReadEvent { get; set; }
        public override ushort FactorValue { get; set; }
        public override double Speed { get; set; }
        public override double Acc { get; set; }
        public override double Dec { get; set; }
        public override int[][] Axis_IO { get; set; }
        public override short[] CoordinateSystemStates { get; set; }
        /// <summary>
        /// 板卡是否打开
        /// </summary>
        public override bool IsOpenCard { get; set; }

        public override event Action<DateTime, string> CardLogEvent;

        public override void AwaitIOinput(ushort card, ushort indexes, bool waitvalue, int timeout = 0)
        {
            throw new NotImplementedException();
        }

        public override void AwaitMoveAbs(ushort axis, double position, double speed, int time = 0)
        {
            throw new NotImplementedException();
        }

        public override void AwaitMoveHome(ushort axis, ushort home_model, double home_speed, int timeout = 0, double acc = 0.5, double dcc = 0.5, double offpos = 0)
        {
            throw new NotImplementedException();
        }

        public override void AwaitMoveRel(ushort axis, double position, double speed, int time = 0)
        {
            throw new NotImplementedException();
        }

        public override void AxisBasicSet(ushort axis, double equiv, double startvel, double speed, double acc, double dec, double stopvel, double s_para, int posi_mode, int stop_mode)
        {
            throw new NotImplementedException();
        }

        public override void AxisErrorReset(ushort axis)
        {
            throw new NotImplementedException();
        }

        public override void AxisOff(ushort card, ushort axis)
        {
            throw new NotImplementedException();
        }

        public override void AxisOff()
        {
            throw new NotImplementedException();
        }

        public override void AxisOn(ushort card, ushort axis)
        {
            throw new NotImplementedException();
        }

        public override void AxisOn()
        {
            if (IsOpenCard)
            {
                for (int i = 0; i < Axis.Length; i++)
                {

                    if (CardErrorMessage(CMCDLL_NET.MCF_Set_Servo_Enable_Net((ushort)i, 1, 0)))
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{i}轴上使能！");
                    }
                }
            }
        }

        public override void AxisStop(ushort axis, int stop_mode, bool all)
        {
            throw new NotImplementedException();
        }

        public override void CloseCard()
        {
            throw new NotImplementedException();
        }

        public override bool[] Getall_IOinput(ushort card)
        {
            throw new NotImplementedException();
        }

        public override bool[] Getall_IOoutput(ushort card)
        {
            throw new NotImplementedException();
        }

        public override int[] GetAxisExternalio(ushort axis)
        {
            throw new NotImplementedException();
        }

        public override double[] GetAxisState(ushort axis)
        {
            throw new NotImplementedException();
        }

        public override int[] GetEtherCATState(ushort card_number)
        {
            throw new NotImplementedException();
        }

        public override void MoveAbs(ushort axis, double position, double speed, int time = 0)
        {
            throw new NotImplementedException();
        }

        public override void MoveCircle_Center(ushort card, ControlState t, int time = 0)
        {
            throw new NotImplementedException();
        }

        public override void MoveHome(ushort axis, ushort home_model, double home_speed, int timeout = 0, double acc = 0.5, double dcc = 0.5, double offpos = 0)
        {
            throw new NotImplementedException();
        }

        public override void MoveJog(ushort axis, double speed, int posi_mode, double acc = 0.5, double dec = 0.5)
        {
            throw new NotImplementedException();
        }

        public override void MoveLines(ushort card, ControlState t, int time = 0)
        {
            throw new NotImplementedException();
        }

        public override void MoveRel(ushort axis, double position, double speed, int time = 0)
        {
            throw new NotImplementedException();
        }

        public override void MoveReset(ushort axis)
        {
            throw new NotImplementedException();
        }

        public override bool OpenCard(ushort card_number)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 打开板卡
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">板卡型号数组参数异常</exception>
        public override bool OpenCard()
        {
            bool isopen = false;
            if (Card_Number != null && Axisquantity != -1)
            {
                Axis = new ushort[Axisquantity];
                ushort[] Station_Number = { 0, 1, 2, 3, 4, 5, 6, 7 };
                var refs = CMCDLL_NET.MCF_Open_Net((ushort)Card_Number.Length, ref Station_Number[0], ref Card_Number[0]);
                if (refs != 0)
                {
                    IsOpenCard = true;
                    isopen = false;
                    CardErrorMessage(refs);
                }
                else
                {
                    IsOpenCard = false;
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, "打开板卡成功!");
                    isopen = true;
                }
            }
            else
            {
                if (Card_Number == null)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, "未设置板卡型号！在OpenCard前请先在Card_Number数组中设置板卡型号");
                    throw new Exception("未设置板卡型号！在OpenCard前请先在Card_Number数组中设置板卡型号");
                }
                else if (Axis == null)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, "未设置轴数量！在OpenCard前请先在Axis数组中设置轴数量");
                    throw new Exception("未设置轴数量！在OpenCard前请先在Axis数组中设置轴数量");
                }

            }
            return isopen;
        }

        public override void ResetCard(ushort card, ushort reset)
        {
            throw new NotImplementedException();
        }

        public override void SetAxis_iniFile()
        {
            throw new NotImplementedException();
        }

        public override void SetbjectDictionary(ushort card, ushort etherCATLocation, ushort primeindex, ushort wordindexing, ushort bitlength, int value)
        {
            throw new NotImplementedException();
        }

        public override void SetEtherCAT_eniFiel()
        {
            throw new NotImplementedException();
        }

        public override void SetExternalTrigger(ushort card, ushort start, ushort reset, ushort stop, ushort estop)
        {
            throw new NotImplementedException();
        }

        public override void Set_IOoutput(ushort card, ushort indexes, bool value)
        {
            throw new NotImplementedException();
        }
    }
}
