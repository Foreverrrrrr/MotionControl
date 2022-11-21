using System;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.IO;
using System.Security.Cryptography;

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

        public override void AxisJog(ushort axis, double speed, int posi_mode, double acc = 0.1, double dec = 0.1)
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

        public override void AxisABS(ushort axis, double position, double speed)
        {
            if (axis < Axis.Length)
            {
                CardErrorMessage(LTDMC.dmc_set_profile_unit(0, axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(0, axis, position, 1));
            }
        }

        public override void AxisRel(ushort axis, double position, double speed)
        {
            if (axis < Axis.Length)
            {
                CardErrorMessage(LTDMC.dmc_set_profile_unit(0, axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(0, axis, position, 0));
            }
        }

        public override object GetAxisState(ushort axis)
        {
            ushort state = 0;
            LTDMC.dmc_get_axis_run_mode(0, axis, ref state);
            return state;
        }

        public override object GetAxisExternalio(ushort axis)
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
    }
}
