using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace MotionControl.MotionClass
{
    internal sealed class LeiSai : MotionBase
    {
        /// <summary>
        /// 启动按钮上升沿触发事件
        /// </summary>
        private event Func<DateTime, string, byte> StartPEvent;

        /// <summary>
        /// 启动按钮下降沿触发事件
        /// </summary>
        private event Func<DateTime, string, byte> StartNEvent;

        /// <summary>
        /// 复位按钮上升沿触发事件
        /// </summary>
        private event Func<DateTime, string, byte> ResetPEvent;

        /// <summary>
        /// 复位按钮下降沿触发事件
        /// </summary>
        private event Func<DateTime, string, byte> ResetNEvent;

        /// <summary>
        /// 停止按钮上升沿触发事件
        /// </summary>
        private event Func<DateTime, string, byte> StopPEvent;

        /// <summary>
        /// 停止按钮下降沿触发事件
        /// </summary>
        private event Func<DateTime, string, byte> StopNEvent;

        /// <summary>
        /// 紧急停止按钮上升沿触发事件
        /// </summary>
        private event Func<DateTime, string, byte> EStopPEvent;

        /// <summary>
        /// 紧急停止按钮下降沿触发事件
        /// </summary>
        private event Func<DateTime, string, byte> EStopNEvent;

        /// <summary>
        /// 控制卡号1,轴映射
        /// </summary>
        public enum CardOne
        {
            X,
        }

        public override bool[] IO_Input { get; set; }
        public override bool[] IO_Output { get; set; }
        public override short Card_Number { get; set; }
        public override ushort[] Axis { get; set; }
        public int Axisquantity { get; set; }
        public override ushort FactorValue { get; set; }

        public double Speed { get; set; }
        public double Acc { get; set; }
        public double Dec { get; set; }

        public override Thread Read_t1 { get; set; }

        public override double[] AxisStates { get; set; }

        public override ManualResetEvent AutoReadEvent { get; set; }

        public override ConcurrentBag<MoveState> IMoveStateQueue { get; set; }

        public override event Action<DateTime, string> CardLogEvent;

        public LeiSai()
        {
            AutoReadEvent = new ManualResetEvent(true);
            Read_t1 = new Thread(Read);
            IMoveStateQueue = new ConcurrentBag<MoveState>();
        }

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
            for (int i = 0; i < Axis.Length; i++)
            {
                LTDMC.dmc_set_factor_error(0, Axis[i], 1, 10);
            }
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
                ushort input = 0;
                ushort output = 0;
                CardErrorMessage(LTDMC.nmc_get_total_axes(0, ref totalaxis));
                CardErrorMessage(LTDMC.nmc_get_total_ionum(0, ref input, ref output));
                IO_Input = new bool[input];
                IO_Output = new bool[output];
                if (totalaxis == System.Enum.GetNames(typeof(CardOne)).Length)
                    Axisquantity = (int)totalaxis;
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"初始化{Card_Number}张板卡，总轴数为{Axisquantity}");
                Axis = new ushort[Axisquantity];
                for (int i = 0; i < Axis.Length; i++)
                {
                    LTDMC.dmc_set_factor_error(0, Axis[i], 1, 20);
                }
                if (Read_t1.ThreadState == System.Threading.ThreadState.Unstarted)
                {
                    Read_t1.IsBackground = true;
                    Read_t1.Start();
                }
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

        public override void MoveJog(ushort axis, double speed, int posi_mode, double acc = 0.5, double dec = 0.5)
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

        private void MoveAbs(MoveState state)
        {
            if (AxisStates[4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴绝对定位复位启动错误！ {state.Axis}轴在运动中！");
            if (AxisStates[5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴绝对定位复位启动错误！ {state.Axis}轴未上使能！");
            if (state.Axis < Axis.Length && AxisStates[4] == 1 && AxisStates[5] == 4)
            {
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(0, state.Axis));
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(0, state.Axis, 0, state.Speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(0, state.Axis, state.Position, 1));
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    do
                    {
                        if (stopwatch.Elapsed.TotalMilliseconds > state.OutTime)
                            goto Timeout;

                    } while (AxisStates[4] == 0);
                    stopwatch.Stop();
                    if (AxisStates[AxisStates.Length - 1] == 0 && LTDMC.dmc_check_success_encoder(0, state.Axis) == 1)
                    {
                        IMoveStateQueue.TryTake(out state);
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴绝对定位到位完成：{stopwatch.Elapsed}mm");
                        return;
                    }
                    else
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴绝对定位停止：{stopwatch.Elapsed}mm");
                        return;
                    }
                Timeout:
                    stopwatch.Stop();
                    AxisStop(state.Axis);
                    Console.WriteLine(stopwatch.Elapsed);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴绝对定位等待到位超时：{stopwatch.Elapsed}mm");
                    throw new Exception($"{state.Axis}轴定位地址{state.Position}，非阻塞单轴绝对定位等待到位超时：{stopwatch.Elapsed}");
                });
            }
        }

        private void MoveRel(MoveState state)
        {
            if (AxisStates[4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴相对定位复位启动错误！ {state.Axis}轴在运动中！");
            if (AxisStates[5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴相对定位复位启动错误！ {state.Axis}轴未上使能！");
            if (state.Axis < Axis.Length && AxisStates[4] == 1 && AxisStates[5] == 4)
            {
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(0, state.Axis));
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                var t = state.Position - AxisStates[0];
                CardErrorMessage(LTDMC.dmc_set_profile_unit(0, state.Axis, 0, state.Speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(0, state.Axis, t, 0));
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    do
                    {
                        if (stopwatch.Elapsed.TotalMilliseconds > state.OutTime)
                            goto Timeout;

                    } while (AxisStates[4] == 0);
                    stopwatch.Stop();
                    if (AxisStates[AxisStates.Length - 1] == 0 && LTDMC.dmc_check_success_encoder(0, state.Axis) == 1)
                    {
                        IMoveStateQueue.TryTake(out state);
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位到位完成：{stopwatch.Elapsed}mm");
                        return;
                    }
                    else
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位停止：{stopwatch.Elapsed}mm");
                        return;
                    }
                Timeout:
                    stopwatch.Stop();
                    AxisStop(state.Axis);
                    Console.WriteLine(stopwatch.Elapsed);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位等待到位超时：{stopwatch.Elapsed}mm");
                    throw new Exception($"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位等待到位超时：{stopwatch.Elapsed}");
                });
            }

        }

        public override void MoveAbs(ushort axis, double position, double speed, int time)
        {
            if (AxisStates[4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴绝对定位启动错误！ {axis}轴在运动中！");
            if (AxisStates[5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴绝对定位启动错误！ {axis}轴未上使能！");
            if (axis < Axis.Length && AxisStates[4] == 1 && AxisStates[5] == 4)
            {
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(0, axis));
                Stopwatch stopwatch = new Stopwatch();
                MoveState state = new MoveState()
                {
                    Axis = axis,
                    CurrentPosition = AxisStates[0],
                    Speed = speed,
                    Position = position,
                    Movetype = 0,
                    OutTime = time,
                    Handle = DateTime.Now,
                };
                var colose = IMoveStateQueue.ToList().Find(e => e.Axis == 0);
                IMoveStateQueue.TryTake(out colose);
                IMoveStateQueue.Add(state);
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(0, axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(0, axis, position, 1));
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    do
                    {
                        if (stopwatch.Elapsed.TotalMilliseconds > time)
                            goto Timeout;
                    } while (AxisStates[4] == 0);
                    stopwatch.Stop();
                    if (AxisStates[AxisStates.Length - 1] == 0 && LTDMC.dmc_check_success_encoder(0, axis) == 1)
                    {

                        IMoveStateQueue.TryTake(out state);
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，非阻塞单轴绝对定位到位完成：{stopwatch.Elapsed}mm");
                        return;
                    }
                    else
                    {

                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，非阻塞单轴绝对定位停止：{stopwatch.Elapsed}mm");
                        return;
                    }
                Timeout:
                    stopwatch.Stop();
                    AxisStop(axis);
                    Console.WriteLine(stopwatch.Elapsed);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，非阻塞单轴绝对定位等待到位超时：{stopwatch.Elapsed}mm");
                    throw new Exception($"{axis}轴定位地址{position}，非阻塞单轴绝对定位等待到位超时：{stopwatch.Elapsed}");
                });
            }
        }

        public override void MoveRel(ushort axis, double position, double speed, int time)
        {
            if (AxisStates[4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴相对定位启动错误！ {axis}轴在运动中！");
            if (AxisStates[5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴相对定位启动错误！ {axis}轴未上使能！");
            if (axis < Axis.Length && AxisStates[4] == 1 && AxisStates[5] == 4)
            {
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(0, axis));
                Stopwatch stopwatch = new Stopwatch();
                MoveState state = new MoveState()
                {
                    Axis = axis,
                    CurrentPosition = AxisStates[0],
                    Speed = speed,
                    Position = AxisStates[0] + position,
                    Movetype = 2,
                    OutTime = time,
                    Handle = DateTime.Now,
                };
                var colose = IMoveStateQueue.ToList().Find(e => e.Axis == 0);
                IMoveStateQueue.TryTake(out colose);
                IMoveStateQueue.Add(state);
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(0, axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(0, axis, position, 0));
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    do
                    {
                        if (stopwatch.Elapsed.TotalMilliseconds > time)
                            goto Timeout;

                    } while (AxisStates[4] == 0);
                    stopwatch.Stop();
                    if (AxisStates[AxisStates.Length - 1] == 0 && LTDMC.dmc_check_success_encoder(0, axis) == 1)
                    {
                        IMoveStateQueue.TryTake(out state);
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位到位完成：{stopwatch.Elapsed}mm");
                        return;
                    }
                    else
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位停止：{stopwatch.Elapsed}mm");
                        return;
                    }
                Timeout:
                    stopwatch.Stop();
                    AxisStop(state.Axis);
                    Console.WriteLine(stopwatch.Elapsed);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位等待到位超时：{stopwatch.Elapsed}mm");
                    throw new Exception($"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位等待到位超时：{stopwatch.Elapsed}");
                });
            }
        }

        public override double[] GetAxisState(ushort axis)
        {
            ushort[] state = new ushort[2];
            double[] doubles = new double[8];
            int a = 0;
            LTDMC.dmc_get_position_unit(0, axis, ref doubles[0]); //脉冲位置
            LTDMC.dmc_get_encoder_unit(0, axis, ref doubles[1]);//编码器
            LTDMC.dmc_get_target_position_unit(0, axis, ref doubles[2]);//目标位置
            LTDMC.dmc_read_current_speed_unit(0, axis, ref doubles[3]);//速度
            doubles[4] = LTDMC.dmc_check_done(0, axis);//轴运动到位 0=运动中 1=轴停止
            LTDMC.nmc_get_axis_state_machine(0, axis, ref state[0]);//轴状态机：0：轴处于未启动状态 1：轴处于启动禁止状态 2：轴处于准备启动状态 3：轴处于启动状态 4：轴处于操作使能状态 5：轴处于停止状态 6：轴处于错误触发状态 7：轴处于错误状态
            LTDMC.dmc_get_axis_run_mode(0, axis, ref state[1]);//轴运行模式：0：空闲 1：定位模式 2：定速模式 3：回零模式 4：手轮模式 5：Ptt / Pts 6：Pvt / Pvts 10：Continue
            LTDMC.dmc_get_stop_reason(0, axis, ref a);//轴停止原因获取：0：正常停止  3：LTC 外部触发立即停止，IMD_STOP_AT_LTC 4：EMG 立即停止，IMD_STOP_AT_EMG 5：正硬限位立即停止，IMD_STOP_AT_ELP6：负硬限位立即停止，IMD_STOP_AT_ELN7：正硬限位减速停止，DEC_STOP_AT_ELP8：负硬限位减速停止，DEC_STOP_AT_ELN9：正软限位立即停止，IMD_STOP_AT_SOFT_ELP10：负软限位立即停止，IMD_STOP_AT_SOFT_ELN11：正软限位减速停止，DEC_STOP_AT_SOFT_ELP12：负软限位减速停止，DEC_STOP_AT_SOFT_ELN13：命令立即停止，IMD_STOP_AT_CMD14：命令减速停止，DEC_STOP_AT_CMD15：其它原因立即停止，IMD_STOP_AT_OTHER16：其它原因减速停止，DEC_STOP_AT_OTHER17：未知原因立即停止，IMD_STOP_AT_UNKOWN18：未知原因减速停止，DEC_STOP_AT_UNKOWN
            Array.Copy(state, 0, doubles, 5, 2);
            doubles[7] = a;
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
            foreach (var item in IMoveStateQueue)
            {
                switch (item.Movetype)
                {
                    case 0: MoveAbs(item); return;
                    case 2: MoveRel(item); return;

                    default:
                        break;
                }
            }
        }

        private void Read()
        {
            Stopwatch stopwatch = new Stopwatch();
            while (true)
            {
                stopwatch.Restart();
                for (ushort i = 0; i < Axis.Length; i++)
                {
                    GetEtherCATState(i);
                    AxisStates = GetAxisState(i);
                    GetAxisExternalio(i);
                    Getall_IOinput(i);
                    Getall_IOoutput(i);
                }
                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed);//数据刷新用时
                AutoReadEvent.WaitOne();
            }
        }

        public override void AwaitMoveAbs(ushort axis, double position, double speed, int time)
        {
            if (AxisStates[4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴阻塞绝对定位启动错误！ {axis}轴在运动中！");

            if (AxisStates[5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴阻塞绝对定位启动错误！ {axis}轴未上使能！");
            if (axis < Axis.Length && AxisStates[4] == 1 && AxisStates[5] == 4)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(0, axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(0, axis, position, 1));
                do
                {
                    if (stopwatch.Elapsed.TotalMilliseconds > time)
                        goto Timeout;
                } while (AxisStates[4] == 0);
                stopwatch.Stop();
                if (LTDMC.dmc_check_success_encoder(0, axis) == 1)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，单轴绝对定位到位完成：{stopwatch.Elapsed}mm");
                }
                else
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，到位编码器误差过大！：{stopwatch.Elapsed}mm");
                }
                return;

            Timeout:
                stopwatch.Stop();
                AxisStop(axis);
                Console.WriteLine(stopwatch.Elapsed);
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，单轴绝对定位等待到位超时：{stopwatch.Elapsed}mm");
                throw new Exception($"{axis}轴定位地址{position}，单轴绝对定位等待到位超时：{stopwatch.Elapsed}");
            }
        }

        public override void AwaitMoveRel(ushort axis, double position, double speed, int time)
        {
            if (AxisStates[4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴阻塞相对定位启动错误！ {axis}轴在运动中！");
            if (AxisStates[5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴阻塞相对定位启动错误！ {axis}轴未上使能！");
            if (axis < Axis.Length && AxisStates[4] == 1 && AxisStates[5] == 4)
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(0, axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(0, axis, position, 0));
                do
                {
                    if (stopwatch.Elapsed.TotalMilliseconds > time)
                        goto Timeout;
                } while (AxisStates[4] == 0);
                stopwatch.Stop();
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，单轴相对定位到位完成：{stopwatch.Elapsed}mm");
                return;
            Timeout:
                stopwatch.Stop();
                AxisStop(axis);
                Console.WriteLine(stopwatch.Elapsed);
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，单轴相对定位等待到位超时：{stopwatch.Elapsed}mm");
                throw new Exception($"{axis}轴定位地址{position}，单轴相对定位等待到位超时：{stopwatch.Elapsed}");
            }

        }

        public override bool[] Getall_IOinput(ushort card)
        {
            if (IO_Input != null)
            {
                var input = LTDMC.dmc_read_inport(card, 0);
                for (int i = 0; i < IO_Input.Length; i++)
                {
                    IO_Input[i] = (input & (1 << i)) == 0 ? LevelSignal : !LevelSignal;
                }

            }
            return IO_Input;
        }

        public override bool[] Getall_IOoutput(ushort card)
        {
            if (IO_Output != null)
            {
                var output = LTDMC.dmc_read_outport(card, 0);
                for (int i = 0; i < IO_Output.Length; i++)
                {
                    IO_Output[i] = (output & (1 << i)) == 0 ? LevelSignal : !LevelSignal;
                }
            }
            return IO_Output;
        }

        public override void Set_IOoutput(ushort card, ushort indexes, bool value)
        {
            if (IO_Output != null)
            {
                if (LevelSignal)
                    value = true;
                else
                    value = false;
                CardErrorMessage(LTDMC.dmc_write_outbit(card, indexes, Convert.ToUInt16(value)));
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"设置输出口{indexes}，状态{!value}");
            }
        }

        public override void AwaitIOinput(ushort card, ushort indexes, bool waitvalue, int timeout)
        {
            if (IO_Input != null)
            {
                if (LevelSignal)
                    waitvalue = true;
                else
                    waitvalue = false;
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                do
                {
                    if (stopwatch.Elapsed.TotalMilliseconds > timeout)
                        goto Timeout;

                } while (LTDMC.dmc_read_inbit(card, indexes) != Convert.ToInt16(waitvalue));
                stopwatch.Stop();
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"等待输入口{indexes}，状态{!waitvalue}完成：{stopwatch.Elapsed}mm");
                return;
            Timeout:
                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed);
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"等待输入口{indexes}，状态{!waitvalue}超时：{stopwatch.Elapsed}mm");
                throw new Exception($"等待输入口{indexes}，状态{!waitvalue}超时：{stopwatch.Elapsed}mm");
            }
        }

        public override int[] GetEtherCATState(ushort card_number)
        {
            if (card_number < Card_Number)
            {
                int a = 0; int b = 0;
                LTDMC.nmc_get_cycletime(0, 2, ref a);//总线循环周期us
                LTDMC.nmc_get_errcode(0, 2, ref b);//总线状态 0=正常
                return new int[] { a, b };
            }
            return null;
        }

        public override void ResetCard(ushort card, ushort reset)
        {
            if (card < Card_Number)
            {
                AutoReadEvent.Reset();
                Thread.Sleep(100);
                switch (reset)
                {
                    case 0: LTDMC.dmc_soft_reset(card); break;
                    case 1: LTDMC.dmc_cool_reset(card); break;
                    case 2: LTDMC.dmc_original_reset(card); break;
                    default:
                        break;
                }
                LTDMC.dmc_board_close();
                Thread.Sleep(15000);
                OpenCard();
                AutoReadEvent.Set();
            }
        }

        public override void SetExternalTrigger(ushort card, ushort start, ushort reset, ushort stop, ushort estop)
        {
            throw new NotImplementedException();
        }
    }
}
