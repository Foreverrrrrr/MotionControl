using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MotionControl
{
    public sealed class MoShengTai : MotionBase
    {
        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, System.Text.StringBuilder retVal, int size, string filePath);

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
        /// 输入输出高低电平反转
        /// </summary>
        public override bool LevelSignal { get; set; }
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

        public override event Action<DateTime,bool, string> CardLogEvent;


        public MoShengTai()
        {
            AutoReadEvent = new ManualResetEvent(true);
            Read_t1 = new Thread(Read);
            IMoveStateQueue = new List<MoveState>();
            MotionBase.Thismotion = this;
        }

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
            if (IsOpenCard)
            {
                if (CardErrorMessage(CMCDLL_NET.MCF_Set_Servo_Enable_Net(axis, 0, card)))
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now,false, $"{axis}轴上使能！");
                }
            }
        }

        public override void AxisOff()
        {
            if (IsOpenCard)
            {
                for (int i = 0; i < Axis.Length; i++)
                {
                    if (CardErrorMessage(CMCDLL_NET.MCF_Set_Servo_Enable_Net((ushort)i, 0, 0)))
                    {
                        if (CardLogEvent != null)
                            CardLogEvent(DateTime.Now, false, $"{i}轴下使能！");
                    }
                }
            }
        }

        public override void AxisOn(ushort card, ushort axis)
        {
            if (IsOpenCard)
            {
                if (CardErrorMessage(CMCDLL_NET.MCF_Set_Servo_Enable_Net(axis, 1, card)))
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, false, $"{axis}轴上使能！");
                }
            }
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
                            CardLogEvent(DateTime.Now, false, $"{i}轴上使能！");
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


        /// <summary>
        /// 获取板卡全部数字输入
        /// </summary>
        /// <param name="card">板卡号</param>
        /// <returns></returns>
        public override bool[] Getall_IOinput(ushort card)
        {
            uint a = 0;
            uint b = 0;
            CardErrorMessage(CMCDLL_NET.MCF_Get_Input_Net(ref a, ref b, card));
            int aLength = a != 0 ? Convert.ToString(a, 2).ToCharArray().Length : 0;
            int bLength = b != 0 ? Convert.ToString(b, 2).ToCharArray().Length : 0;
            bool[] ret = new bool[aLength + bLength];
            for (int i = 0; i < aLength + bLength; i++)
            {
                if (i < aLength)
                    ret[i] = (a & (1 << i)) == 0 ? !LevelSignal : LevelSignal;
                else
                    ret[i] = (b & (1 << i)) == 0 ? !LevelSignal : LevelSignal;
            }
            return ret;
        }

        /// <summary>
        /// 获取板卡全部数字输出
        /// </summary>
        /// <param name="card">板卡号</param>
        /// <returns></returns>
        public override bool[] Getall_IOoutput(ushort card)
        {
            uint a = 0;
            CMCDLL_NET.MCF_Get_Output_Net(ref a, card);
            int aLength = a != 0 ? Convert.ToString(a, 2).ToCharArray().Length : 0;
            bool[] ret = new bool[aLength];
            for (int i = 0; i < aLength; i++)
            {
                ret[i] = (a & (1 << i)) == 0 ? !LevelSignal : LevelSignal;
            }
            return ret;
        }

        /// <summary>
        /// 获取轴专用IO
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <returns>
        /// <param> bool[0]==伺服报警(True=ON)</param>
        /// <param> bool[1]==正限位(True=ON)</param>
        /// <param> bool[2]==负限位(True=ON)</param>
        /// <param> bool[3]==急停(True=ON)</param>
        /// <param> bool[4]==原点(True=ON)</param>
        /// <param> bool[5]==正软限位(True=ON)</param>
        /// <param> bool[6]==负软限位(True=ON)</param>
        /// </returns>
        public override int[] GetAxisExternalio(ushort axis)
        {
            ushort[] ushorts = new ushort[7];
            int[] outint = new int[7];
            CardErrorMessage(CMCDLL_NET.MCF_Get_Servo_Alarm_Net(axis, ref ushorts[0]));
            CardErrorMessage(CMCDLL_NET.MCF_Get_Positive_Limit_Net(axis, ref ushorts[1]));
            CardErrorMessage(CMCDLL_NET.MCF_Get_Negative_Limit_Net(axis, ref ushorts[2]));
            CardErrorMessage(CMCDLL_NET.MCF_Get_Home_Net(axis, ref ushorts[4]));
            for (int i = 0; i < ushorts.Length; i++)
            {
                outint[i] = ushorts[i];
            }
            return outint;
        }

        /// <summary>
        /// 获取轴状态信息
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <returns> 返回值double[6] 数组
        ///double[0]= 脉冲位置
        ///double[1]= 伺服编码器位置
        ///double[2]= 速度
        ///double[3]= 目标位置
        ///double[4]= 轴运动到位 0=运动中 1=轴停止
        ///double[5]= 轴状态机0：轴处于未启动状态 1：轴处于启动禁止状态 2：轴处于准备启动状态 3：轴处于启动状态 4：轴处于操作使能状态 5：轴处于停止状态 6：轴处于错误触发状态 7：轴处于错误状态
        ///double[6]= 轴运行模式0：空闲 1：Pmove 2：Vmove 3：Hmove 4：Handwheel 5：Ptt / Pts 6：Pvt / Pvts 10：Continue
        ///double[7]= 轴停止原因获取0：正常停止  3：LTC 外部触发立即停止  4：EMG 立即停止  5：正硬限位立即停止  6：负硬限位立即停止  7：正硬限位减速停止  8：负硬限位减速停止  9：正软限位立即停止  
        ///10：负软限位立即停止11：正软限位减速停止  12：负软限位减速停止  13：命令立即停止  14：命令减速停止  15：其它原因立即停止  16：其它原因减速停止  17：未知原因立即停止  18：未知原因减速停止
        /// </returns>
        public override double[] GetAxisState(ushort axis)
        {
            double[] doubles = new double[8];
            double[] vel = new double[2];
            int Encoderpos = 0;
            int pos = 0;
            ushort indog = 0;
            CMCDLL_NET.MCF_Get_Position_Net(axis, ref pos);//脉冲位置
            CMCDLL_NET.MCF_Get_Encoder_Net(axis, ref Encoderpos);//编码器位置
            CMCDLL_NET.MCF_Get_Vel_Net(axis, ref vel[0], ref vel[1]);//速度读取
            CMCDLL_NET.MCF_Get_Servo_INP_Net(axis, ref indog);//运动到位
            var stop = CMCDLL_NET.MCF_Clear_Axis_State_Net(axis);//停止原因
            switch (stop)
            {
                case 0: doubles[4] = 1; doubles[7] = 0; break;
                case 1: doubles[4] = 0; break;
                case 2: doubles[7] = 4; break;
                case 14: doubles[7] = 5; break;
                case 16: doubles[7] = 6; break;
                case 15: doubles[7] = 7; break;
                case 17: doubles[7] = 8; break;
                case 18: doubles[7] = 9; break;
                case 20: doubles[7] = 10; break;
                case 19: doubles[7] = 11; break;
                case 21: doubles[7] = 12; break;
                case 22: doubles[7] = 13; break;
                case 23: doubles[7] = 14; break;
                default:
                    doubles[7] = 15;
                    break;
            }

            doubles[0] = Convert.ToDouble(pos);
            doubles[1] = Convert.ToDouble(Encoderpos);
            doubles[2] = vel[0];
            doubles[3] = 0;
            return doubles;
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
                    IsOpenCard = false;
                    isopen = false;
                    CardErrorMessage(refs);
                }
                else
                {
                    IsOpenCard = true;
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now,false, "打开板卡成功!");
                    isopen = true;
                    if (Read_t1.ThreadState == System.Threading.ThreadState.Unstarted)
                    {
                        Read_t1.IsBackground = true;
                        Read_t1.Start();
                    }
                }
            }
            else
            {
                if (Card_Number == null)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now,true, "未设置板卡型号！在OpenCard前请先在Card_Number数组中设置板卡型号");
                    throw new Exception("未设置板卡型号！在OpenCard前请先在Card_Number数组中设置板卡型号");
                }
                else if (Axis == null)
                {
                    if (CardLogEvent != null)
                        CardLogEvent(DateTime.Now, true, "未设置轴数量！在OpenCard前请先在Axis数组中设置轴数量");
                    throw new Exception("未设置轴数量！在OpenCard前请先在Axis数组中设置轴数量");
                }
            }
            return isopen;
        }

        private void Read()
        {
            AxisStates = new double[Axis.Length][];
            Axis_IO = new int[Axis.Length][];
            Stopwatch stopwatch = new Stopwatch();
            while (true)
            {
                List<bool> inputList = new List<bool>();
                List<bool> outputList = new List<bool>();
                stopwatch.Restart();
                //EtherCATStates = GetEtherCATState(0);
                for (ushort i = 0; i < Axis.Length; i++)
                {
                    AxisStates[i] = GetAxisState(i);
                    Axis_IO[i] = GetAxisExternalio(i);
                }
                for (ushort i = 0; i < Card_Number.Length; i++)
                {
                    inputList.AddRange(Getall_IOinput(i));
                    outputList.AddRange(Getall_IOoutput(i));
                }
                IO_Input = inputList.ToArray();
                IO_Output = outputList.ToArray();
                //for (ushort i = 0; i < 5; i++)
                //{
                //    CoordinateSystemStates[i] = LTDMC.dmc_conti_get_run_state(Card_Number[0], i);
                //}
                //for (ushort i = 0; i < Card_Number.Length; i++)
                //{
                //    IO_Input = Getall_IOinput(i);
                //    IO_Output = Getall_IOoutput(i);
                //}
                //SetExternalTrigger(0, 1, 2, 3, 4);
                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed);//数据刷新用时
                AutoReadEvent.WaitOne();
            }
        }

        public override void ResetCard(ushort card, ushort reset)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 设置板卡轴配置文件
        /// </summary>
        public override void SetAxis_iniFile(string path)
        {
            Span<string> lines = System.IO.File.ReadAllLines(path);
            string pattern = "=(.*)";
            string io = null;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("["))
                {
                    for (int j = 1; j < 7; j++)
                    {
                        Match match = Regex.Match(lines[i + j], pattern);
                        if (match.Success && Convert.ToInt32(match.Groups[1].Value) < 5 && Convert.ToInt32(match.Groups[1].Value) >= 0)
                        {
                            switch (j)
                            {
                                case 1: CMCDLL_NET.MCF_Set_ELP_Trigger_Net((ushort)(i / 5), Convert.ToUInt16(match.Groups[1].Value)); break;
                                case 2: CMCDLL_NET.MCF_Set_ELN_Trigger_Net((ushort)(i / 5), Convert.ToUInt16(match.Groups[1].Value)); break;
                                case 3: CMCDLL_NET.MCF_Set_Alarm_Trigger_Net((ushort)(i / 5), Convert.ToUInt16(match.Groups[1].Value)); break;
                                case 4: CMCDLL_NET.MCF_Set_Home_Trigger_Net((ushort)(i / 5), Convert.ToUInt16(match.Groups[1].Value)); break;
                                case 5: io = match.Groups[1].Value; break;
                                case 6: CardErrorMessage(CMCDLL_NET.MCF_Set_Input_Trigger_Net(0, (ushort)(i / 5), Convert.ToUInt16(io), Convert.ToUInt16(match.Groups[1].Value))); break;
                                default:
                                    break;
                            }
                        }
                    }
                    i = i + 4;
                }
            }
        }

        public override void SetbjectDictionary(ushort card, ushort etherCATLocation, ushort primeindex, ushort wordindexing, ushort bitlength, int value)
        {
            throw new NotImplementedException();
        }

        public override void SetEtherCAT_eniFiel()
        {
            throw new NotImplementedException();
        }

        public override void SetExternalTrigger(ushort start, ushort reset, ushort stop, ushort estop)
        {
            throw new NotImplementedException();
        }

        public override void Set_IOoutput(ushort card, ushort indexes, bool value)
        {
            throw new NotImplementedException();
        }

        public override void AwaitMoveLines(ushort card, ControlState t, int time = 0)
        {
            throw new NotImplementedException();
        }

        public override void AxisReset(ushort axis)
        {
            throw new NotImplementedException();
        }

        ///// <summary>
        ///// 设置外部紧急停止输入（摩升泰专用方法）
        ///// </summary>
        ///// <param name="card">卡号</param>
        ///// <param name="in_put">输入点</param>
        ///// <param name="stop_model">停止模式 0：关闭触发 1：低电平触发紧急停止 2：低电平触发减速停止 3：高电平触发紧急停止 4：高电平触发减速停止</param>
        //public override void Set_ExigencyIO(ushort card, ushort in_put, uint stop_model)
        //{
        //    if (IsOpenCard)
        //    {
        //        for (ushort i = 0; i < Axis.Length; i++)
        //        {
        //            CardErrorMessage(CMCDLL_NET.MCF_Set_Input_Trigger_Net(card, i, in_put, stop_model));
        //        }
        //    }
        //}
    }
}
