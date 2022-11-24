using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MotionControl.MotionClass
{
    internal interface IControlBaseInterface
    {
        /// <summary>
        /// 数字io输入
        /// </summary>
        bool[] IO_Input { get; set; }

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
        /// 数据读取后台线程
        /// </summary>
        Thread Read_t1 { get; set; }

        /// <summary>
        /// 数据读取线程管理
        /// </summary>
        ManualResetEvent AutoReadEvent { get; set; }
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
        /// 所有轴使能
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
        /// 单轴JOG运动
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="speed">运行速度</param>
        /// <param name="posi_mode">运动方向，0：负方向，1：正方向</param>
        /// <param name="acc">加速度</param>
        /// <param name="dec">减速度</param>
        void MoveJog(ushort axis, double speed, int posi_mode = 0, double acc = 0.5, double dec = 0.5);

        /// <summary>
        /// 轴停止
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="stop_mode">停止方式 0=减速停止 1=紧急停止</param>
        /// <param name="all">是否全部轴停止</param>
        void AxisStop(ushort axis, int stop_mode, bool all);

        /// <summary>
        /// 轴复位停止前运动
        /// </summary>
        /// <param name="axis">轴号</param>
        void MoveReset(ushort axis);

        /// <summary>
        /// 单轴绝对定位（非阻塞模式，调用该方法后需要自行处理是否运动完成）
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="position">定位地址</param>
        /// <param name="speed">定位速度</param>
        void MoveAbs(ushort axis, double position, double speed);

        /// <summary>
        /// 单轴相对定位（非阻塞模式，调用该方法后需要自行处理是否运动完成）
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="position">定位地址</param>
        /// <param name="speed">定位速度</param>
        /// 
        void MoveRel(ushort axis, double position, double speed);

        /// <summary>
        /// 单轴绝对定位（阻塞模式，调用该方法后定位运动完成后或超时返回）
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="position">绝对地址</param>
        /// <param name="speed">定位速度</param>
        /// <param name="time">等待超时时长：0=一直等待直到定位完成</param>
        void AwaitMoveAbs(ushort axis, double position, double speed, int time);

        /// <summary>
        /// 单轴相对定位（阻塞模式，调用该方法后定位运动完成后或超时返回）
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <param name="position">相对地址</param>
        /// <param name="speed">定位速度</param>
        /// <param name="time">等待超时时长：0=一直等待直到定位完成</param>
        void AwaitMoveRel(ushort axis, double position, double speed, int time);

        /// <summary>
        /// 读取总线状态
        /// </summary>
        /// <param name="card_number">板卡号</param>
        /// <returns>int[0]=总线扫描时长us int[1]总线状态==0正常</returns>
        int[] GetEtherCATState(ushort card_number);

        /// <summary>
        /// 获取轴状态信息
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <returns>
        /// 返回值double[6] 数组
        ///double[0]= 位置
        ///double[1]= 伺服编码器位置
        ///double[2]= 速度
        ///double[3]= 目标位置
        ///double[4]= 轴状态机0：轴处于未启动状态 1：轴处于启动禁止状态 2：轴处于准备启动状态 3：轴处于启动状态 4：轴处于操作使能状态 5：轴处于停止状态 6：轴处于错误触发状态 7：轴处于错误状态
        ///double[5]= 轴运行模式0：空闲 1：Pmove 2：Vmove 3：Hmove 4：Handwheel 5：Ptt / Pts 6：Pvt / Pvts 10：Continue
        ///double[6]= 轴停止原因获取0：正常停止  3：LTC 外部触发立即停止  4：EMG 立即停止  5：正硬限位立即停止  6：负硬限位立即停止  7：正硬限位减速停止  8：负硬限位减速停止  9：正软限位立即停止  
        ///10：负软限位立即停止11：正软限位减速停止  12：负软限位减速停止  13：命令立即停止  14：命令减速停止  15：其它原因立即停止  16：其它原因减速停止  17：未知原因立即停止  18：未知原因减速停止
        /// </returns>
        double[] GetAxisState(ushort axis);

        /// <summary>
        /// 获取轴专用IO
        /// </summary>
        /// <param name="axis">轴号</param>
        /// <returns></returns>
        bool[] GetAxisExternalio(ushort axis);

        /// <summary>
        /// 获取板卡全部数字输入
        /// </summary>
        /// <param name="card">板卡号</param>
        /// <returns></returns>
        bool[] Getall_IOinput(ushort card);

        /// <summary>
        /// 获取板卡全部数字输出
        /// </summary>
        /// <param name="card">板卡号</param>
        /// <returns></returns>
        bool[] Getall_IOoutput(ushort card);

        /// <summary>
        /// 设置数字输出
        /// </summary>
        /// <param name="card">板卡号</param>
        /// <param name="indexes">输出口</param>
        /// <param name="value">输出值</param>
        void Set_IOoutput(ushort card, ushort indexes, bool value);

        /// <summary>
        /// 等待输入信号
        /// </summary>
        /// <param name="card">板卡号</param>
        /// <param name="indexes">输入口</param>
        /// <param name="waitvalue">等待状态</param>
        /// <param name="timeout">等待超时时间</param>
        void AwaitIOinput(ushort card, ushort indexes, bool waitvalue, int timeout);

        /// <summary>
        /// 外部IO单按钮触发事件设置
        /// </summary>
        /// <param name="card">外部输入触发板卡号</param>
        /// <param name="start">启动按钮输入点</param>
        /// <param name="reset">复位按钮输入点</param>
        /// <param name="stop">停止按钮输入点</param>
        /// <param name="estop">紧急停止按钮输入点</param>
        void SetExternalTrigger(ushort card, ushort start, ushort reset, ushort stop, ushort estop);

        /// <summary>
        /// 运动控制卡复位
        /// </summary>
        /// <param name="card">板卡号</param>
        /// <param name="reset">0=热复位 1=冷复位 2=初始复位</param>
        void ResetCard(ushort card, ushort reset);

    }
}
