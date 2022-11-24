using MotionControl.MotionClass;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
            motion.CardErrorMessageEvent += (i, message) =>
            {
                Console.WriteLine(i.ToString(),message);
            };
            motion.OpenCard();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

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

        private void button3_Click(object sender, EventArgs e)
        {
            motion.MoveJog(0, 100000, 0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            motion.MoveJog(0, 100000, 1);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            textBox1.Text = motion.AxisStates[4].ToString();
            textBox2.Text = motion.AxisStates[5].ToString();
            textBox3.Text = motion.AxisStates[1].ToString();
            textBox4.Text = motion.AxisStates[0].ToString();
            textBox5.Text = motion.AxisStates[3].ToString();
            textBox6.Text = motion.AxisStates[2].ToString();
        }
    }
}
