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
    public partial class Form2 : Form
    {
        private MotionBase motion;
        public Form2()
        {
            InitializeComponent();
            motion = MotionBase.GetClassType(MotionBase.CardName.MoShengTai);
            motion.Axisquantity = 8;
            motion.Card_Number = new ushort[] { (ushort)MoShengTai.ModelType.NMC5800_5600_1800_1600R, (ushort)MoShengTai.ModelType.NIO4832_3232 };
            motion.FactorValue = 20;
            motion.CardErrorMessageEvent += (i, message) =>
            {
                Console.WriteLine(i.ToString(), message);
            };
        }

        private void Form2_Load(object sender, EventArgs e)
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

        private void button1_Click(object sender, EventArgs e)
        {
            motion.OpenCard();
            motion.SetAxis_iniFile();
        }
    }
}
