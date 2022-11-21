using SQLiteHelper;
using System;

namespace MotionControl.MotionClass
{
    public abstract class MotionBase : IControlBaseInterface
    {
        public abstract bool[] IO_Intput { get; set; }
        public abstract bool[] IO_Output { get; set; }
        public abstract ushort[] Card_Number { get; set; }
        public abstract ushort[] AxisNo { get; set; }

        /// <summary>
        /// 运动控制板卡方法异常事件
        /// </summary>
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

        public bool CardErrorMessage<T>(T type, int returnPattern = 1) where T : struct
        {
            if (Convert.ToInt64(type) != 0)
            {
                var data = SQLHelper.Readdata(CardBrand, Convert.ToUInt16(type));
                CardErrorMessageEvent(type, data);
                return false;
            }
            else
            {
                return true;
            }
        }


        public abstract bool OpenCard(ushort[] Card_Number);
    }
}
