using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MotionControl
{
    public partial class Form1 : Form
    {
        private MotionBase motion;
        public Form1()
        {
            InitializeComponent();
            motion = MotionBase.GetClassType(MotionBase.CardName.LeiSai);
            motion.FactorValue = 20;
            motion.CardErrorMessageEvent += (i, message) =>
            {
                Console.WriteLine(i.ToString(), message);
            };
            motion.OpenCard();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            motion.CardLogEvent += (i, ereeor, message) =>
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
            motion.AxisOn();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (motion.AxisStates != null)
            {
                textBox1.Text = motion.AxisStates[0][4].ToString();
                textBox2.Text = motion.AxisStates[0][6].ToString();
                textBox3.Text = motion.AxisStates[0][1].ToString();
                textBox4.Text = motion.AxisStates[0][0].ToString();
                textBox5.Text = motion.AxisStates[0][2].ToString();
                textBox6.Text = motion.AxisStates[0][3].ToString();
                textBox7.Text = motion.AxisStates[0][7].ToString();
                textBox8.Text = motion.AxisStates[0][5].ToString();
                textBox9.Text = motion.CoordinateSystemStates[0].ToString();
            }
        }

        private void button3_MouseUp(object sender, MouseEventArgs e)
        {
            motion.AxisStop(0, 0,false);
            motion.AxisStop(1, 0, false);
        }

        private void button3_MouseDown(object sender, MouseEventArgs e)
        {
            motion.MoveJog(0, 10000, 0);
            motion.MoveJog(1, 10000, 0);
        }

        private void button4_MouseDown(object sender, MouseEventArgs e)
        {
            motion.MoveJog(0, 10000, 1);
            motion.MoveJog(1, 10000, 1);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            motion.MoveAbs(0, 10000, 2000000, 0);
            motion.MoveAbs(1, 10000, 1000000, 0);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            motion.MoveRel(0, 500000, 2000000, 0);
            motion.MoveRel(1, 500000, 1000000, 0);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            motion.Set_IOoutput(0, 0, true);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            motion.AwaitIOinput(0, 0, true, 3000);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            motion.ResetCard(0, 0);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            motion.MoveReset(0);
            motion.MoveReset(1);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        motion.AwaitMoveAbs(0, 7000000, 4000000, 0);
                        motion.AwaitMoveAbs(0, 0, 4000000, 0);
                    }
                    catch (Exception)
                    {
                        return;
                        throw;
                    }
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        motion.AwaitMoveAbs(1, 1000000, 1000000, 0);
                        motion.AwaitMoveAbs(1, 0, 1000000, 0);
                    }
                    catch (Exception)
                    {
                        return;
                        throw;
                    }
                }
            });
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                motion.AwaitMoveRel(0, 10000, 50000, 0);
                motion.AwaitMoveRel(0, 20000, 10000, 0);
            });
            Task.Run(() =>
            {
                motion.AwaitMoveRel(1, 20000, 60000, 0);
                motion.AwaitMoveRel(1, 10000, 10000, 0);
            });
        }

        private void button13_Click(object sender, EventArgs e)
        {
            motion.MoveHome(0, 27, 10000);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            motion.AxisStop(0, 1, true);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            for (ushort i = 0; i < motion.Axis.Length; i++)
            {
                motion.AxisReset(i);
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { -1000000, 0 }, Speed = 10000000, locationModel = 1 });
                    motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { 0, -1000000 }, Speed = 10000000, locationModel = 1});

                }

            });
        }
    }
}
