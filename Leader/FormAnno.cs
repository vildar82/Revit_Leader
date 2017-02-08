using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Leader
{
    public partial class FormAnno : Form
    {
        static Point location;

        public FormAnno()
        {
            InitializeComponent();
            KeyDown += FormAnno_KeyDown;
            Activated += FormAnno_Activated;
            FormClosed += FormAnno_FormClosed;
            textBox1.Text = Text1;
            textBox2.Text = Text2;            
        }

        private void FormAnno_FormClosed(object sender, FormClosedEventArgs e)
        {
            location = Location;
        }

        private void FormAnno_Activated(object sender, EventArgs e)
        {
            if (location.IsEmpty) return;
            Location = location;
        }

        private void FormAnno_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                DialogResult = DialogResult.OK;
                bInsert_Click(null, null);
                Close();
            }
        }

        public static string Text1 { get; set; } = "Строка 1";
        public static string Text2 { get; set; }= "Строка 2";

        private void bInsert_Click(object sender, EventArgs e)
        {
            Text1 = GetText (textBox1.Text);
            Text2 = GetText(textBox2.Text);            
        }

        private string GetText(string text)
        {
            if (string.IsNullOrEmpty(text)) return " ";
            return text;
        }
    }
}
