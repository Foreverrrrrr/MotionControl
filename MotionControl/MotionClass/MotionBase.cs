using SQLiteHelper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace MotionControl
{
    public abstract class MotionBase : IControlBaseInterface
    {
        /// <summary>
        /// 运动控制类实例对象
        /// </summary>
        public static MotionBase Thismotion { get; set; }
        /// <inheritdoc/>
        public abstract bool[] IO_Input { get; set; }
        /// <inheritdoc/>
        public abstract bool[] IO_Output { get; set; }
        /// <inheritdoc/>
        public abstract ushort[] Card_Number { get; set; }
        /// <inheritdoc/>
        public abstract ushort[] Axis { get; set; }
        /// <inheritdoc/>
        public abstract int[] EtherCATStates { get; set; }
        /// <inheritdoc/>
        public abstract double[][] AxisStates { get; set; }
        /// <summary>
        /// 输入输出高低电平反转
        /// </summary>
        public virtual bool LevelSignal { get; set; }
        /// <inheritdoc/>
        public abstract Thread Read_t1 { get; set; }
        /// <inheritdoc/>
        public abstract ManualResetEvent AutoReadEvent { get; set; }
        /// <inheritdoc/>
        public abstract event Action<DateTime, string> CardLogEvent;
        /// <inheritdoc/>
        public virtual event Action<object, string> CardErrorMessageEvent;

        /// <summary>
        /// 轴定位状态
        /// </summary>
        public struct MoveState
        {
            /// <summary>
            /// 卡号
            /// </summary>
            public ushort CardID { get; set; }
            /// <summary>
            /// 定位前位置
            /// </summary>
            public double CurrentPosition { get; set; }
            /// <summary>
            /// 目标位置
            /// </summary>
            public double Position { get; set; }
            /// <summary>
            /// 轴定位指令
            /// </summary>
            public ushort Movetype { get; set; }
            /// <summary>
            /// 定位速度
            /// </summary>
            public double Speed { get; set; }
            /// <summary>
            /// 定位轴号
            /// </summary>
            public ushort Axis { get; set; }
            /// <summary>
            /// 原点回归模式
            /// </summary>
            public ushort HomeModel { get; set; }
            /// <summary>
            /// 加速度
            /// </summary>
            public double ACC { get; set; }
            /// <summary>
            /// 减速度
            /// </summary>
            public double Dcc { get; set; }
            /// <summary>
            /// 零点偏置
            /// </summary>
            public double Home_off { get; set; }
            /// <summary>
            /// 等待超时时间（ms）
            /// </summary>
            public int OutTime { get; set; }
            /// <summary>
            /// 状态句柄
            /// </summary>
            public DateTime Handle { get; set; }
        }

        /// <summary>
        /// 板卡品牌名称
        /// </summary>
        public enum CardName
        {
            /// <summary>
            /// 雷赛板卡
            /// </summary>
            LeiSai,
            /// <summary>
            /// 高川板卡
            /// </summary>
            GaoChuān,
        }

        /// <summary>
        /// 板卡厂商
        /// </summary>
        public static string CardBrand { get; set; }

        /// <summary>
        /// 轴定位状态队列
        /// </summary>
        public virtual ConcurrentBag<MoveState> IMoveStateQueue { get; set; }
        /// <inheritdoc/>
        public abstract ushort FactorValue { get; set; }
        /// <summary>
        /// 定位速度
        /// </summary>
        public abstract double Speed { get; set; }
        /// <summary>
        /// 加速度
        /// </summary>
        public abstract double Acc { get; set; }
        /// <summary>
        /// 减速度
        /// </summary>
        public abstract double Dec { get; set; }
        /// <inheritdoc/>
        public abstract int[][] Axis_IO { get; set; }


        /// <summary>
        /// 获取板卡对象
        /// </summary>
        /// <param name="modelname">板卡厂商名称</param>
        /// <returns>返回板卡对象</returns>
        /// <exception cref="Exception"> MotionBase类：GetClass方法中出现异常</exception>
        public static MotionBase GetClassType(CardName modelname)
        {
            try
            {
                CardBrand = modelname.ToString();
                var Assemblyname = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Namespace;
                Type type = Type.GetType(Assemblyname + "." + modelname.ToString());
                dynamic obj = type.Assembly.CreateInstance(type.ToString());
                MotionBase classBase = obj as MotionBase;
                return classBase;
            }
            catch (Exception ex)
            {
                throw new Exception(" MotionBase类：GetClass方法中出现异常", ex);
            }
        }

        /// <summary>
        /// 板卡方法异常查询
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="type">方法返回值</param>
        /// <param name="returnPattern">是否查询</param>
        /// <returns></returns>
        public virtual bool CardErrorMessage<T>(T type, bool returnPattern = true) where T : struct
        {
            if (Convert.ToInt64(type) != 0)
            {
                string data = "";
                if (returnPattern)
                {
                    data = SQLHelper.Readdata(CardBrand, Convert.ToUInt16(type));
                }
                if (CardErrorMessageEvent != null)
                    CardErrorMessageEvent(type, data);
                throw new Exception("type:" + data);
            }
            else
            {
                return true;
            }
        }
        /// <inheritdoc/>
        public abstract bool OpenCard(ushort card_number);
        /// <inheritdoc/>
        public abstract bool OpenCard();
        /// <inheritdoc/>
        public abstract void AxisOn(ushort card, ushort axis);
        /// <inheritdoc/>
        public abstract void AxisOn();
        /// <inheritdoc/>
        public abstract void AxisBasicSet(ushort axis, double equiv, double startvel, double speed, double acc, double dec, double stopvel, double s_para, int posi_mode, int stop_mode);
        /// <inheritdoc/>
        public abstract void MoveJog(ushort axis, double speed, int posi_mode, double acc = 0.5, double dec = 0.5);
        /// <inheritdoc/>
        public abstract void AxisStop(ushort axis, int stop_mode, bool all);
        /// <inheritdoc/>
        public abstract void MoveAbs(ushort axis, double position, double speed, int time);
        /// <inheritdoc/>
        public abstract void MoveRel(ushort axis, double position, double speed, int time);
        /// <inheritdoc/>
        public abstract double[] GetAxisState(ushort axis);
        /// <inheritdoc/>
        public abstract int[] GetAxisExternalio(ushort axis);
        /// <inheritdoc/>
        public abstract void MoveReset(ushort axis);
        /// <inheritdoc/>
        public abstract void AwaitMoveAbs(ushort axis, double position, double speed, int time = 3000);
        /// <inheritdoc/>
        public abstract void AwaitMoveRel(ushort axis, double position, double speed, int time = 3000);
        /// <inheritdoc/>
        public abstract bool[] Getall_IOinput(ushort card);
        /// <inheritdoc/>
        public abstract bool[] Getall_IOoutput(ushort card);
        /// <inheritdoc/>
        public abstract void Set_IOoutput(ushort card, ushort indexes, bool value);
        /// <inheritdoc/>
        public abstract void AwaitIOinput(ushort card, ushort indexes, bool waitvalue, int timeout = 3000);
        /// <inheritdoc/>
        public abstract void SetExternalTrigger(ushort card, ushort start, ushort reset, ushort stop, ushort estop);
        /// <inheritdoc/>
        public abstract int[] GetEtherCATState(ushort card_number);
        /// <inheritdoc/>
        public abstract void ResetCard(ushort card, ushort reset);
        /// <inheritdoc/>
        public abstract void MoveHome(ushort axis, ushort home_model, double home_speed, int timeout = 3000, double acc = 0.5, double dcc = 0.5, double offpos = 0);
        /// <inheritdoc/>
        public abstract void AwaitMoveHome(ushort axis, ushort home_model, double home_speed, int timeout = 3000, double acc = 0.5, double dcc = 0.5, double offpos = 0);
    }
}
