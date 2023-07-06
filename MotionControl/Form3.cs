using SQLiteHelper;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MotionControl
{
    public partial class Form3 : Form
    {
        MotionBase motion;
        public Form3()
        {
            InitializeComponent();
            motion = MotionBase.GetClassType(MotionBase.CardName.LeiSaiPulse);
            motion.FactorValue = 20;
            motion.OpenCard();
            motion.AxisOn();
            motion.SetExternalTrigger(0, 1, 2, 3, 4, 5, 6);
            motion.StartNEvent += (x) =>
            {
                Console.WriteLine("启动");
            };

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

                textBox6.Text = motion.AxisStates[0][3].ToString();//轴速度

                textBox7.Text = motion.AxisStates[0][7].ToString();//轴停止原因

                textBox8.Text = motion.AxisStates[0][5].ToString();//轴状态




                textBox11.Text = motion.AxisStates[1][4].ToString();//运动到位

                textBox10.Text = motion.AxisStates[1][6].ToString();//运动模式

                textBox15.Text = motion.AxisStates[1][1].ToString();//轴编码器位置

                textBox17.Text = motion.AxisStates[1][0].ToString();//轴位置

                textBox18.Text = motion.AxisStates[1][2].ToString();//轴目标位置

                textBox13.Text = motion.AxisStates[1][3].ToString();//轴速度

                textBox12.Text = motion.AxisStates[1][7].ToString();//轴停止原因

                textBox16.Text = motion.AxisStates[1][5].ToString();//轴状态



                textBox20.Text = motion.AxisStates[2][4].ToString();

                textBox21.Text = motion.AxisStates[2][0].ToString();//轴位置

                textBox19.Text = motion.AxisStates[2][6].ToString();//运动模式

                textBox26.Text = motion.AxisStates[2][3].ToString();//轴速度

                textBox22.Text = motion.AxisStates[2][2].ToString();//轴目标位置

                textBox23.Text = motion.AxisStates[1][5].ToString();//轴状态
                textBox27.Text = motion.AxisStates[1][7].ToString();//轴停止原因
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            motion.AxisOn();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            motion.AxisOff();
        }

        private void button5_MouseDown(object sender, MouseEventArgs e)
        {
            motion.MoveJog(1, 10000, 1);
        }

        private void button5_MouseUp(object sender, MouseEventArgs e)
        {
            motion.AxisStop(1, 0, false);
        }

        private void button6_MouseDown(object sender, MouseEventArgs e)
        {
            motion.MoveJog(1, 10000, 0);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            for (ushort i = 0; i < motion.Axis.Length; i++)
            {
                motion.AxisReset(i);
            }

        }

        private void Form3_Load(object sender, EventArgs e)
        {
            motion.CardLogEvent += (i, ereeor, message) =>
            {

                this.Invoke(new Action(() =>
                {
                    listBox1.Items.Insert(0, message);
                }));
            };
        }

        private void button7_Click(object sender, EventArgs e)
        {
            motion.MoveAbs(1, 0, 200000);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                motion.AwaitMoveAbs(1, 10000, 10000);
                motion.AwaitMoveAbs(1, 0, 20000);
                motion.AwaitMoveAbs(1, 20000, 60000);
            });

        }

        private void button21_Click(object sender, EventArgs e)
        {
            motion.MoveRel(1, 100000, 100000);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            motion.MoveHome(1, 2, 1000);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            motion.AwaitMoveHome(1, 2, 1000);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            motion.AxisStop(1, 1, false);
        }

        private void button22_Click(object sender, EventArgs e)
        {
            motion.MoveLines(0, new MotionBase.ControlState { UsingAxisNumber = 2, Position = new double[] { 50000, 50000 }, Axis = new ushort[] { 1, 2 }, Acc = 0.5, Dcc = 0.001, Speed = 500000, locationModel = 0 });
        }

        private void button24_Click(object sender, EventArgs e)
        {
            motion.AxisStop(1, 0, true);
        }

        private void button25_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                motion.MoveReset(1);


            });
        }

        private void button26_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                motion.AwaitMoveLines(0, new MotionBase.ControlState { UsingAxisNumber = 2, Position = new double[] { 90000, 50000 }, Axis = new ushort[] { 1, 2 }, Acc = 0.05, Dcc = 0.001, Speed = 500000, locationModel = 1 });


            });
        }
        Thread thread;
        private void button27_Click(object sender, EventArgs e)
        {
            thread = new Thread(mod);

            thread.IsBackground = true;
            thread.Start();


        }

        private void mod()
        {
            while (true)
            {
                motion.AwaitMoveAbs(1, 0, 100000);
                motion.AwaitMoveAbs(2, 0, 100000);
                motion.AwaitMoveAbs(1, 100000, 200000);
                motion.AwaitMoveAbs(2, -100000, 100000);
                for (int i = 0; i < 10; i++)
                {
                    motion.AwaitMoveRel(1, 10000, 50000);
                    motion.AwaitMoveRel(2, 10000, 50000);
                }
                motion.AwaitMoveLines(0, new MotionBase.ControlState { UsingAxisNumber = 2, Position = new double[] { 100000, 100000 }, Axis = new ushort[] { 1, 2 }, Acc = 0.05, Dcc = 0.001, Speed = 500000, locationModel = 1 });
                for (int i = 1; i < 10; i++)
                {
                    motion.AwaitMoveLines(0, new MotionBase.ControlState { UsingAxisNumber = 2, Position = new double[] { 100000, 100000 }, Axis = new ushort[] { 1, 2 }, Acc = 0.05, Dcc = 0.001, Speed = 20000 * i, locationModel = 0 });

                }
            }
        }

        private void button28_Click(object sender, EventArgs e)
        {
            thread.Abort();
        }

        private void button29_Click(object sender, EventArgs e)
        {

            motion.Set_IOoutput(0, 1, false);
        }

        private void button30_Click(object sender, EventArgs e)
        {
            motion.Set_IOoutput(0, 1, true);
        }

        private void button31_Click(object sender, EventArgs e)
        {
            motion.AwaitIOinput(0, 0, true);
        }

        private void button32_Click(object sender, EventArgs e)
        {
            motion.AwaitIOinput(0, 0, false);
        }
    }
}
