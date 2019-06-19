using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;


namespace ServersView.Формы
{
    /// <summary>
    /// Форма для отображения архивных машин.
    /// </summary>
    public partial class Archive : DevExpress.XtraEditors.XtraForm
    {
        DataSet DataSet = new DataSet();
        BindingSource main = new BindingSource();
        string command = "SELECT ID, ParrentServerID, ServerName, IP, Domain, Description FROM dbo.ArchiveList";
        
        /// <summary>
        /// Инициализация компонентов формы.
        /// </summary>
        public Archive()
        {
            InitializeComponent();
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.UserSkins.BonusSkins.Register();
        }

        /// <summary>
        /// Загрузить данные в случае подключение.
        /// </summary>
        public void GetData()
        {
            DataSet.Locale = System.Globalization.CultureInfo.InvariantCulture;
            using (SqlConnection cn = new SqlConnection(DB.Connection))
            {
                cn.ConnectionString = DB.Connection;
                try
                {
                    cn.Open();
                    SqlCommand sCommand = new SqlCommand(command, cn);
                    SqlDataAdapter Adapter = new SqlDataAdapter(sCommand);
                    Adapter.Fill(DataSet, "ArchiveList");

                    main.DataSource = DataSet;
                    main.DataMember = "ArchiveList";
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("Не создано подключение к базе данных\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            DataSet.Dispose();
        }

        /// <summary>
        /// Загрузка формы с асихронным вызовом делегата по заполнению из базы в родительском потоке.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Archive_Load(object sender, EventArgs e)
        {
            dataGridView1.DataSource = main;
            dataGridView1.BeginInvoke(new Action(() => 
            {
                GetData();
                SetWidth();
            }));
        }

        /// <summary>
        /// Размер столбцов
        /// </summary>
        private void SetWidth()
        {
            dataGridView1.Columns[0].Width = 24;
            dataGridView1.Columns[1].Width = 110;
            dataGridView1.Columns[2].Width = 120;
            dataGridView1.Columns[3].Width = 120;
            dataGridView1.Columns[4].Width = 100;
            dataGridView1.Columns[5].Width = 350;
        }
    }
}
