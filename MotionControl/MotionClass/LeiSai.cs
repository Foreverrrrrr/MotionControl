using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace MotionControl.MotionClass
{
    internal sealed class LeiSai : MotionBase
    {
        /// <summary>
        /// 控制卡号1,轴映射
        /// </summary>
        public enum CardOne
        {
            X,
        }

        public override bool[] IO_Intput { get; set; }
        public override bool[] IO_Output { get; set; }
        public override short Card_Number { get; set; }
        public override ushort[] Axis { get; set; }
        public int Axisquantity { get; set; }

        public double Speed { get; set; }
        public double Acc { get; set; }
        public double Dec { get; set; }
        public override Thread Read_t1 { get; set; }

        public override event Action<DateTime, string> CardLogEvent;

        public override void AxisOn(ushort card, ushort axis)
        {
            _ = Axis == null ? throw new Exception("请先初始化板卡再使能轴！") : true;
            if (card < Card_Number)
            {
                if (axis < Axis.Length)
                {
                    LTDMC.nmc_set_axis_enable(card, axis);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{card}号板卡{axis}号轴上使能");
                }
                else
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, "指定轴号错误");
                    throw new Exception("指定轴号错误");
                }
            }
            else
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, "指定卡号错误");
                throw new Exception("指定卡号错误");
            }
        }

        public override void AxisOn()
        {
            _ = Axis == null ? throw new Exception("请先初始化板卡再使能轴！") : true;
            LTDMC.nmc_set_axis_enable(0, 255);
            if (CardLogEvent != null)
                CardLogEvent(DateTime.Now, $"所有轴上使能");
        }

        public override void AxisBasicSet(ushort axis, double equiv, double startvel, double speed, double acc, double dec, double stopvel, double s_para, int posi_mode, int stop_mode)
        {
            if (axis < Axis.Length)
            {
                if (axis < 8)
                {
                    Acc = acc;
                    Dec = dec;
                    Speed = speed;
                    CardErrorMessage(LTDMC.dmc_set_equiv(0, axis, equiv));  //设置脉冲当量
                    CardErrorMessage(LTDMC.dmc_set_profile_unit(0, axis, startvel, speed, acc, dec, stopvel));//设置速度参数
                    CardErrorMessage(LTDMC.dmc_set_s_profile(0, axis, 0, s_para));//设置S段速度参数
                    CardErrorMessage(LTDMC.dmc_stop(0, axis, (ushort)stop_mode));//制动方式
                    CardErrorMessage(LTDMC.dmc_set_dec_stop_time(0, axis, posi_mode)); //设置减速停止时间
                }
            }
        }

        public override bool OpenCard(ushort card_number)
        {
            Card_Number = LTDMC.dmc_board_init();
            if (Card_Number > 0)
            {
                uint totalaxis = 0;
                for (int i = 0; i < Card_Number; i++)
                {
                    CardErrorMessage(LTDMC.nmc_get_total_axes((ushort)i, ref totalaxis));
                    Axisquantity += (int)totalaxis;
                }
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"初始化{card_number}号板卡，轴数量为{Axisquantity}");
                return true;
            }
            else
            {
                if (Card_Number == 0)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, "未查找到板卡!");
                    throw new Exception("未查找到板卡!");
                }
                else
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, "有 2 张或 2 张以上控制卡的硬件设置卡号相同!");
                    throw new Exception("有 2 张或 2 张以上控制卡的硬件设置卡号相同!");
                }
            }
        }

        public override bool OpenCard()
        {
            Card_Number = LTDMC.dmc_board_init();
            if (Card_Number > 0)
            {
                uint totalaxis = 0;
                CardErrorMessage(LTDMC.nmc_get_total_axes(0, ref totalaxis));
                if (totalaxis == System.Enum.GetNames(typeof(CardOne)).Length)
                    Axisquantity = (int)totalaxis;
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"初始化{Card_Number}张板卡，总轴数为{Axisquantity}");
                Axis = new ushort[Axisquantity];
                Read_t1 = new Thread(Read);
                Read_t1.IsBackground = true;
                Read_t1.Start();
                return true;
            }
            else
            {
                if (Card_Number == 0)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, "未查找到板卡!");
                    throw new Exception("未查找到板卡!");
                }
                else
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, "有 2 张或 2 张以上控制卡的硬件设置卡号相同!");
                    throw new Exception("有 2 张或 2 张以上控制卡的硬件设置卡号相同!");
                }
            }
        }

        public override void MoveJog(ushort axis, double speed, int posi_mode, double acc = 0.1, double dec = 0.1)
        {
            if (axis < Axis.Length)
            {
                CardErrorMessage(LTDMC.dmc_set_profile_unit(0, axis, 0, speed, acc, dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_vmove(0, axis, (ushort)posi_mode));
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"轴{axis}进行JOG运动");
            }
        }

        public override void AxisStop(ushort axis, int stop_mode = 0, bool all = false)
        {
            if (axis < Axis.Length)
            {
                if (!all)
                    CardErrorMessage(LTDMC.dmc_stop(0, axis, (ushort)stop_mode));
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}轴停止！");
                else
                {
                    for (int i = 0; i < Axis.Length; i++)
                    {
                        CardErrorMessage(LTDMC.dmc_stop(0, (ushort)i, (ushort)stop_mode));
                    }
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"全部轴停止！");

                }
            }
        }

        public override void MoveAbs(ushort axis, double position, double speed)
        {
            if (axis < Axis.Length)
            {
                CardErrorMessage(LTDMC.dmc_set_profile_unit(0, axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(0, axis, position, 1));
            }
        }

        public override void MoveRel(ushort axis, double position, double speed)
        {
            if (axis < Axis.Length)
            {
                CardErrorMessage(LTDMC.dmc_set_profile_unit(0, axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(0, axis, position, 0));
            }
        }

        public override double[] GetAxisState(ushort axis)
        {
            ushort[] state = new ushort[2];
            double[] doubles = new double[7];
            int a = 0;
            LTDMC.dmc_get_target_position_unit(0, axis, ref doubles[0]);//位置
            LTDMC.dmc_get_encoder_unit(0, axis, ref doubles[1]);//编码器
            LTDMC.dmc_read_current_speed_unit(0, axis, ref doubles[2]);//速度
            LTDMC.dmc_get_position_unit(0, axis, ref doubles[3]);//目标位置
            LTDMC.nmc_get_axis_state_machine(0, axis, ref state[0]);//轴状态机：0：轴处于未启动状态 1：轴处于启动禁止状态 2：轴处于准备启动状态 3：轴处于启动状态 4：轴处于操作使能状态 5：轴处于停止状态 6：轴处于错误触发状态 7：轴处于错误状态
            LTDMC.dmc_get_axis_run_mode(0, axis, ref state[1]);//轴运行模式：0：空闲 1：Pmove 2：Vmove 3：Hmove 4：Handwheel 5：Ptt / Pts 6：Pvt / Pvts 10：Continue
            LTDMC.dmc_get_stop_reason(0, axis, ref a);//轴停止原因获取：0：正常停止  3：LTC 外部触发立即停止，IMD_STOP_AT_LTC 4：EMG 立即停止，IMD_STOP_AT_EMG 5：正硬限位立即停止，IMD_STOP_AT_ELP6：负硬限位立即停止，IMD_STOP_AT_ELN7：正硬限位减速停止，DEC_STOP_AT_ELP8：负硬限位减速停止，DEC_STOP_AT_ELN9：正软限位立即停止，IMD_STOP_AT_SOFT_ELP10：负软限位立即停止，IMD_STOP_AT_SOFT_ELN11：正软限位减速停止，DEC_STOP_AT_SOFT_ELP12：负软限位减速停止，DEC_STOP_AT_SOFT_ELN13：命令立即停止，IMD_STOP_AT_CMD14：命令减速停止，DEC_STOP_AT_CMD15：其它原因立即停止，IMD_STOP_AT_OTHER16：其它原因减速停止，DEC_STOP_AT_OTHER17：未知原因立即停止，IMD_STOP_AT_UNKOWN18：未知原因减速停止，DEC_STOP_AT_UNKOWN
            Array.Copy(state, 0, doubles, 4, 2);
            doubles[6] = a;
            return doubles;
        }

        public override bool[] GetAxisExternalio(ushort axis)
        {
            var state = LTDMC.dmc_axis_io_status(0, axis);
            bool[] bools = new bool[8];
            bools[0] = (state & 0) == 0 ? false : true;
            bools[1] = (state & 1) == 1 ? false : true;
            bools[2] = (state & 2) == 2 ? false : true;
            bools[3] = (state & 3) == 3 ? false : true;
            bools[4] = (state & 4) == 4 ? false : true;
            bools[5] = (state & 5) == 5 ? false : true;
            bools[6] = (state & 6) == 6 ? false : true;
            bools[7] = (state & 7) == 7 ? false : true;
            return bools;
        }

        public override void MoveReset(ushort axis)
        {
            throw new NotImplementedException();
        }

        private void Read()
        {
            Stopwatch stopwatch = new Stopwatch();
            while (true)
            {
                stopwatch.Restart();
                for (ushort i = 0; i < Axis.Length; i++)
                {
                    GetAxisState(i);
                    GetAxisExternalio(i);
                }
                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed);//数据刷新用时
            }
        }
    }
}
