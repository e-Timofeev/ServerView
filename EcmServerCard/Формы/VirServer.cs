using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace EcmServerCard
{
    public partial class VirServer : EcmServerCard.NewServer
    {
        #region Поля
        /// <summary>
        /// [dbo].[PhyServers].[ID] полученный при выборе сервера в ComboBox.
        /// </summary>
        public byte CurrentServerID = 0;
        private string connection = DB.Connection();
        #endregion

        #region Методы
        /// <summary>
        /// Выводит список серверов в выпадающем списке
        /// </summary>
        /// <param name="main"></param>
        public void GetData(BindingSource main)
        {
            DataSet DataSet = new System.Data.DataSet();
            DataSet.Locale = System.Globalization.CultureInfo.InvariantCulture;
            using (SqlConnection cn = new SqlConnection(connection))
            {
                string command = "Select ID, ServerName from PhyServers";

                cn.ConnectionString = connection;
                try
                {
                    cn.Open();
                    SqlCommand sCommand = new SqlCommand(command, cn);
                    SqlDataAdapter Adapter = new SqlDataAdapter(sCommand);
                    Adapter.Fill(DataSet, "PhyServers");
                    main.DataSource = DataSet;
                    main.DataMember = "PhyServers";
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("Не создано подключение к базе данных\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            DataSet.Dispose();
        }
        #endregion

        /// <summary>
        /// Инициализация формы.
        /// </summary>
        public VirServer()
        {
            InitializeComponent();
        }

        #region Обработчики
        /// <summary>
        /// Загрузка формы. Загружаем данные с сервера.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VirServer_Shown(object sender, EventArgs e)
        {
            this.Refresh();
            GetData(bindingSource1);
            comboBox1.ValueMember = "ServerName";
            this.Load -= comboBox1_SelectedIndexChanged;            // отписываемся от события
        }
       
        /// <summary>
        /// При выборе сервера в comboBox получаем его ID из базы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataSet DataSet = new System.Data.DataSet();
            int index = comboBox1.SelectedIndex;
            using (SqlConnection cn = new SqlConnection())
            {
                cn.ConnectionString = connection;
                try
                {
                    cn.Open();
                    SqlCommand sCommand = new SqlCommand("Select ID, ServerName from PhyServers", cn);
                    SqlDataAdapter Adapter = new SqlDataAdapter(sCommand);
                    Adapter.Fill(DataSet, "PhyServers");
                    DataTable work = DataSet.Tables[0];
                    CurrentServerID = work.Rows[index].Field<byte>(work.Columns[0]);
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("Проблема в подключении\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            DataSet.Dispose();
        }
        #endregion

        /// <summary>
        /// Переопределенный метод добавления сервера.
        /// </summary>
        public override void Add_Click(object sender, EventArgs e)
        {
            {
                if (checkBox1.Checked) { textBox2.Text = string.Empty; }
                if (checkBox2.Checked) { textBox3.Text = string.Empty; }
                if (checkBox3.Checked) { textBox4.Text = string.Empty; }
                if (checkBox4.Checked) { textBox4.Text = string.Empty; }
            }
            if (comboBox1.Text != string.Empty)
            {
                result = true;
                Close();
            }
            else
            {
                MessageBox.Show("Не выбран родительский сервер!", "ECMServersActive", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);
                return;
            }
        }


    }
}
