using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MotionControl
{
    public partial class Form2 : Form
    {
        private MotionBase motion;
        public Form2()
        {
            InitializeComponent();

            motion = MotionBase.GetClassType(MotionBase.CardName.MoShengTai);
            motion.Axisquantity = 8;
            motion.Card_Number = new ushort[] { (ushort)MoShengTai.ModelType.NMC5160_5120 };
            motion.FactorValue = 20;
            motion.CardErrorMessageEvent += (i, message) =>
            {
                Console.WriteLine(i.ToString(), message);
            };
            motion.OpenCard();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            motion.CardLogEvent += (i, error, message) =>
            {
                Console.WriteLine(i.ToString(), message);
                this.Invoke(new Action(() =>
                {
                    listBox1.Items.Insert(0, message);
                }));
            };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            motion.OpenCard();
            //motion.SetAxis_iniFile();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            motion.AxisOn();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            motion.AxisOff();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (motion.IsOpenCard)
            {
                textBox1.Text = motion.AxisStates[0][4].ToString();//运动到位

                textBox2.Text = motion.AxisStates[0][6].ToString();//运动模式

                textBox3.Text = motion.AxisStates[0][1].ToString();//轴编码器位置

                textBox4.Text = motion.AxisStates[0][0].ToString();//轴位置

                textBox5.Text = motion.AxisStates[0][2].ToString();//轴目标位置

                textBox6.Text = motion.AxisStates[0][2].ToString();//轴速度

                textBox7.Text = motion.AxisStates[0][7].ToString();//轴停止原因

                textBox8.Text = motion.AxisStates[0][5].ToString();//轴状态




                textBox11.Text = motion.AxisStates[1][4].ToString();//运动到位

                textBox10.Text = motion.AxisStates[1][6].ToString();//运动模式

                textBox15.Text = motion.AxisStates[1][1].ToString();//轴编码器位置

                textBox17.Text = motion.AxisStates[1][0].ToString();//轴位置

                textBox18.Text = motion.AxisStates[1][2].ToString();//轴目标位置

                textBox13.Text = motion.AxisStates[1][2].ToString();//轴速度

                textBox12.Text = motion.AxisStates[1][7].ToString();//轴停止原因

                textBox16.Text = motion.AxisStates[1][5].ToString();//轴状态
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            motion.MoveJog(1, 10000, 1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            motion.AxisStop(0, 0, false);
            motion.AxisStop(1, 0, false);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            motion.MoveJog(1, -10000, 1);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            motion.MoveAbs(1, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));
        }

        private void button15_Click(object sender, EventArgs e)
        {
            motion.MoveJog(0, 10000, 1);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            motion.MoveJog(0, -10000, 1);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            motion.MoveAbs(0, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));
        }

        private void button12_Click(object sender, EventArgs e)
        {
            motion.AxisStop(0, 0, false);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            motion.AxisStop(1, 0, false);
        }

        private void textBox29_TextChanged(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            for (ushort i = 0; i < motion.Axis.Length; i++)
            {
                motion.AxisReset(i);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            motion.AwaitMoveAbs(1, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));
        }

        private void button11_Click(object sender, EventArgs e)
        {
            motion.AwaitMoveAbs(0, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));

        }

        private void button20_Click(object sender, EventArgs e)
        {
            motion.MoveRel(0, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));
        }

        private void button21_Click(object sender, EventArgs e)
        {
            motion.MoveRel(1, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));
        }

        private void button22_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    motion.AwaitMoveRel(0, 100000, 100000);
                    motion.AwaitMoveRel(1, 50000, 100000);
                    //motion.MoveRel(1, 100000, 100000);
                    //motion.MoveAbs(0, 0, 100000);
                    //motion.MoveAbs(1, 0, 100000);
                    motion.AwaitMoveAbs(0, 100000, 500000);
                    motion.AwaitMoveAbs(0, 0, 500000);
                    motion.AwaitMoveAbs(1, 50000, 500000);
                    motion.AwaitMoveRel(1, -50000, 500000);
                    motion.AwaitMoveAbs(1, 0, 500000);
                    motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.2, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { -200000, -200000 }, Speed = 300000, locationModel = 0 });
                    motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.2, Dcc = 0.2, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { -50000, 200000 }, Speed = 300000, locationModel = 1 });
                }
            });
        }

        private void button23_Click(object sender, EventArgs e)
        {
            motion.MoveHome(0, 20, 100000);
        }

        private void button24_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                motion.MoveReset(0);
                motion.MoveReset(1);
            });
        }

        private void button25_Click(object sender, EventArgs e)
        {
            motion.MoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { -100000, 0 }, Speed = 50000, locationModel = 1 });
        }

        private void button26_Click(object sender, EventArgs e)
        {
            motion.MoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { -30000, 50000 }, Speed = 20000, locationModel = 0 });
        }

        private void button27_Click(object sender, EventArgs e)
        {
            motion.AxisStop(0, 1, true);
        }

        private void button28_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 3, Axis = new ushort[] { 0, 1 ,2}, Position = new double[] { 100000, 100000 ,100000}, Speed = 40000, locationModel = 1 });
            });
        }

        private void button29_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { 100000, 100000 }, Speed = 70000, locationModel = 0 });
            });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            motion.AxisOn();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            motion.AxisOff();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (motion.IsOpenCard)
            {
                textBox1.Text = motion.AxisStates[0][4].ToString();//运动到位

                textBox2.Text = motion.AxisStates[0][6].ToString();//运动模式

                textBox3.Text = motion.AxisStates[0][1].ToString();//轴编码器位置

                textBox4.Text = motion.AxisStates[0][0].ToString();//轴位置

                textBox5.Text = motion.AxisStates[0][2].ToString();//轴目标位置

                textBox6.Text = motion.AxisStates[0][2].ToString();//轴速度

                textBox7.Text = motion.AxisStates[0][7].ToString();//轴停止原因

                textBox8.Text = motion.AxisStates[0][5].ToString();//轴状态




                textBox11.Text = motion.AxisStates[1][4].ToString();//运动到位

                textBox10.Text = motion.AxisStates[1][6].ToString();//运动模式

                textBox15.Text = motion.AxisStates[1][1].ToString();//轴编码器位置

                textBox17.Text = motion.AxisStates[1][0].ToString();//轴位置

                textBox18.Text = motion.AxisStates[1][2].ToString();//轴目标位置

                textBox13.Text = motion.AxisStates[1][2].ToString();//轴速度

                textBox12.Text = motion.AxisStates[1][7].ToString();//轴停止原因

                textBox16.Text = motion.AxisStates[1][5].ToString();//轴状态
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            motion.MoveJog(1, 20000, 1);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            motion.AxisStop(0, 0, false);
            motion.AxisStop(1, 0, false);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            motion.MoveJog(1, -20000, 1);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            motion.MoveAbs(1, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));
        }

        private void button15_Click(object sender, EventArgs e)
        {
            motion.MoveJog(0, 20000, 1);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            motion.MoveJog(0, -20000, 1);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            motion.MoveAbs(0, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));
        }

        private void button12_Click(object sender, EventArgs e)
        {
            motion.AxisStop(0, 0, false);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            motion.AxisStop(1, 0, false);
        }

        private void textBox29_TextChanged(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            for (ushort i = 0; i < motion.Axis.Length; i++)
            {
                motion.AxisReset(i);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            motion.AwaitMoveAbs(1, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));
        }

        private void button11_Click(object sender, EventArgs e)
        {
            motion.AwaitMoveAbs(0, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));

        }

        private void button20_Click(object sender, EventArgs e)
        {
            motion.MoveRel(0, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));
        }

        private void button21_Click(object sender, EventArgs e)
        {
            motion.MoveRel(1, Convert.ToDouble(textBox29.Text), Convert.ToDouble(textBox28.Text));
        }

        private void button22_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    motion.AwaitMoveRel(0, 100000, 100000);
                    motion.AwaitMoveRel(1, 50000, 100000);
                    //motion.MoveRel(1, 100000, 100000);
                    //motion.MoveAbs(0, 0, 100000);
                    //motion.MoveAbs(1, 0, 100000);
                    motion.AwaitMoveAbs(0, 100000, 500000);
                    motion.AwaitMoveAbs(0, 0, 500000);
                    motion.AwaitMoveAbs(1, 50000, 500000);
                    motion.AwaitMoveRel(1, -50000, 500000);
                    motion.AwaitMoveAbs(1, 0, 500000);
                    motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.2, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { -200000, -200000 }, Speed = 300000, locationModel = 0 });
                    motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.2, Dcc = 0.2, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { -50000, 200000 }, Speed = 300000, locationModel = 1 });
                }
            });
        }

        private void button23_Click(object sender, EventArgs e)
        {
            motion.MoveHome(0, 20, 10000);
        }

        private void button24_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                motion.MoveReset(0);
                motion.MoveReset(1);
            });
        }

        private void button25_Click(object sender, EventArgs e)
        {
            motion.MoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { -100000, 0 }, Speed = 50000, locationModel = 1 });
        }

        private void button26_Click(object sender, EventArgs e)
        {
            motion.MoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { -30000, 50000 }, Speed = 20000, locationModel = 0 });
        }

        private void button27_Click(object sender, EventArgs e)
        {
            motion.AxisStop(0, 1, true);
        }

        private void button28_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 3, Axis = new ushort[] { 0, 1 ,2}, Position = new double[] { 100000, 100000 ,100000}, Speed = 40000, locationModel = 1 });
            });
        }

        private void button29_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { 100000, 100000 }, Speed = 70000, locationModel = 0 });
            });
        }
    }
}
