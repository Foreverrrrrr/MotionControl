using System;
using System.Threading;
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
            motion = MotionBase.GetClassType(MotionBase.CardName.LeiSaiEtherCat);
            motion.FactorValue = 20;
            motion.CardErrorMessageEvent += (i, message) =>
            {
                MessageBox.Show(message);
            };
            motion.OpenCard();
            motion.SetExternalTrigger(8, 9, 0, 0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            motion.CardLogEvent += (i, ereeor, message) =>
            {

                this.Invoke(new Action(() =>
                {
                    listBox1.Items.Insert(0, message);
                }));
            };

            motion.StartPEvent += (t) =>
            {

                Task.Run(() =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        listBox1.Items.Insert(0, "启动按钮上升沿");
                    }));
                });
            };

            motion.StartNEvent += (t) =>
            {
                Task.Run(() =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        listBox1.Items.Insert(0, "启动按钮下降沿");
                    }));
                });

            };

            motion.ResetPEvent += (t) =>
            {
                Task.Run(() =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        listBox1.Items.Insert(0, "复位按钮上升沿");
                    }));
                });
            };

            motion.ResetNEvent += (t) =>
            {
                Task.Run(() =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        listBox1.Items.Insert(0, "复位按钮下降沿");
                    }));
                });
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
            motion.AxisStop(0, 1, true);
            motion.AxisStop(1, 1, false);
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
            motion.MoveAbs(0, 0, 200000, 0);
            motion.MoveAbs(1, 0, 200000, 0);

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
            Task.Run(() =>
            {
                motion.AwaitIOinput(0, 8, true, 0);
                motion.AwaitIOinput(0, 9, true, 0);
            });

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

                    motion.AwaitMoveAbs(0, 700000, 400000, 0);
                    motion.AwaitMoveAbs(0, 0, 400000, 0);

                }
            });
            Task.Run(() =>
            {
                while (true)
                {

                    motion.AwaitMoveAbs(1, 100000, 100000, 0);
                    motion.AwaitMoveAbs(1, 0, 100000, 0);

                }
            });
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                //while (true)
                //{
                //Thread.Sleep(500);
                motion.AwaitMoveRel(0, 50000, 500000, 0);
                //Thread.Sleep(500);
                motion.AwaitMoveRel(0, -20000, 100000, 0);
                //}

            });
            Task.Run(() =>
            {
                //while (true)
                //{
                //Thread.Sleep(500);
                motion.AwaitMoveRel(1, 20000, 600000, 0);
                //Thread.Sleep(500);
                motion.AwaitMoveRel(1, -50000, 100000, 0);
                //}

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

        private Thread thread;
        private void button16_Click(object sender, EventArgs e)
        {
            thread = new Thread(Movlin);
            thread.IsBackground = true;
            thread.Start();
        }

        private void Movlin()
        {
            try
            {
                while (true)
                {
                    //Thread.Sleep(1000);
                    motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { -100000, 0 }, Speed = 200000, locationModel = 1 });
                    //Thread.Sleep(1000);
                    motion.AwaitMoveLines(0, new MotionBase.ControlState { Acc = 0.5, Dcc = 0.5, UsingAxisNumber = 2, Axis = new ushort[] { 0, 1 }, Position = new double[] { 0, -100000 }, Speed = 200000, locationModel = 1 });

                }
            }
            catch (ThreadAbortException ex)
            {
                motion.AxisStop(0, 1, true);
                return;
            }
            catch (Exception ex)
            {
                return;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            motion.AxisOff();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            thread.Abort();
        }
    }
}
