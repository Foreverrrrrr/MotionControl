using SQLiteHelper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace MotionControl.MotionClass
{
    public abstract class MotionBase : IControlBaseInterface
    {
        public abstract bool[] IO_Input { get; set; }
        public abstract bool[] IO_Output { get; set; }
        public abstract short Card_Number { get; set; }
        public abstract ushort[] Axis { get; set; }
        public abstract double[] AxisStates { get; set; }
        public virtual bool LevelSignal { get; set; }
        public abstract Thread Read_t1 { get; set; }

        public abstract ManualResetEvent AutoReadEvent { get; set; }

        public abstract event Action<DateTime, string> CardLogEvent;

        public virtual event Action<object, string> CardErrorMessageEvent;

        /// <summary>
        /// 轴定位状态
        /// </summary>
        public struct MoveState
        {
            /// <summary>
            /// 轴定位状态
            /// </summary>
            /// <param name="axis">定位轴号</param>
            /// <param name="cpos">定位前位置</param>
            /// <param name="pos">定位目标位置</param>
            /// <param name="move">定位方式</param>
            /// <param name="sp">定位速度</param>
            /// <param name="outtime">等待超时时间</param>
            /// <param name="handel">对象句柄</param>
            public MoveState(byte axis, double cpos, double pos, byte move, double sp, int outtime, DateTime handel)
            {
                CurrentPosition = cpos;
                Position = pos;
                Movetype = move;
                Speed = sp;
                Axis = axis;
                OutTime = outtime;
                Handle = handel;
            }

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
        public abstract ushort FactorValue { get; set; }


        /// <summary>
        /// 获取板卡对象
        /// </summary>
        /// <param name="modelname">板卡厂商名称</param>
        /// <returns>返回板卡对象</returns>
        /// <exception cref="Exception"> MotionBase类：GetClass方法中出现异常</exception>
        internal static MotionBase GetClassType(CardName modelname)
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
                throw new Exception("type:"+data);
                return false;
            }
            else
            {
                return true;
            }
        }
        public abstract bool OpenCard(ushort card_number);
        public abstract bool OpenCard();
        public abstract void AxisOn(ushort card, ushort axis);
        public abstract void AxisOn();
        public abstract void AxisBasicSet(ushort axis, double equiv, double startvel, double speed, double acc, double dec, double stopvel, double s_para, int posi_mode, int stop_mode);
        public abstract void MoveJog(ushort axis, double speed, int posi_mode, double acc = 0.5, double dec = 0.5);
        public abstract void AxisStop(ushort axis, int stop_mode, bool all);
        public abstract void MoveAbs(ushort axis, double position, double speed, int time);
        public abstract void MoveRel(ushort axis, double position, double speed, int time);
        public abstract double[] GetAxisState(ushort axis);
        public abstract bool[] GetAxisExternalio(ushort axis);
        public abstract void MoveReset(ushort axis);
        public abstract void AwaitMoveAbs(ushort axis, double position, double speed, int time = 3000);
        public abstract void AwaitMoveRel(ushort axis, double position, double speed, int time = 3000);
        public abstract bool[] Getall_IOinput(ushort card);
        public abstract bool[] Getall_IOoutput(ushort card);
        public abstract void Set_IOoutput(ushort card, ushort indexes, bool value);
        public abstract void AwaitIOinput(ushort card, ushort indexes, bool waitvalue, int timeout = 3000);
        public abstract void SetExternalTrigger(ushort card, ushort start, ushort reset, ushort stop, ushort estop);
        public abstract int[] GetEtherCATState(ushort card_number);
        public abstract void ResetCard(ushort card, ushort reset);
    }
}
