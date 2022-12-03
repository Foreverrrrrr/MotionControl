using MotionControl;
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
    public partial class Form1 : Form
    {
        MotionBase motion;
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
            motion.CardLogEvent += (i, message) =>
            {
                Console.WriteLine(i.ToString(), message);
                this.Invoke(new Action(() =>
                {
                    listBox1.Items.Insert(0, message);
                }));

            };
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            motion.AxisOn();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            textBox1.Text = motion.AxisStates[0][4].ToString();
            textBox2.Text = motion.AxisStates[0][5].ToString();
            textBox3.Text = motion.AxisStates[0][1].ToString();
            textBox4.Text = motion.AxisStates[0][0].ToString();
            textBox5.Text = motion.AxisStates[0][2].ToString();
            textBox6.Text = motion.AxisStates[0][3].ToString();
            textBox7.Text = motion.AxisStates[0][6].ToString();
        }

        private void button3_MouseUp(object sender, MouseEventArgs e)
        {
            motion.AxisStop(0, 0,false);
        }

        private void button3_MouseDown(object sender, MouseEventArgs e)
        {
            motion.MoveJog(0, 100000, 0);
        }

        private void button4_MouseDown(object sender, MouseEventArgs e)
        {
            motion.MoveJog(0, 100000, 1);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //motion.MoveAbs(0, 1000, 10000);
            
            motion.MoveAbs((ushort)LeiSai.CardOne.X, 10000, 10000, 0);

        }

        private void button6_Click(object sender, EventArgs e)
        {
            motion.MoveRel(0, 100000, 10000,0);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            motion.Set_IOoutput(0, 0, true);
        }

        private void button8_Click(object sender, EventArgs e)
        {
           
            motion.AwaitIOinput(0, 0, true,3000);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            motion.ResetCard(0, 0);
            
        }

        private void button10_Click(object sender, EventArgs e)
        {
            motion.MoveReset(0);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            motion.AwaitMoveAbs(0, 10000, 10000, 0);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            motion.AwaitMoveRel(0, 10000, 10000, 0);
        }
    }
}
