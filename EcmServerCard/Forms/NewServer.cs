using System;
using System.Windows.Forms;

namespace ServersView
{
    public partial class NewServer : Form
    {
        /// <summary>
        /// Флаг для передачи или непередачи значений в главную форму.
        /// </summary>
        public bool result;

        public NewServer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// При простом закрытии формы передаем флаг с указанияем запрета передачи значений.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cancel_Click(object sender, EventArgs e)
        {
            result = false;
            this.Close();
        }

        /// <summary>
        /// Передаем значения в главную форму.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void Add_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked) { textBox2.Text = string.Empty; }
            if (checkBox2.Checked) { textBox3.Text = string.Empty; }
            if (checkBox3.Checked) { textBox4.Text = string.Empty; }
            if (checkBox4.Checked) { textBox4.Text = string.Empty; }
            result = true;
            this.Close();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            { textBox2.Enabled = false; }
            else {textBox2.Enabled = true;}
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            { textBox3.Enabled = false; }
            else { textBox3.Enabled = true; }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            { textBox4.Enabled = false; }
            else { textBox4.Enabled = true; }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            { textBox4.Enabled = false; }
            else { textBox4.Enabled = true; }
        }
    }
}
