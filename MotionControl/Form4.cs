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
    public partial class Form4 : Form
    {
        private MotionBase motion;

        public Form4()
        {
            InitializeComponent();
            motion = MotionBase.GetClassType(MotionBase.CardName.LeiSaiPulse_1000B);
            motion.Axisquantity = 8;
            motion.FactorValue = 20;
            motion.CardErrorMessageEvent += (i, message) =>
            {
                Console.WriteLine(i.ToString(), message);
            };
           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            motion.OpenCard();
        }
    }
}
