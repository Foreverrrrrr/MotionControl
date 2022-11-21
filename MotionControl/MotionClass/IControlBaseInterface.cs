using System;
using System.Collections.Generic;

namespace MotionControl.MotionClass
{
    internal interface IControlBaseInterface
    {
        /// <summary>
        /// 数字io输入
        /// </summary>
        bool[] IO_Intput { get; set; }

        /// <summary>
        /// 数字io输出
        /// </summary>
        bool[] IO_Output { get; set; }

        /// <summary>
        /// 板卡号
        /// </summary>
        short Card_Number { get; set; }

        /// <summary>
        /// 轴号
        /// </summary>
        ushort[] Axis { get; set; }
        /// <summary>
        /// 卡配置
        /// </summary>

        /// <summary>
        /// 运动控制板卡方法异常事件
        /// </summary>
        event Action<object, string> CardErrorMessageEvent;

        /// <summary>
        /// 板卡运行日志事件
        /// </summary>
        event Action<DateTime, string> CardLogEvent;

        /// <summary>
        /// 打开指定板卡
        /// </summary>
        /// <param name="card_number">板卡号</param>
        /// <returns></returns>
        bool OpenCard(ushort card_number);

        /// <summary>
        /// 打开所有板卡
        /// </summary>
        /// <returns></returns>
        bool OpenCard();

        /// <summary>
        /// 单个轴使能
        /// </summary>
        /// <param name="card">卡号</param>
        /// <param name="axis">轴号</param>
        /// <returns></returns>
        void AxisOn(ushort card, ushort axis);

        /// <summary>
        /// 轴批量使能
        /// </summary>
        /// <returns></returns>
        void AxisOn();

        /// <summary>
        /// 轴基础参数设置
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="equiv">脉冲当量</param>
        /// <param name="startvel">起始速度</param>
        /// <param name="speed">运行速度</param>
        /// <param name="acc">加速度</param>
        /// <param name="dec">减速度</param>
        /// <param name="stopvel">停止速度</param>
        /// <param name="s_para">S段时间</param>
        /// <param name="posi_mode">运动模式 0：相对坐标模式，1：绝对坐标模式</param>
        /// <param name="stop_mode">制动方式 0：减速停止，1：紧急停止</param>
        void AxisBasicSet(ushort axis, double equiv, double startvel, double speed, double acc, double dec, double stopvel, double s_para, int posi_mode, int stop_mode);

        /// <summary>
        /// 轴JOG运动
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="speed">运行速度</param>
        /// <param name="posi_mode">运动方向，0：负方向，1：正方向</param>
        /// <param name="acc">加速度</param>
        /// <param name="dec">减速度</param>
        void AxisJog(ushort axis, double speed, int posi_mode = 0, double acc = 0.1, double dec = 0.1);

        /// <summary>
        /// 轴停止
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="stop_mode">停止方式 0=减速停止 1=紧急停止</param>
        /// <param name="all">是否全部轴停止</param>
        void AxisStop(ushort axis, int stop_mode, bool all);

        /// <summary>
        /// 轴绝对定位
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="position">定位地址</param>
        /// <param name="speed">定位速度</param>
        void AxisABS(ushort axis, double position, double speed);
        /// <summary>
        /// 轴相对定位
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="position">定位地址</param>
        /// <param name="speed">定位速度</param>
        void AxisRel(ushort axis, double position, double speed);

        object GetAxisState(ushort axis);

        object GetAxisExternalio(ushort axis);
    }
}
