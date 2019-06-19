using ServersView.Формы;

using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ServersView
{
    public partial class Main : DevExpress.XtraEditors.XtraForm
    {
        #region Поля
        /// <summary>
        /// Индекс выделенной строки грида физических серверов.
        /// </summary>
        private byte PhyRowID = 0;

        /// <summary>
        /// Индекс выделенной строки грида виртуальных серверов.
        /// </summary>
        private byte VirRowID = 0;

        /// <summary>
        /// ID выделенного сервера (PK в PhyServers).
        /// </summary>
        private byte PhyServerID = 0;

        /// <summary>
        /// ID выделенного сервера (PK в VirServers).
        /// </summary>
        private byte VirServerID = 0;
        private string connection = DB.Connection;
        private string comPhy = DB.CommandMain;
        private string comVir = DB.CommandDetail;

        #endregion
        /// <summary>
        /// Точка входа
        /// </summary>
        public Main()
        {
            InitializeComponent();
            DevExpress.Skins.SkinManager.EnableFormSkins();
            DevExpress.UserSkins.BonusSkins.Register();
        }

        #region Вспомогательные методы

        /// <summary>
        /// Очистка таблицы для корректного обновления содержимого таблиц.
        /// </summary>
        /// <param name="grid">Сам контрол</param>
        /// <param name="binding">Источник привязки данных</param>
        private void ClearDatagridview(DataGridView grid, BindingSource binding)
        {

            try
            {
                binding = null;
                grid.DataSource = null;
                grid.Rows.Clear();
                grid.Columns.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось очистить datagridview: " + grid.Name + " \n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        /// <summary>
        /// Устанавливает нужные свойства для таблиц. Используется при загрузке и обновлении данных.
        /// </summary>
        private void SetGridProperties()
        {
            dataGridView1.AutoGenerateColumns = true;
            dataGridView3.AutoGenerateColumns = true;

            dataGridView1.Columns[6].Visible = false;
            dataGridView3.Columns[6].Visible = false;
            dataGridView3.Columns[7].Visible = false;

            dataGridView1.Columns[0].Width = 25;
            dataGridView1.Columns[4].Width = 200;

            dataGridView3.Columns[0].Width = 25;
            dataGridView3.Columns[4].Width = 200;
        }

        /// <summary>
        /// Последовательно выполняет очистку и повторную загрузку данных из базы.
        /// </summary>
        private void UpdateAllDataGridView()
        {
            BugSelectRowInDatagridview(dataGridView3);

            try
            {
                ClearDatagridview(dataGridView1, bindingSource1);
                ClearDatagridview(dataGridView3, bindingSource2);

                bindingSource1 = new BindingSource();
                bindingSource2 = new BindingSource();

                dataGridView1.DataSource = bindingSource1;
                dataGridView3.DataSource = bindingSource2;

                BeginInvoke(new Action(() =>
                {
                    DB.GetDataCompleted += SetGridProperties;
                    DB.GetData(bindingSource1, bindingSource2);
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось выполнить апдейт таблицы в методе UpdateAllDataGridView\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        /// <summary>
        /// Отображение данных выделенной строки для удобства редактирования.
        /// </summary>
        private void FilterGrid(DataGridView grid)
        {
            if (grid.Rows.Count != 0)
            {
                textBox1.Text = grid[1, grid.CurrentRow.Index].Value.ToString(); // получаем имя сервера
                textBox2.Text = grid[2, grid.CurrentRow.Index].Value.ToString(); // получаем адрес сервера
                textBox3.Text = grid[3, grid.CurrentRow.Index].Value.ToString(); // получаем домен сервера
                textBox4.Text = grid[4, grid.CurrentRow.Index].Value.ToString(); // получаем описание сервера
            }
        }

        /// <summary>
        /// Обновление грида из заполненных текстбоксов. Если у них пустые значения, то записывается пустая строка.
        /// </summary>
        /// <param name="grid">Нужный грид</param>
        /// <param name="index">Индекс строки, который был выделении</param>
        private void UpdateGridAdapter(DataGridView grid, byte index)
        {
            if (grid.Rows.Count != 0)
            {
                grid.EndEdit();
                if (textBox1.Text != "") { grid[1, index].Value = textBox1.Text; }
                else { grid[1, index].Value = string.Empty; }
                if (textBox2.Text != "") { grid[2, index].Value = textBox2.Text; }
                else { grid[2, index].Value = string.Empty; }
                if (textBox3.Text != "") { grid[3, index].Value = textBox3.Text; }
                else { grid[3, index].Value = string.Empty; }
                if (textBox4.Text != "") { grid[4, index].Value = textBox4.Text; }
                else { grid[4, index].Value = string.Empty; }
            }
        }

        /// <summary>
        /// Меняем режим выделения строки на выделение ячейки, иначе некорректно работает получения значения для VirServerID.
        /// Баг datagridview. 
        /// </summary>
        private void BugSelectRowInDatagridview(DataGridView grid)
        {
            if (grid.DataSource != null && grid.Rows.Count > 0)
            {
                if (grid.SelectionMode != DataGridViewSelectionMode.RowHeaderSelect)
                {
                    grid.SelectionMode = DataGridViewSelectionMode.RowHeaderSelect;
                }
            }
        }

        /// <summary>
        /// Выделяет всю строку при принудительноой установке режима вся строка. 
        /// </summary>
        private void FullSelectRow(DataGridView dg)
        {
            if (dg.Rows.Count != 0)
            {
                try
                {
                    dg.Rows[dg.CurrentRow.Index].Selected = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка в выделении всей строки\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        /// <summary>
        /// Преобразует структуру DataTable в html код. 
        /// </summary>
        /// <param name="dt"></param>
        /// <returns>Код html, созданные по DataTable</returns>
        protected string ExportDatatableToHtml(DataTable dt)
        {
            StringBuilder strHTMLBuilder = new StringBuilder();
            strHTMLBuilder.Append("<html >");
            strHTMLBuilder.Append(@"<head>
                                  <title>Тестовые серверы EcmGroup</title>");
            strHTMLBuilder.Append("</head>");
            strHTMLBuilder.Append("<body>");
            strHTMLBuilder.Append(@"<table border='1px' 
                                    cellpadding='3' 
                                    cellspacing='2'                                   
                                    cols='4'
                                    style='font-family:Georgia, 'Times New Roman', Times, serif; font-size:bigger'>");
            strHTMLBuilder.Append("<tr >");
            foreach (DataColumn myColumn in dt.Columns)
            {
                strHTMLBuilder.Append("<td >");
                strHTMLBuilder.Append(myColumn.ColumnName);
                strHTMLBuilder.Append("</td>");
            }
            strHTMLBuilder.Append("</tr>");

            foreach (DataRow myRow in dt.Rows)
            {
                strHTMLBuilder.Append("<tr >");
                foreach (DataColumn myColumn in dt.Columns)
                {
                    strHTMLBuilder.Append("<td >");
                    strHTMLBuilder.Append(myRow[myColumn.ColumnName].ToString());
                    strHTMLBuilder.Append("</td>");
                }
                strHTMLBuilder.Append("</tr>");
            }

            strHTMLBuilder.Append("</table>");
            strHTMLBuilder.Append("</body>");
            strHTMLBuilder.Append("</html>");

            string Htmltext = strHTMLBuilder.ToString();

            return Htmltext;
        }

        /// <summary>
        /// Экспорт в HTML данных из объединненых объектов DataTable.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Проверка запросов SQL на уязвимости безопасности")]
        public void ExportToHTML()//BindingSource main, BindingSource detail)
        {
            DataSet DataSet = new DataSet
            {
                Locale = System.Globalization.CultureInfo.InvariantCulture
            };
            using (SqlConnection cn = new SqlConnection(DB.Connection))
            {
                try
                {
                    cn.Open();
                    SqlCommand sCommandMain = new SqlCommand(comPhy, cn);
                    SqlCommand sCommandDetail = new SqlCommand(comVir, cn);

                    SqlDataAdapter AdapterMain = new SqlDataAdapter(sCommandMain);
                    SqlDataAdapter AdapterDetail = new SqlDataAdapter(sCommandDetail);

                    AdapterMain.Fill(DataSet, "PhyServers");
                    AdapterDetail.Fill(DataSet, "VirServers");

                    // Удаляем лишние столбцы, не нужные в выгрузке.
                    DataSet.Tables[0].Columns.RemoveAt(5);
                    DataSet.Tables[0].Columns.RemoveAt(0);

                    DataSet.Tables[1].Columns.RemoveAt(6);
                    DataSet.Tables[1].Columns.RemoveAt(5);
                    DataSet.Tables[1].Columns.RemoveAt(0);

                    // Добавляем пустую строку - разделить и соединяем 2 таблицы. 
                    DataSet.Tables[0].Rows.Add();
                    DataSet.Tables[0].Merge(DataSet.Tables[1]);

                    string HtmlBody = ExportDatatableToHtml(DataSet.Tables[0]);
                    string documents = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    File.WriteAllText(documents + @"\Серверы компании (~тестовые сервера).html", HtmlBody, Encoding.UTF8);

                    MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                    DialogResult result;

                    result = MessageBox.Show("Хотите открыть полученный файл?", "ECMServersActive", buttons, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        Process.Start(documents + @"\Серверы компании (~тестовые сервера).html");
                    }
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

        #region Обработчики контролов формы

        /// <summary>
        /// Обработчик события загрузки формы.
        /// </summary>
        private void Main_Shown(object sender, EventArgs e)
        {
            Refresh();
            try
            {
                BeginInvoke(new Action(() =>
                {
                    DB.GetDataCompleted += SetGridProperties;
                    DB.GetData(bindingSource1, bindingSource2);
                }));
                //SetGridProperties();
            }
            catch (Exception)
            {
                MessageBox.Show("Не создано подключение к базе данных. Попробуйте обновить данные и проверить доступ sp13sql.\n", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
     
        /// <summary>
        /// Обработчик сохранения всех изменений в базу.
        /// </summary>
        private void ToolUpdate_Click(object sender, EventArgs e)
        {
            UpdateAllDataGridView();    // уже имеет обработку исключений
        }

        /// <summary>
        /// Обход грида физ. серверов по выделенным строкам.
        /// </summary>
        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.Rows.Count != 0)
                {
                    if (dataGridView1.SelectedRows.Count > 0)
                    {
                        if (dataGridView1.Focused)
                        {
                            checkBox1.Checked = true;
                            checkBox2.Checked = false;
                            FilterGrid(dataGridView1);
                            PhyRowID = (byte)dataGridView1.CurrentRow.Index;
                            PhyServerID = (byte)dataGridView1[0, PhyRowID].Value;
                        }
                    }
                }
                else
                {
                    foreach (Control TB in this.Controls)
                    {
                        if (TB.GetType() == typeof(TextBox))
                            TB.Text = string.Empty;
                    }
                }
                label1.Text = PhyRowID.ToString();
                label2.Text = PhyServerID.ToString();
            }
            catch { }
        }

        /// <summary>
        /// Обход грида вир. серверов по выделенным строкам.
        /// </summary>
        private void DataGridView3_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView3.Rows.Count != 0)
                {
                    if (dataGridView3.SelectedRows.Count > 0)
                    {

                        if (dataGridView3.Focused)
                        {
                            checkBox2.Checked = true;
                            checkBox1.Checked = false;
                            FilterGrid(dataGridView3);

                            VirRowID = (byte)dataGridView3.CurrentRow.Index;
                            VirServerID = (byte)dataGridView3[6, VirRowID].Value;
                        }
                    }
                }
                else
                {
                    foreach (Control TB in this.Controls)
                    {
                        if (TB.GetType() == typeof(TextBox))
                            TB.Text = string.Empty;
                    }
                }
                label1.Text = VirRowID.ToString();
                label2.Text = VirServerID.ToString();
            }
            catch { }
        }

        /// <summary>
        /// Добавление нового физ. сервера в базу данных.
        /// </summary>          
        private void ФизическийСерверToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewServer f = new NewServer();
            f.ShowDialog();
            if (f.result)
            {
                // InsertData и UpdateAllDataGridView уже содержат обработку исключений.
                // Нет нужды лишний раз нагружать ресурсы.
                DB.InsertData(f.textBox1.Text, f.textBox2.Text, f.textBox3.Text, f.textBox4.Text);
                UpdateAllDataGridView();
            }
        }

        /// <summary>
        /// Добавление нового вир. сервера в базу данных.
        /// </summary>
        private void ВиртуальныйСерверToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VirServer f = new VirServer();
            f.ShowDialog();
            if (f.result)
            {
                // InsertData и UpdateAllDataGridView уже содержат обработку исключений.
                // Нет нужды лишний раз нагружать ресурсы.
                DB.InsertData(f.textBox1.Text, f.textBox2.Text, f.textBox3.Text, f.textBox4.Text, f.CurrentServerID);
                UpdateAllDataGridView();
            }
        }

        /// <summary>
        /// Удаление физического сервера.
        /// </summary>
        private void DeletePhy_Click(object sender, EventArgs e)
        {
            // DeleteDataPhy и UpdateAllDataGridView уже содержат обработку исключений.
            DB.DeleteDataPhy(PhyServerID);
            UpdateAllDataGridView();
        }

        /// <summary>
        /// Удаление виртуального сервера.
        /// </summary>
        private void DeleteVir_Click(object sender, EventArgs e)
        {
            // DeleteDataVir и UpdateAllDataGridView уже содержат обработку исключений.
            DB.DeleteDataVir(VirServerID);
            UpdateAllDataGridView();
        }

        /// <summary>
        /// Подписываемся на событие удаления физического сервера.
        /// </summary>
        private void ФизическийToolStripMenuItem_Click(object sender, EventArgs e)
        {
            физическийToolStripMenuItem.Click += DeletePhy_Click;
        }

        /// <summary>
        /// Подписываемся на событие удаления виртуального сервера.
        /// </summary>
        private void ВиртуальныйToolStripMenuItem_Click(object sender, EventArgs e)
        {
            виртуальныйToolStripMenuItem.Click += DeleteVir_Click;
        }

        /// <summary>
        /// Обновление данных через текстбоксы.
        /// </summary>
        private void Button1_Click(object sender, EventArgs e)
        {
            BugSelectRowInDatagridview(dataGridView3);
            try
            {
                if (checkBox1.Checked)
                {
                    UpdateGridAdapter(dataGridView1, PhyRowID);
                    UpdateGridAdapter(dataGridView1);
                }
                else if (checkBox2.Checked)
                {
                    UpdateGridAdapter(dataGridView3, VirRowID);
                    UpdateGridAdapter(dataGridView3);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void UpdateGridAdapter(DataGridView dataGrid)
        {
            if (dataGrid.Name == "dataGridView1") BeginInvoke(new Action(() => DB.UpdateDataPhy(dataGridView1)));
            else BeginInvoke(new Action(() => DB.UpdateDataVir(dataGridView3)));
        }

        /// <summary>
        /// При клике отображаем дочерние элементы.
        /// </summary>
        private void ToolAddServer_ButtonClick(object sender, EventArgs e)
        {
            toolAddServer.ShowDropDown();
        }

        /// <summary>
        /// Аналогично добавлению - выводим дочерние пункты.
        /// </summary>
        private void DeleteServer_ButtonClick(object sender, EventArgs e)
        {
            deleteServer.ShowDropDown();
        }

        /// <summary>
        /// Подписка на событие выделения строки, для отображения данных в текстбоксах.
        /// </summary>
        private void DataGridView1_Click(object sender, EventArgs e)
        {
            FullSelectRow(dataGridView1);
        }

        /// <summary>
        /// Перехватываем любые ошибки в DataGridView виртуальных серверов.
        /// Необходимо реализовать корректную обработку или логирование.
        /// </summary>
        private void DataGridView3_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Exception != null)
            {
                e.Cancel = true;
                e.ThrowException = false;
            }
        }

        /// <summary>
        /// Перехватываем любые ошибки в DataGridView физических серверов.
        /// Необходимо реализовать корректную обработку или логирование.
        /// </summary>
        private void DataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            if (e.Exception != null)
            {
                e.Cancel = true;
                e.ThrowException = false;
            }
        }

        /// <summary>
        /// Устанавливаем фокус при наведении мыши, чтобы плавно работал скороллинг.
        /// </summary>
        private void DataGridView1_MouseMove_1(object sender, MouseEventArgs e)
        {
            dataGridView1.Focus();
        }

        /// <summary>
        /// Устанавливаем фокус при наведении мыши, чтобы плавно работал скороллинг.
        /// </summary>
        private void DataGridView3_MouseMove_1(object sender, MouseEventArgs e)
        {
            dataGridView3.Focus();
        }

        /// <summary>
        /// Подключение по RDP к физическим серверам, с проверкой адреса и домена, перед вызовом клиента MSTSC.
        /// </summary>
        private async void ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            byte rowID = PhyRowID;
            string adress = dataGridView1[2, rowID].Value.ToString();
            string domain = dataGridView1[3, rowID].Value.ToString();
            string _domainEN = "ecm";
            string _domainRU = "есм";
            bool _adress = adress.Trim() == string.Empty || adress == null ? true : false;
            bool _Domain = domain.Trim() == string.Empty || domain == null ? true : false;

            // Если ip и домен заданы, при этом домен, действительно, относится к ecm.
            if (!_adress && (!_Domain && (domain.Trim().ToLower().Contains(_domainEN) || domain.Trim().ToLower().Contains(_domainRU))))
            {
                if (await CMD.PingAsync(adress))
                {
                    MSTSC mstsc = new MSTSC(adress.Trim(), _domainEN);
                    mstsc.Show();
                }
                else
                {
                    MessageBox.Show("Сервер не в сети. Обновите данные и проверьте подключение", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            // Если адрес не задан, а домен пустой или не включает подстроку ecm (есм)
            else if (_adress || (_Domain || !domain.Trim().ToLower().Contains(_domainEN) || !domain.Trim().ToLower().Contains(_domainRU)))
            {
                if (_adress)
                {
                    MessageBox.Show("Не задан IP адрес подключаемого сервера.", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                // Проверка по домену не проходит.
                else
                {
                    MessageBox.Show("Не указан или некорректно задан домен сервера.", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }
        }

        /// <summary>
        /// Подключение по RDP к виртуальным серверам, с проверкой адреса и домена, перед вызовом клиента MSTSC.
        /// </summary>
        private async void ToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            byte rowID = VirRowID;
            string adress = dataGridView3[2, rowID].Value.ToString();
            string domain = dataGridView3[3, rowID].Value.ToString();
            string _domainEN = "vt";
            string _domainRU = "вт";
            bool _adress = adress.Trim() == string.Empty || adress == null ? true : false;
            bool _Domain = domain.Trim() == string.Empty || domain == null ? true : false;

            // Если ip и домен заданы, при этом домен, действительно, относится к vt.
            if (!_adress && (!_Domain && (domain.Trim().ToLower().Contains(_domainEN) || domain.Trim().ToLower().Contains(_domainRU) || domain.Trim().ToLower().Contains("wg: ecm"))))
            {
                if (await CMD.PingAsync(adress))
                {
                    MSTSC mstsc = new MSTSC(adress.Trim(), _domainEN);
                    mstsc.Show();
                }
                else
                {
                    MessageBox.Show("Сервер не в сети. Обновите данные и проверьте подключение", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            // Если адрес не задан, и домен пустой или не включает подстроку vt (вт)
            else if (_adress & (_Domain || !domain.Trim().ToLower().Contains(_domainEN) || !domain.Trim().ToLower().Contains(_domainRU)))
            {
                if (_adress)
                {
                    MessageBox.Show("Не задан IP адрес подключаемого сервера.", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                else
                {
                    MessageBox.Show("Не указан или некорректно задан домен сервера.", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
            }
            //07.05.2018: Дать возможность подключаться к домену есм из виртуальных серверов
            else if (!_adress && domain == "ecm.ecmgroup.pro")
            {
                if (await CMD.PingAsync(adress))
                {
                    MSTSC mstsc = new MSTSC(adress.Trim(), domain);
                    mstsc.Show();
                }
                else
                {
                    MessageBox.Show("Сервер не в сети. Обновите данные и проверьте подключение", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        /// <summary>
        /// Вызывает функцию ExportToHTML для экспорта в htm.
        /// </summary>
        private void ToolStripButton1_Click(object sender, EventArgs e)
        {
            ExportToHTMLAsync();
        }
        private async void ExportToHTMLAsync()
        {
            await System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                ExportToHTML();
            });
        }

        #region Навигация по БиндингНавигатору грида виртуальных серверов.
        /// <summary>
        /// Вперед по навигатору физических серверов. При навигации выделеяем всю строку.
        /// Нужно для корректного получения значения индекстов строки.
        /// </summary>
        private void BindingNavigatorMoveNextItem1_Click(object sender, EventArgs e)
        {
            FullSelectRow(dataGridView3);
        }

        /// <summary>
        /// Назад по навигатору физических серверов. При навигации выделеяем всю строку.
        /// </summary>
        private void BindingNavigatorMovePreviousItem1_Click(object sender, EventArgs e)
        {
            FullSelectRow(dataGridView3);
        }

        /// <summary>
        /// Переход к самой последней строке в таблице.
        /// </summary>
        private void BindingNavigatorMoveLastItem1_Click(object sender, EventArgs e)
        {
            FullSelectRow(dataGridView3);
        }

        /// <summary>
        /// Переход к самой первой строке в таблице.
        /// </summary>
        private void BindingNavigatorMoveFirstItem1_Click(object sender, EventArgs e)
        {
            FullSelectRow(dataGridView3);
        }

        /// <summary>
        /// При клике по любой ячейки - выделяем всю строку. Нужно для корректного получения значения индексов строки.
        /// </summary>
        private void DataGridView3_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            FullSelectRow(dataGridView3);
        }
        #endregion

        #region Навигация по БиндингНавигатору грида физических серверов.

        private void BindingNavigatorMoveFirstItem_Click(object sender, EventArgs e)
        {
            FullSelectRow(dataGridView1);
        }

        private void BindingNavigatorMovePreviousItem_Click(object sender, EventArgs e)
        {
            FullSelectRow(dataGridView1);
        }

        private void BindingNavigatorMoveNextItem_Click(object sender, EventArgs e)
        {
            FullSelectRow(dataGridView1);
        }

        private void BindingNavigatorMoveLastItem_Click(object sender, EventArgs e)
        {
            FullSelectRow(dataGridView1);
        }
        #endregion

        private void ToolStripButton2_Click(object sender, EventArgs e)
        {
            Archive _arc = new Archive();
            _arc.ShowDialog();
        }

        #endregion

        /// <summary>
        /// Помечает выделенный сервер как неактивный в таблице виртуальных серверов.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ВременноПриостановленToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DB.UpdateStatusVir(VirServerID);
            UpdateAllDataGridView();
        }

        /// <summary>
        /// Помечает выделенный сервер как неактивный в таблице физических серверов.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ВременноПриостановленToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DB.UpdateStatusPhy(PhyServerID);
            UpdateAllDataGridView();
        }

        /// <summary>
        /// Снимает метку неактивности.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void СнятьМеткуНеактивностиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DB.UpdateStatusPhy2(PhyServerID);
            UpdateAllDataGridView();
        }

        /// <summary>
        /// Снимает метку неактивности.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void СнятьМеткуНеактивностиToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DB.UpdateStatusVir2(VirServerID);
            UpdateAllDataGridView();
        }

    }
}