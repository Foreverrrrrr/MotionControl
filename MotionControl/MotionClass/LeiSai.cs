using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace MotionControl
{
    public sealed class LeiSai : MotionBase
    {
        /// <summary>
        /// 启动按钮上升沿触发事件
        /// </summary>
        private event Action<DateTime> StartPEvent;

        /// <summary>
        /// 启动按钮下降沿触发事件
        /// </summary>
        private event Action<DateTime> StartNEvent;

        /// <summary>
        /// 复位按钮上升沿触发事件
        /// </summary>
        private event Action<DateTime> ResetPEvent;

        /// <summary>
        /// 复位按钮下降沿触发事件
        /// </summary>
        private event Action<DateTime> ResetNEvent;

        /// <summary>
        /// 停止按钮上升沿触发事件
        /// </summary>
        private event Action<DateTime> StopPEvent;

        /// <summary>
        /// 停止按钮下降沿触发事件
        /// </summary>
        private event Action<DateTime> StopNEvent;

        /// <summary>
        /// 紧急停止按钮上升沿触发事件
        /// </summary>
        private event Action<DateTime> EStopPEvent;

        /// <summary>
        /// 紧急停止按钮下降沿触发事件
        /// </summary>
        private event Action<DateTime> EStopNEvent;

        /// <summary>
        /// 控制卡号1,轴映射
        /// </summary>
        public enum CardOne
        {
            X,
            Y1,
            Y2,
            Y3,
            Y4

        }
        public enum CardTwo
        {
            X = 5,
            Y1,
            Y2,
            Y3,
            Y4

        }
        /// <inheritdoc/>
        public override bool[] IO_Input { get; set; }
        /// <inheritdoc/>
        public override bool[] IO_Output { get; set; }
        /// <inheritdoc/>
        public override ushort[] Card_Number { get; set; }
        /// <inheritdoc/>
        public override ushort[] Axis { get; set; }
        public int Axisquantity { get; set; }
        /// <inheritdoc/>
        public override ushort FactorValue { get; set; }
        /// <inheritdoc/>
        public override double Speed { get; set; }
        /// <inheritdoc/>
        public override double Acc { get; set; }
        /// <inheritdoc/>
        public override double Dec { get; set; }
        /// <summary>
        /// 特殊IO
        /// </summary>
        public bool[] Special_io { get; set; }
        /// <inheritdoc/>

        public override Thread Read_t1 { get; set; }
        /// <inheritdoc/>

        public override double[][] AxisStates { get; set; }
        /// <inheritdoc/>
        public override ManualResetEvent AutoReadEvent { get; set; }
        /// <inheritdoc/>
        public override ConcurrentBag<MoveState> IMoveStateQueue { get; set; }
        /// <inheritdoc/>
        public override int[] EtherCATStates { get; set; }
        /// <inheritdoc/>
        public override int[][] Axis_IO { get; set; }
        /// <inheritdoc/>
        public override event Action<DateTime, string> CardLogEvent;

        public LeiSai()
        {
            AutoReadEvent = new ManualResetEvent(true);
            Read_t1 = new Thread(Read);
            IMoveStateQueue = new ConcurrentBag<MoveState>();
            MotionBase.Thismotion = this;
        }

        /// <inheritdoc/>
        public override void AxisOn(ushort card, ushort axis)
        {
            _ = Axis == null ? throw new Exception("请先初始化板卡再使能轴！") : true;
            if (card < Card_Number.Length)
            {
                if (axis < Axis.Length)
                {
                    LTDMC.nmc_set_axis_enable(Card_Number[card], axis);
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

        /// <inheritdoc/>
        public override void AxisOn()
        {
            _ = Axis == null ? throw new Exception("请先初始化板卡再使能轴！") : true;
            for (int i = 0; i < Card_Number.Length; i++)
            {
                LTDMC.nmc_set_axis_enable(Card_Number[i], 255);
                for (int j = 0; j < Axis.Length; j++)
                {
                    LTDMC.dmc_set_factor_error(Card_Number[i], Axis[j], 1, 20);
                }
            }
            if (CardLogEvent != null)
                CardLogEvent(DateTime.Now, $"所有轴上使能");

        }

        /// <inheritdoc/>
        public override void AxisBasicSet(ushort axis, double equiv, double startvel, double speed, double acc, double dec, double stopvel, double s_para, int posi_mode, int stop_mode)
        {
            if (Axis == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (axis < Axis.Length)
            {
                if (axis < 8)
                {
                    Acc = acc;
                    Dec = dec;
                    Speed = speed;
                    CardErrorMessage(LTDMC.dmc_set_equiv(Card_Number[0], axis, equiv));  //设置脉冲当量
                    CardErrorMessage(LTDMC.dmc_set_profile_unit(Card_Number[0], axis, startvel, speed, acc, dec, stopvel));//设置速度参数
                    CardErrorMessage(LTDMC.dmc_set_s_profile(Card_Number[0], axis, 0, s_para));//设置S段速度参数
                    CardErrorMessage(LTDMC.dmc_stop(Card_Number[0], axis, (ushort)stop_mode));//制动方式
                    CardErrorMessage(LTDMC.dmc_set_dec_stop_time(Card_Number[0], axis, posi_mode)); //设置减速停止时间
                }
            }
        }

        /// <inheritdoc/>
        public override bool OpenCard(ushort card_number)
        {
            var cardid = LTDMC.dmc_board_init();
            Card_Number = new ushort[cardid];
            if (Card_Number.Length > 0)
            {
                uint totalaxis = 0;
                for (int i = 0; i < Card_Number.Length; i++)
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
                if (Card_Number.Length == 0)
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

        /// <inheritdoc/>
        public override bool OpenCard()
        {
            var cardid = LTDMC.dmc_board_init();
            Card_Number = new ushort[cardid];
            if (Card_Number.Length > 0)
            {
                ushort _num = 0;
                ushort[] cardids = new ushort[cardid];
                uint[] cardtypes = new uint[cardid];
                short res = LTDMC.dmc_get_CardInfList(ref _num, cardtypes, cardids);
                Card_Number = cardids;
                uint totalaxis = 0;
                ushort input = 0;
                ushort output = 0;
                CardErrorMessage(LTDMC.nmc_get_total_axes(Card_Number[0], ref totalaxis));
                CardErrorMessage(LTDMC.nmc_get_total_ionum(Card_Number[0], ref input, ref output));
                IO_Input = new bool[input];
                IO_Output = new bool[output];
                if (totalaxis == System.Enum.GetNames(typeof(CardOne)).Length)
                    Axisquantity = (int)totalaxis;
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"初始化{Card_Number}张板卡，总轴数为{Axisquantity}");
                Axis = new ushort[Axisquantity];
                for (int i = 0; i < Axis.Length; i++)
                {
                    LTDMC.dmc_set_factor_error(Card_Number[0], Axis[i], 1, FactorValue);
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
                if (Card_Number.Length == 0)
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

        /// <inheritdoc/>
        public override void MoveJog(ushort axis, double speed, int posi_mode, double acc = 0.5, double dec = 0.5)
        {
            if (Axis == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (axis < Axis.Length)
            {
                CardErrorMessage(LTDMC.dmc_set_profile_unit(Card_Number[0], axis, 0, speed, acc, dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_vmove(Card_Number[0], axis, (ushort)posi_mode));
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"轴{axis}进行JOG运动");
            }
        }

        /// <inheritdoc/>
        public override void AxisStop(ushort axis, int stop_mode = 0, bool all = false)
        {
            if (Axis == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (axis < Axis.Length)
            {
                if (!all)
                    CardErrorMessage(LTDMC.dmc_stop(Card_Number[0], axis, (ushort)stop_mode));
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}轴停止！");
                else
                {
                    for (int i = 0; i < Axis.Length; i++)
                    {
                        CardErrorMessage(LTDMC.dmc_stop(Card_Number[0], (ushort)i, (ushort)stop_mode));
                    }
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"全部轴停止！");

                }
            }
        }

        private void MoveAbs(MoveState state)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[state.Axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴绝对定位复位启动错误！ {state.Axis}轴在运动中！");
            if (AxisStates[state.Axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴绝对定位复位启动错误！ {state.Axis}轴未上使能！");
            if (state.Axis < Axis.Length && AxisStates[state.Axis][4] == 1 && AxisStates[state.Axis][5] == 4)
            {
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], state.Axis));
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(Card_Number[0], state.Axis, 0, state.Speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(Card_Number[0], state.Axis, state.Position, 1));
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    do
                    {
                        if (state.OutTime != 0 && stopwatch.Elapsed.TotalMilliseconds > state.OutTime)
                            goto Timeout;

                    } while (AxisStates[state.Axis][4] == 0);
                    stopwatch.Stop();
                    if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] == 0 && LTDMC.dmc_check_success_encoder(Card_Number[0], state.Axis) == 1)
                    {

                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴绝对定位到位完成（{stopwatch.Elapsed}）");
                        IMoveStateQueue.TryTake(out state);
                        return;
                    }
                    else
                    {
                        if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] != 0)
                        {
                            if (CardLogEvent != null)
                                CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴绝对定位停止！（{stopwatch.Elapsed}）");
                            return;
                        }
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，到位编码器误差过大！（{stopwatch.Elapsed}）");
                        return;
                    }
                Timeout:
                    stopwatch.Stop();
                    AxisStop(state.Axis);
                    Console.WriteLine(stopwatch.Elapsed);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴绝对定位等待到位超时（{stopwatch.Elapsed}）");
                    throw new Exception($"{state.Axis}轴定位地址{state.Position}，非阻塞单轴绝对定位等待到位超时（{stopwatch.Elapsed}）");
                });
            }
        }

        private void MoveRel(MoveState state)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[state.Axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴相对定位复位启动错误！ {state.Axis}轴在运动中！");
            if (AxisStates[state.Axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴相对定位复位启动错误！ {state.Axis}轴未上使能！");
            if (state.Axis < Axis.Length && AxisStates[state.Axis][4] == 1 && AxisStates[state.Axis][5] == 4)
            {
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], state.Axis));
                var t = state.Position - AxisStates[state.Axis][0];
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(Card_Number[0], state.Axis, 0, state.Speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(Card_Number[0], state.Axis, t, 0));
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    do
                    {
                        if (state.OutTime != 0 && stopwatch.Elapsed.TotalMilliseconds > state.OutTime)
                            goto Timeout;

                    } while (AxisStates[state.Axis][4] == 0);
                    stopwatch.Stop();
                    if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] == 0 && LTDMC.dmc_check_success_encoder(Card_Number[0], state.Axis) == 1)
                    {

                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位到位完成（{stopwatch.Elapsed}）");
                        IMoveStateQueue.TryTake(out state);
                        return;
                    }
                    else
                    {
                        if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] != 0)
                        {
                            if (CardLogEvent != null)
                                CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位停止！（{stopwatch.Elapsed}）");
                            return;
                        }
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，到位编码器误差过大！（{stopwatch.Elapsed}）");
                        return;
                    }
                Timeout:
                    stopwatch.Stop();
                    AxisStop(state.Axis);
                    Console.WriteLine(stopwatch.Elapsed);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位等待到位超时（{stopwatch.Elapsed}）");
                    throw new Exception($"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位等待到位超时（{stopwatch.Elapsed}）");
                });
            }

        }

        private void AwaitMoveAbs(MoveState state)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[state.Axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴阻塞绝对定位复位启动错误！ {state.Axis}轴在运动中！");

            if (AxisStates[state.Axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴阻塞绝对定位复位启动错误！ {state.Axis}轴未上使能！");
            if (state.Axis < Axis.Length && AxisStates[state.Axis][4] == 1 && AxisStates[state.Axis][5] == 4)
            {
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], state.Axis));
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}复位单轴阻塞绝对定位开始启动，定位地址{state.Position}，定位速度：{state.Speed}");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(Card_Number[0], state.Axis, 0, state.Speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(Card_Number[0], state.Axis, state.Position, 1));
                Thread.Sleep(100);
                do
                {
                    if (state.OutTime != 0 && stopwatch.Elapsed.TotalMilliseconds > state.OutTime)
                        goto Timeout;
                } while (AxisStates[state.Axis][4] == 0);
                stopwatch.Stop();
                if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] == 0 && LTDMC.dmc_check_success_encoder(Card_Number[0], state.Axis) == 1)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，单轴绝对定位到位完成（{stopwatch.Elapsed}）");
                    IMoveStateQueue.TryTake(out state);
                    return;
                }
                else
                {
                    if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] != 0)
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，单轴绝对定位停止！（{stopwatch.Elapsed}）");
                        return;
                    }
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，到位编码器误差过大！（{stopwatch.Elapsed}）");
                    return;
                }
            Timeout:
                stopwatch.Stop();
                AxisStop(state.Axis);
                Console.WriteLine(stopwatch.Elapsed);
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，单轴绝对定位等待到位超时（{stopwatch.Elapsed}）");
                throw new Exception($"{state.Axis}轴定位地址{state.Position}，单轴绝对定位等待到位超时（{stopwatch.Elapsed}）");
            }
        }

        private void AwaitMoveRel(MoveState state)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[state.Axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴阻塞相对定位复位启动错误！ {state.Axis}轴在运动中！");

            if (AxisStates[state.Axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴阻塞相对定位复位启动错误！ {state.Axis}轴未上使能！");
            if (state.Axis < Axis.Length && AxisStates[state.Axis][4] == 1 && AxisStates[state.Axis][5] == 4)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}复位单轴阻塞相对定位开始启动，定位地址{state.Position}，定位速度：{state.Speed}");
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], state.Axis));
                Stopwatch stopwatch = new Stopwatch();
                var t = state.Position - AxisStates[state.Axis][0];
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(Card_Number[0], state.Axis, 0, state.Speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(Card_Number[0], state.Axis, t, 0));
                Thread.Sleep(100);
                do
                {
                    if (state.OutTime != 0 && stopwatch.Elapsed.TotalMilliseconds > state.OutTime)
                        goto Timeout;
                } while (AxisStates[state.Axis][4] == 0);
                stopwatch.Stop();
                if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] == 0 && LTDMC.dmc_check_success_encoder(Card_Number[0], state.Axis) == 1)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，单轴相对定位到位完成 （{stopwatch.Elapsed}）");
                    IMoveStateQueue.TryTake(out state);
                    return;
                }
                else
                {
                    if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] != 0)
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，单轴相对定位停止！（{stopwatch.Elapsed}）");
                        return;
                    }
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，到位编码器误差过大！（{stopwatch.Elapsed}）");
                    return;
                }
            Timeout:
                stopwatch.Stop();
                AxisStop(state.Axis);
                Console.WriteLine(stopwatch.Elapsed);
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，单轴相对定位等待到位超时 （{stopwatch.Elapsed}");
                throw new Exception($"{state.Axis}轴定位地址{state.Position}，单轴相对定位等待到位超时 （{stopwatch.Elapsed}");
            }
        }

        private void MoveHome(MoveState state)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[state.Axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴原点回归复位启动错误！ {state.Axis}轴在运动中！");
            if (AxisStates[state.Axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴原点回归复位启动错误！ {state.Axis}轴未上使能！");
            if (state.Axis < Axis.Length && AxisStates[state.Axis][4] == 1 && AxisStates[state.Axis][5] == 4)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴原点回归启动！");
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], state.Axis));
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.nmc_set_home_profile(Card_Number[0], state.Axis, state.HomeModel, state.Speed / 2, state.Speed, state.ACC, state.Dcc, state.Home_off));

                CardErrorMessage(LTDMC.nmc_home_move(Card_Number[0], state.Axis));
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    do
                    {
                        if (state.OutTime != 0 && stopwatch.Elapsed.TotalMilliseconds > state.OutTime)
                            goto Timeout;

                    } while (AxisStates[state.Axis][4] == 0);
                    stopwatch.Stop();
                    if (AxisStates[state.Axis][0] < FactorValue && AxisStates[state.Axis][0] > -FactorValue && LTDMC.dmc_check_success_encoder(Card_Number[0], state.Axis) == 1)
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴原点回归复位完成，零点误差：{AxisStates[state.Axis][0]} （{stopwatch.Elapsed}）");
                        IMoveStateQueue.TryTake(out state);
                        LTDMC.dmc_set_position_unit(Card_Number[0], state.Axis, 0);
                        LTDMC.dmc_set_encoder_unit(Card_Number[0], state.Axis, 0);
                        return;
                    }
                    else
                    {
                        if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] != 0)
                        {
                            if (CardLogEvent != null)
                                CardLogEvent(DateTime.Now, $"{state.Axis}轴原点回归复位异常停止！（{stopwatch.Elapsed}）");
                            return;
                        }
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴原点回归复位异常停止，到位编码器误差过大！（{stopwatch.Elapsed}）");
                        return;
                    }
                Timeout:
                    stopwatch.Stop();
                    AxisStop(state.Axis);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴原点回归复位超时停止！（{stopwatch.Elapsed}）");
                    throw new Exception($"{state.Axis}轴原点回归复位超时停止！（{stopwatch.Elapsed}）");
                });
            }
        }

        private void AwaitMoveHome(MoveState state)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[state.Axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴原点回归启动错误！ {state.Axis}轴在运动中！");
            if (AxisStates[state.Axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴原点回归启动错误！ {state.Axis}轴未上使能！");
            if (state.Axis < Axis.Length && AxisStates[state.Axis][4] == 1 && AxisStates[state.Axis][5] == 4)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}单轴原点回归启动！");
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], state.Axis));
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.nmc_set_home_profile(Card_Number[0], state.Axis, state.HomeModel, state.Speed / 2, state.Speed, state.ACC, state.Dcc, state.Home_off));

                CardErrorMessage(LTDMC.nmc_home_move(Card_Number[0], state.Axis));
                Thread.Sleep(100);
                do
                {
                    if (state.OutTime != 0 && stopwatch.Elapsed.TotalMilliseconds > state.OutTime)
                        goto Timeout;

                } while (AxisStates[state.Axis][4] == 0);
                stopwatch.Stop();
                if (AxisStates[state.Axis][0] < FactorValue && AxisStates[state.Axis][0] > -FactorValue && LTDMC.dmc_check_success_encoder(Card_Number[0], state.Axis) == 1)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴原点回归完成，零点误差：{AxisStates[state.Axis][0]} （{stopwatch.Elapsed}）");
                    IMoveStateQueue.TryTake(out state);
                    LTDMC.dmc_set_position_unit(Card_Number[0], state.Axis, 0);
                    LTDMC.dmc_set_encoder_unit(Card_Number[0], state.Axis, 0);
                    return;
                }
                else
                {
                    if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] != 0)
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴原点回归异常停止！（{stopwatch.Elapsed}）");
                        return;
                    }
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴原点回归异常停止，到位编码器误差过大！（{stopwatch.Elapsed}）");
                    return;
                }
            Timeout:
                stopwatch.Stop();
                AxisStop(state.Axis);
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{state.Axis}轴原点回归超时停止！（{stopwatch.Elapsed}）");
                throw new Exception($"{state.Axis}轴原点回归超时停止！（{stopwatch.Elapsed}）");
            }
        }

        /// <inheritdoc/>
        public override void MoveAbs(ushort axis, double position, double speed, int time)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴绝对定位启动错误！ {axis}轴在运动中！");
            if (AxisStates[axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴绝对定位启动错误！ {axis}轴未上使能！");
            if (axis < Axis.Length && AxisStates[axis][4] == 1 && AxisStates[axis][5] == 4)
            {
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], axis));
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴绝对定位开始启动，定位地址{position}，定位速度：{speed}");
                Stopwatch stopwatch = new Stopwatch();
                MoveState state = new MoveState()
                {
                    Axis = axis,
                    CurrentPosition = AxisStates[axis][0],
                    Speed = speed,
                    Position = position,
                    Movetype = 1,
                    OutTime = time,
                    Handle = DateTime.Now,
                };
                var colose = IMoveStateQueue.ToList().Find(e => e.Axis == axis);
                IMoveStateQueue.TryTake(out colose);
                IMoveStateQueue.Add(state);
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(Card_Number[0], axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(Card_Number[0], axis, position, 1));
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    do
                    {
                        if (time != 0 && stopwatch.Elapsed.TotalMilliseconds > time)
                            goto Timeout;
                    } while (AxisStates[axis][4] == 0);
                    stopwatch.Stop();
                    if (AxisStates[axis][AxisStates[axis].Length - 1] == 0 && LTDMC.dmc_check_success_encoder(Card_Number[0], axis) == 1)
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，非阻塞单轴绝对定位到位完成 （{stopwatch.Elapsed}）");
                        IMoveStateQueue.TryTake(out state);
                        return;
                    }
                    else
                    {
                        if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] != 0)
                        {
                            if (CardLogEvent != null)
                                CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴绝对定位停止！（{stopwatch.Elapsed}）");
                            return;
                        }
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，到位编码器误差过大！（{stopwatch.Elapsed}）");
                        return;
                    }
                Timeout:
                    stopwatch.Stop();
                    AxisStop(axis);
                    Console.WriteLine(stopwatch.Elapsed);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，非阻塞单轴绝对定位等待到位超时 （{stopwatch.Elapsed}）");
                    throw new Exception($"{axis}轴定位地址{position}，非阻塞单轴绝对定位等待到位超时 （{stopwatch.Elapsed}）");
                });
            }
        }

        /// <inheritdoc/>
        public override void MoveRel(ushort axis, double position, double speed, int time)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴相对定位启动错误！ {axis}轴在运动中！");
            if (AxisStates[axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴相对定位启动错误！ {axis}轴未上使能！");
            if (axis < Axis.Length && AxisStates[axis][4] == 1 && AxisStates[axis][5] == 4)
            {
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], axis));
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴相对定位开始启动，定位地址{position}，定位速度：{speed}");
                Stopwatch stopwatch = new Stopwatch();
                MoveState state = new MoveState()
                {
                    Axis = axis,
                    CurrentPosition = AxisStates[axis][0],
                    Speed = speed,
                    Position = AxisStates[axis][0] + position,
                    Movetype = 2,
                    OutTime = time,
                    Handle = DateTime.Now,
                };
                var colose = IMoveStateQueue.ToList().Find(e => e.Axis == axis);
                IMoveStateQueue.TryTake(out colose);
                IMoveStateQueue.Add(state);
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(Card_Number[0], axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(Card_Number[0], axis, position, 0));
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    do
                    {
                        if (time != 0 && stopwatch.Elapsed.TotalMilliseconds > time)
                            goto Timeout;

                    } while (AxisStates[axis][4] == 0);
                    stopwatch.Stop();
                    if (AxisStates[axis][AxisStates[axis].Length - 1] == 0 && LTDMC.dmc_check_success_encoder(Card_Number[0], axis) == 1)
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位到位完成 （{stopwatch.Elapsed}）");
                        IMoveStateQueue.TryTake(out state);
                        return;
                    }
                    else
                    {
                        if (AxisStates[state.Axis][AxisStates[state.Axis].Length - 1] != 0)
                        {
                            if (CardLogEvent != null)
                                CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位停止！（{stopwatch.Elapsed}）");
                            return;
                        }
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，到位编码器误差过大！（{stopwatch.Elapsed}）");
                        return;
                    }
                Timeout:
                    stopwatch.Stop();
                    AxisStop(state.Axis);
                    Console.WriteLine(stopwatch.Elapsed);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位等待到位超时 （{stopwatch.Elapsed}）");
                    throw new Exception($"{state.Axis}轴定位地址{state.Position}，非阻塞单轴相对定位等待到位超时 （{stopwatch.Elapsed}）");
                });
            }
        }

        /// <inheritdoc/>
        public override double[] GetAxisState(ushort axis)
        {
            ushort[] state = new ushort[2];
            double[] doubles = new double[8];
            int a = 0;
            LTDMC.dmc_get_position_unit(Card_Number[0], axis, ref doubles[0]); //脉冲位置
            LTDMC.dmc_get_encoder_unit(Card_Number[0], axis, ref doubles[1]);//编码器
            LTDMC.dmc_get_target_position_unit(Card_Number[0], axis, ref doubles[2]);//目标位置
            LTDMC.dmc_read_current_speed_unit(Card_Number[0], axis, ref doubles[3]);//速度
            doubles[4] = LTDMC.dmc_check_done(Card_Number[0], axis);//轴运动到位 0=运动中 1=轴停止
            LTDMC.nmc_get_axis_state_machine(Card_Number[0], axis, ref state[0]);//轴状态机：0：轴处于未启动状态 1：轴处于启动禁止状态 2：轴处于准备启动状态 3：轴处于启动状态 4：轴处于操作使能状态 5：轴处于停止状态 6：轴处于错误触发状态 7：轴处于错误状态
            LTDMC.dmc_get_axis_run_mode(Card_Number[0], axis, ref state[1]);//轴运行模式：0：空闲 1：定位模式 2：定速模式 3：回零模式 4：手轮模式 5：Ptt / Pts 6：Pvt / Pvts 10：Continue
            LTDMC.dmc_get_stop_reason(Card_Number[0], axis, ref a);//轴停止原因获取：0：正常停止  3：LTC 外部触发立即停止，IMD_STOP_AT_LTC 4：EMG 立即停止，IMD_STOP_AT_EMG 5：正硬限位立即停止，IMD_STOP_AT_ELP6：负硬限位立即停止，IMD_STOP_AT_ELN7：正硬限位减速停止，DEC_STOP_AT_ELP8：负硬限位减速停止，DEC_STOP_AT_ELN9：正软限位立即停止，IMD_STOP_AT_SOFT_ELP10：负软限位立即停止，IMD_STOP_AT_SOFT_ELN11：正软限位减速停止，DEC_STOP_AT_SOFT_ELP12：负软限位减速停止，DEC_STOP_AT_SOFT_ELN13：命令立即停止，IMD_STOP_AT_CMD14：命令减速停止，DEC_STOP_AT_CMD15：其它原因立即停止，IMD_STOP_AT_OTHER16：其它原因减速停止，DEC_STOP_AT_OTHER17：未知原因立即停止，IMD_STOP_AT_UNKOWN18：未知原因减速停止，DEC_STOP_AT_UNKOWN
            Array.Copy(state, 0, doubles, 5, 2);
            doubles[7] = a;
            return doubles;
        }

        /// <inheritdoc/>
        public override int[] GetAxisExternalio(ushort axis)
        {
            var state = LTDMC.dmc_axis_io_status(Card_Number[0], axis);
            int[] bools = new int[7];
            bools[0] = (state & 1) == 1 ? 1 : 0;// 伺服报警 True=ON 
            bools[1] = (state & 2) == 2 ? 1 : 0;// 正限位 True=ON 
            bools[2] = (state & 4) == 4 ? 1 : 0;// 负限位 True=ON 
            bools[3] = (state & 8) == 8 ? 1 : 0;// 急停 True=ON 
            bools[4] = (state & 16) == 16 ? 1 : 0;// 原点 True=ON 
            bools[5] = (state & 32) == 32 ? 1 : 0;// 正软限位 True=ON 
            bools[6] = (state & 64) == 64 ? 1 : 0;// 负软限位 True=ON
            return bools;
        }

        /// <inheritdoc/>
        public override void MoveReset(ushort axis)
        {
            foreach (var item in IMoveStateQueue)
            {
                switch (item.Movetype)
                {
                    case 1: MoveAbs(item); return;
                    case 2: MoveRel(item); return;
                    case 3: AwaitMoveAbs(item); return;
                    case 4: AwaitMoveRel(item); return;
                    case 5: MoveHome(item); return;
                    case 6: AwaitMoveHome(item); return;
                    default:
                        break;
                }
            }
        }

        private void Read()
        {
            AxisStates = new double[Axis.Length][];
            Axis_IO = new int[Axis.Length][];
            Stopwatch stopwatch = new Stopwatch();
            while (true)
            {
                stopwatch.Restart();
                EtherCATStates = GetEtherCATState(0);
                for (ushort i = 0; i < Axis.Length; i++)
                {
                    AxisStates[i] = GetAxisState(i);
                    Axis_IO[i] = GetAxisExternalio(i);

                }
                for (ushort i = 0; i < Card_Number.Length; i++)
                {
                    IO_Input = Getall_IOinput(i);
                    IO_Output = Getall_IOoutput(i);
                }
                //SetExternalTrigger(0, 1, 2, 3, 4);
                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed);//数据刷新用时
                AutoReadEvent.WaitOne();
            }
        }


        /// <inheritdoc/>
        public override void AwaitMoveAbs(ushort axis, double position, double speed, int time)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴阻塞绝对定位启动错误！ {axis}轴在运动中！");

            if (AxisStates[axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴阻塞绝对定位启动错误！ {axis}轴未上使能！");
            if (axis < Axis.Length && AxisStates[axis][4] == 1 && AxisStates[axis][5] == 4)
            {
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], axis));
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴阻塞绝对定位开始启动，定位地址{position}，定位速度：{speed}");
                MoveState state = new MoveState()
                {
                    Axis = axis,
                    CurrentPosition = AxisStates[axis][0],
                    Speed = speed,
                    Position = position,
                    Movetype = 3,
                    OutTime = time,
                    Handle = DateTime.Now,
                };
                var colose = IMoveStateQueue.ToList().Find(e => e.Axis == axis);
                IMoveStateQueue.TryTake(out colose);
                IMoveStateQueue.Add(state);
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(Card_Number[0], axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(Card_Number[0], axis, position, 1));
                Thread.Sleep(100);
                do
                {
                    if (time != 0 && stopwatch.Elapsed.TotalMilliseconds > time)
                        goto Timeout;
                } while (AxisStates[axis][4] == 0);
                stopwatch.Stop();
                if (AxisStates[axis][AxisStates[axis].Length - 1] == 0 && LTDMC.dmc_check_success_encoder(Card_Number[0], axis) == 1)
                {
                    IMoveStateQueue.TryTake(out state);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，单轴绝对定位到位完成 （{stopwatch.Elapsed}）");
                }
                else
                {
                    if (AxisStates[axis][AxisStates[axis].Length - 1] != 0)
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，单轴绝对定位停止！（{stopwatch.Elapsed}）");
                        return;
                    }
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，到位编码器误差过大！（{stopwatch.Elapsed}）");
                }
                return;
            Timeout:
                stopwatch.Stop();
                AxisStop(axis);
                Console.WriteLine(stopwatch.Elapsed);
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，单轴绝对定位等待到位超时（{stopwatch.Elapsed}）");
                throw new Exception($"{axis}轴定位地址{position}，单轴绝对定位等待到位超时（{stopwatch.Elapsed}）");
            }
        }


        /// <inheritdoc/>
        public override void AwaitMoveRel(ushort axis, double position, double speed, int time)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴阻塞相对定位启动错误！ {axis}轴在运动中！");
            if (AxisStates[axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴阻塞相对定位启动错误！ {axis}轴未上使能！");
            if (axis < Axis.Length && AxisStates[axis][4] == 1 && AxisStates[axis][5] == 4)
            {
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], axis));
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴阻塞相对定位开始启动，定位地址{position}，定位速度：{speed}");
                MoveState state = new MoveState()
                {
                    Axis = axis,
                    CurrentPosition = AxisStates[axis][0],
                    Speed = speed,
                    Position = AxisStates[axis][0] + position,
                    Movetype = 4,
                    OutTime = time,
                    Handle = DateTime.Now,
                };
                var colose = IMoveStateQueue.ToList().Find(e => e.Axis == axis);
                IMoveStateQueue.TryTake(out colose);
                IMoveStateQueue.Add(state);
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.dmc_set_profile_unit(Card_Number[0], axis, 0, speed, Acc, Dec, 0));//设置速度参数
                CardErrorMessage(LTDMC.dmc_pmove_unit(Card_Number[0], axis, position, 0));
                Thread.Sleep(100);
                do
                {
                    if (time != 0 && stopwatch.Elapsed.TotalMilliseconds > time)
                        goto Timeout;
                } while (AxisStates[axis][4] == 0);
                stopwatch.Stop();
                if (AxisStates[axis][AxisStates[axis].Length - 1] == 0 && LTDMC.dmc_check_success_encoder(Card_Number[0], axis) == 1)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，单轴相对定位到位完成：（{stopwatch.Elapsed}）");
                }
                else
                {
                    if (AxisStates[axis][AxisStates[axis].Length - 1] != 0)
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，单轴相对定位停止！（{stopwatch.Elapsed}）");
                        return;
                    }
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，到位编码器误差过大! （{stopwatch.Elapsed}）");

                }
                return;
            Timeout:
                stopwatch.Stop();
                AxisStop(axis);
                Console.WriteLine(stopwatch.Elapsed);
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}轴定位地址{position}，单轴相对定位等待到位超时（{stopwatch.Elapsed}）");
                throw new Exception($"{axis}轴定位地址{position}，单轴相对定位等待到位超时（{stopwatch.Elapsed}）");
            }

        }

        /// <inheritdoc/>
        public override bool[] Getall_IOinput(ushort card)
        {
            if (IO_Input != null)
            {
                var input = LTDMC.dmc_read_inport(Card_Number[card], 0);
                for (int i = 0; i < IO_Input.Length; i++)
                {
                    IO_Input[i] = (input & (1 << i)) == 0 ? LevelSignal : !LevelSignal;
                }
            }
            return IO_Input;
        }

        /// <inheritdoc/>
        public override bool[] Getall_IOoutput(ushort card)
        {
            if (IO_Output != null)
            {
                var output = LTDMC.dmc_read_outport(Card_Number[card], 0);
                for (int i = 0; i < IO_Output.Length; i++)
                {
                    IO_Output[i] = (output & (1 << i)) == 0 ? LevelSignal : !LevelSignal;
                }
            }
            else
            {
                if (Card_Number == null)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                    throw new Exception($"请先调用OpenCard方法！");
                }
            }
            return IO_Output;
        }

        /// <inheritdoc/>
        public override void Set_IOoutput(ushort card, ushort indexes, bool value)
        {
            if (IO_Output != null)
            {
                if (LevelSignal)
                    value = true;
                else
                    value = false;
                CardErrorMessage(LTDMC.dmc_write_outbit(Card_Number[card], indexes, Convert.ToUInt16(value)));
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"设置输出口{indexes}，状态{!value}");
            }
            else
            {
                if (Card_Number == null)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                    throw new Exception($"请先调用OpenCard方法！");
                }
            }
        }

        /// <inheritdoc/>
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

                } while (LTDMC.dmc_read_inbit(Card_Number[card], indexes) != Convert.ToInt16(waitvalue));
                stopwatch.Stop();
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"等待输入口{indexes}，状态{!waitvalue}完成（{stopwatch.Elapsed}）");
                return;
            Timeout:
                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed);
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"等待输入口{indexes}，状态{!waitvalue}超时（{stopwatch.Elapsed}）");
                throw new Exception($"等待输入口{indexes}，状态{!waitvalue}超时（{stopwatch.Elapsed}）");
            }
            else
            {
                if (Card_Number == null)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                    throw new Exception($"请先调用OpenCard方法！");
                }
            }
        }

        /// <inheritdoc/>
        public override int[] GetEtherCATState(ushort card_number)
        {
            int a = 0;
            int b = 0;
            LTDMC.nmc_get_cycletime(Card_Number[card_number], 2, ref a);//总线循环周期us
            LTDMC.nmc_get_errcode(Card_Number[card_number], 2, ref b);//总线状态 0=正常
            return new int[] { a, b };
        }

        /// <inheritdoc/>
        public override void ResetCard(ushort card, ushort reset)
        {
            if (Card_Number == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法后在进行板卡复位！");
                throw new Exception($"请先调用OpenCard方法后在进行板卡复位！");
            }
            if (card < Card_Number.Length)
            {
                AutoReadEvent.Reset();
                Thread.Sleep(100);
                switch (reset)
                {
                    case 0: LTDMC.dmc_soft_reset(Card_Number[card]); break;
                    case 1: LTDMC.dmc_cool_reset(Card_Number[card]); break;
                    case 2: LTDMC.dmc_original_reset(Card_Number[card]); break;
                    default:
                        break;
                }
                LTDMC.dmc_board_close();
                Thread.Sleep(15000);
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                OpenCard();
                AutoReadEvent.Set();
                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed);
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"总线复位完成！（{stopwatch.Elapsed}）");
                throw new Exception($"总线复位完成！（{stopwatch.Elapsed}）");
            }

        }
        /// <inheritdoc/>

        public override void SetExternalTrigger(ushort card, ushort start, ushort reset, ushort stop, ushort estop)
        {
            if (Special_io != null)
            {
                if (!Special_io[0])
                {
                    if (IO_Input[start])
                    {
                        StartPEvent?.Invoke(new DateTime());
                    }
                }
                else if (Special_io[0])
                {
                    if (!IO_Input[start])
                    {
                        StartNEvent?.Invoke(new DateTime());
                    }
                }
                Special_io[0] = IO_Input[start];
            }
        }

        /// <inheritdoc/>
        public override void MoveHome(ushort axis, ushort home_model, double home_speed, int timeout, double acc = 0.5, double dcc = 0.5, double offpos = 0)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴原点回归启动错误！ {axis}轴在运动中！");
            if (AxisStates[axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴原点回归启动错误！ {axis}轴未上使能！");
            if (axis < Axis.Length && AxisStates[axis][4] == 1 && AxisStates[axis][5] == 4)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴原点回归启动！");
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], axis));
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.nmc_set_home_profile(Card_Number[0], axis, home_model, home_speed / 2, home_speed, acc, dcc, offpos));
                MoveState state = new MoveState()
                {
                    Axis = axis,
                    Speed = home_speed,
                    HomeModel = home_model,
                    Movetype = 5,
                    ACC = acc,
                    Dcc = dcc,
                    Home_off = offpos,
                    OutTime = timeout,
                    Handle = DateTime.Now,
                };
                var colose = IMoveStateQueue.ToList().Find(e => e.Axis == axis);
                IMoveStateQueue.TryTake(out colose);
                IMoveStateQueue.Add(state);
                CardErrorMessage(LTDMC.nmc_home_move(Card_Number[0], axis));
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    do
                    {
                        if (timeout != 0 && stopwatch.Elapsed.TotalMilliseconds > timeout)
                            goto Timeout;

                    } while (AxisStates[axis][4] == 0);
                    stopwatch.Stop();
                    if (AxisStates[axis][0] < FactorValue && AxisStates[axis][0] > -FactorValue && LTDMC.dmc_check_success_encoder(Card_Number[0], axis) == 1)
                    {
                        CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], axis));
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{axis}轴原点回归完成，零点误差：{AxisStates[axis][0]} （{stopwatch.Elapsed}）");
                        IMoveStateQueue.TryTake(out state);
                        LTDMC.dmc_set_position_unit(Card_Number[0], axis, 0);
                        LTDMC.dmc_set_encoder_unit(Card_Number[0], axis, 0);
                        return;
                    }
                    else
                    {
                        if (AxisStates[axis][AxisStates[axis].Length - 1] != 0)
                        {
                            if (CardLogEvent != null)
                                CardLogEvent(DateTime.Now, $"{axis}轴原点回归异常停止！（{stopwatch.Elapsed}）");
                            return;
                        }
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{axis}轴原点回归异常停止，到位编码器误差过大！（{stopwatch.Elapsed}）");
                        return;
                    }
                Timeout:
                    stopwatch.Stop();
                    AxisStop(axis);
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{axis}轴原点回归超时停止！（{stopwatch.Elapsed}）");
                    throw new Exception($"{axis}轴原点回归超时停止！（{stopwatch.Elapsed}）");
                });
            }
        }

        /// <inheritdoc/>
        public override void AwaitMoveHome(ushort axis, ushort home_model, double home_speed, int timeout = 3000, double acc = 0.5, double dcc = 0.5, double offpos = 0)
        {
            if (AxisStates == null)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"请先调用OpenCard方法！");
                throw new Exception($"请先调用OpenCard方法！");
            }
            if (AxisStates[axis][4] != 1)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴原点回归启动错误！ {axis}轴在运动中！");
            if (AxisStates[axis][5] != 4)
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴原点回归启动错误！ {axis}轴未上使能！");
            if (axis < Axis.Length && AxisStates[axis][4] == 1 && AxisStates[axis][5] == 4)
            {
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}单轴原点回归启动！");
                CardErrorMessage(LTDMC.dmc_clear_stop_reason(0, axis));
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Restart();
                CardErrorMessage(LTDMC.nmc_set_home_profile(Card_Number[0], axis, home_model, home_speed / 2, home_speed, acc, dcc, offpos));
                MoveState state = new MoveState()
                {
                    Axis = axis,
                    Speed = home_speed,
                    Position = 0,
                    Movetype = 6,
                    OutTime = timeout,
                    Handle = DateTime.Now,
                };
                var colose = IMoveStateQueue.ToList().Find(e => e.Axis == axis);
                IMoveStateQueue.TryTake(out colose);
                IMoveStateQueue.Add(state);
                CardErrorMessage(LTDMC.nmc_home_move(Card_Number[0], axis));
                Thread.Sleep(100);
                do
                {
                    if (timeout != 0 && stopwatch.Elapsed.TotalMilliseconds > timeout)
                        goto Timeout;

                } while (AxisStates[axis][4] == 0);
                stopwatch.Stop();
                if (AxisStates[axis][0] < FactorValue && AxisStates[axis][0] > -FactorValue && LTDMC.dmc_check_success_encoder(Card_Number[0], axis) == 1)
                {
                    CardErrorMessage(LTDMC.dmc_clear_stop_reason(Card_Number[0], axis));
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{axis}轴原点回归完成，零点误差：{AxisStates[axis][0]} （{stopwatch.Elapsed}）");
                    IMoveStateQueue.TryTake(out state);
                    LTDMC.dmc_set_position_unit(Card_Number[0], axis, 0);
                    LTDMC.dmc_set_encoder_unit(Card_Number[0], axis, 0);
                    return;
                }
                else
                {
                    if (AxisStates[axis][AxisStates[axis].Length - 1] != 0)
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, $"{axis}轴原点回归异常停止！（{stopwatch.Elapsed}）");
                        return;
                    }
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, $"{axis}轴原点回归异常停止，到位编码器误差过大！（{stopwatch.Elapsed}）");
                    return;
                }
            Timeout:
                stopwatch.Stop();
                AxisStop(axis);
                if (CardLogEvent != null)
                    CardLogEvent(DateTime.Now, $"{axis}轴原点回归超时停止！（{stopwatch.Elapsed}）");
                throw new Exception($"{axis}轴原点回归超时停止！（{stopwatch.Elapsed}）");
            }
        }
    }
}
