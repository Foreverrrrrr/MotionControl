using SQLiteHelper;
using System;
using System.Drawing.Drawing2D;
using System.IO;

namespace MotionControl.MotionClass
{
    public abstract class MotionBase : IControlBaseInterface
    {
        public abstract bool[] IO_Intput { get; set; }
        public abstract bool[] IO_Output { get; set; }
        public abstract short Card_Number { get; set; }
        public abstract ushort[] Axis { get; set; }

        public abstract event Action<DateTime, string> CardLogEvent;

        public event Action<object, string> CardErrorMessageEvent;

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

        public static string CardBrand { get; set; }
       

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
                CardErrorMessageEvent(type, data);
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
        public abstract void AxisJog(ushort axis, double speed, int posi_mode, double acc = 0.1, double dec = 0.1);
        public abstract void AxisStop(ushort axis, int stop_mode, bool all);
        public abstract void AxisABS(ushort axis, double position, double speed);
        public abstract void AxisRel(ushort axis, double position, double speed);
        public abstract object GetAxisState(ushort axis);
        public abstract object GetAxisExternalio(ushort axis);
    }
}
