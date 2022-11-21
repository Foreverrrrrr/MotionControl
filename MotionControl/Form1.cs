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
        public Form1()
        {
            InitializeComponent();
            var a= MotionBase.GetClassType(MotionBase.CardName.LeiSai);
            a.CardErrorMessageEvent += (i, message) =>
            {
                Console.WriteLine(i.ToString(),message);
            };
            a.CardErrorMessage(1);
            a.OpenCard(new ushort[0]);
        }
    }
}
