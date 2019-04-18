using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace EcmServerCard
{

    
   // private static event DataCompleted GetDataCompleted;

    /// <summary>
    /// Класс для взаимодействия с базой данных.
    /// </summary>
    static class DB
    {
        public delegate void DataCompleted();
        public static event DataCompleted GetDataCompleted;

        #region Поля
        /// <summary>
        /// Строка для установки соединения с базой. Плохо хранить в таком открытом виде, 
        /// но для локальной задачи внутри компании - задачу решает. Все пароли известны. 
        /// В App.config хранить еще хуже, даже код не надо через dotpeak смотреть.
        /// SecureString, как возможнное решение, если потребуется.
        /// Windows Authentication не подходит.
        /// </summary>
        //private static string ConnectionLocal =
        //@"Data Source=tcp:sp13sql;Initial Catalog=EcmServerList;Persist Security Info=True;User ID=ecm;Password=123qweASD";

        private static string connection =
        @"Data Source=192.168.2.51;Initial Catalog=EcmServerList;Persist Security Info=True;Connection Timeout=5;User ID=ecm;Password=123qweASD;";

        /// <summary>
        /// SQL команда для выборки всех физических серверов.
        /// </summary>
        public static string CommandMain =
        @"Select ID, ServerName as 'Имя сервера', IP as 'Адрес', Domain as 'Домен', Description as 'Описание', Status as 'Состояние', Arhive from PhyServers";

        /// <summary>
        /// SQL команда для выборки виртуальных серверов.
        /// </summary>
        public static string CommandDetail =
        @"Select ParrentServerID as 'ID', ServerName as 'Имя сервера', IP as 'Адрес', Domain as 'Домен', Description as 'Описание', Status as 'Состояние', ID as 'PK', Archive from VirServers";

        private static CMD cmd = new CMD();
        

        #endregion

        #region Методы
        public static string Connection()
        {
            return connection;
        }

        private async static System.Threading.Tasks.Task<bool> GetServerPing()
        {
            bool p = await CMD.PingAsync("192.168.2.51");
            return p;
        }

        /// <summary>
        /// Получение данных из двух таблиц в базе.
        /// Взято с MSDN.
        /// </summary>
        /// <param name="main">Главная</param>
        /// <param name="detail">Дочерняя</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Проверка запросов SQL на уязвимости безопасности")]
        public static async void GetData(BindingSource main, BindingSource detail)
        {
            DataSet db = new DataSet();
            db.Locale = System.Globalization.CultureInfo.InvariantCulture;
            if (await GetServerPing())
            {
                using (SqlConnection cn = new SqlConnection(Connection()))
                {
                    try
                    {
                        cn.Open();
                        SqlCommand sCommandMain = new SqlCommand(CommandMain, cn);
                        sCommandMain.CommandTimeout = 5;
                        SqlCommand sCommandDetail = new SqlCommand(CommandDetail, cn);
                        sCommandDetail.CommandTimeout = 5;

                        SqlDataAdapter AdapterMain = new SqlDataAdapter(sCommandMain);
                        SqlDataAdapter AdapterDetail = new SqlDataAdapter(sCommandDetail);

                        AdapterMain.Fill(db, "PhyServers");
                        AdapterDetail.Fill(db, "VirServers");

                        //Связать отношением
                        DataRelation relation = new DataRelation("AllServers",
                        db.Tables["PhyServers"].Columns["ID"],
                        db.Tables["VirServers"].Columns["ID"]);     //Он же ParrentServer

                        db.Relations.Add(relation);

                        cmd.dgStatus(db.Tables["PhyServers"], 1);   // 1 - таблица физических серверов
                        cmd.dgStatus(db.Tables["VirServers"], 2);   // 2 - таблица виртуальных сер

                        main.DataSource = db;
                        main.DataMember = "PhyServers";

                        detail.DataSource = main;
                        detail.DataMember = "AllServers";

                        // запускаем событие об успешной загрузке данных.
                        GetDataCompleted?.Invoke();
                    }
                    catch (SqlException)
                    {
                        throw;
                    }
                }
                db.Dispose();
            }
            else
            {
                MessageBox.Show("Не создано подключение к базе данных. Проверьте, что вы находитесь в сети ECM.\n", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Добавление нового серерва в таблицу физических серверов.
        /// </summary>
        /// <param name="serv">имя сервера</param>
        /// <param name="ip">адрес</param>=х 
        /// <param name="dc">домен</param>
        /// <param name="desc">описание</param>
        public static void InsertData(string serv, string ip, string dc, string desc)
        {
            using (SqlConnection connect = new SqlConnection(Connection()))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connect;
                    command.CommandText = "INSERT INTO PhyServers" +
                                          "(ServerName, IP, Domain, Description)" +
                                          "VALUES (@ServerName, @IP, @Domain, @Description)";

                    command.Parameters.Add(new SqlParameter("@ServerName", SqlDbType.NVarChar));
                    command.Parameters.Add(new SqlParameter("@IP", SqlDbType.NVarChar));
                    command.Parameters.Add(new SqlParameter("@Domain", SqlDbType.NVarChar));
                    command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar));
                    try
                    {
                        connect.Open();
                        command.Parameters["@ServerName"].Value = serv;
                        command.Parameters["@IP"].Value = ip;
                        command.Parameters["@Domain"].Value = dc;
                        command.Parameters["@Description"].Value = desc;
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Не удалось добавить физический сервер\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Переопределенный метод вставки нового сервера для таблицы виртуальных серверов.
        /// </summary>
        /// <param name="serv">имя</param>
        /// <param name="ip">адрес</param>
        /// <param name="dc">домен</param>
        /// <param name="desc">описание</param>
        /// <param name="ID">id в таблице физ. серверов</param>
        public static void InsertData(string serv, string ip, string dc, string desc, byte ID)
        {

            using (SqlConnection connect = new SqlConnection(Connection()))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connect;
                    command.CommandText = "INSERT INTO VirServers" +
                                          "(ParrentServerID, ServerName, IP, Domain, Description)" +
                                          "VALUES (@ParrentServerID, @ServerName, @IP, @Domain, @Description)";

                    command.Parameters.Add(new SqlParameter("@ParrentServerID", SqlDbType.TinyInt));
                    command.Parameters.Add(new SqlParameter("@ServerName", SqlDbType.NVarChar));
                    command.Parameters.Add(new SqlParameter("@IP", SqlDbType.NVarChar));
                    command.Parameters.Add(new SqlParameter("@Domain", SqlDbType.NVarChar));
                    command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar));
                    try
                    {
                        connect.Open();
                        command.Parameters["@ParrentServerID"].Value = ID;
                        command.Parameters["@ServerName"].Value = serv;
                        command.Parameters["@IP"].Value = ip;
                        command.Parameters["@Domain"].Value = dc;
                        command.Parameters["@Description"].Value = desc;
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Не удалось добавить виртуальный сервер\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Удаляем физ. сервер по ID.
        /// Каскадно удаляются дочерние виртуальные серверы.
        /// </summary>
        /// <param name="ServerID">ID удаляемого сервера</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Проверка запросов SQL на уязвимости безопасности")]
        public static void DeleteDataPhy(byte ServerID)
        {
            string command = string.Format("Delete from PhyServers where ID = '{0}'", ServerID);
            using (SqlConnection connect = new SqlConnection(Connection()))
            {
                using (SqlCommand cmd = new SqlCommand(command, connect))
                {
                    try
                    {
                        connect.Open();
                        cmd.ExecuteNonQuery();
                        connect.Close();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Не удалось удалить физ. сервер\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Удаляем вир. сервер по ID.
        /// </summary>
        /// <param name="ServerID"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Проверка запросов SQL на уязвимости безопасности")]
        public static void DeleteDataVir(byte ServerID)
        {
            string command = string.Format("Delete from VirServers where ID = '{0}'", ServerID);
            using (SqlConnection connect = new SqlConnection(Connection()))
            {
                using (SqlCommand cmd = new SqlCommand(command, connect))
                {
                    try
                    {
                        connect.Open();
                        cmd.ExecuteNonQuery();
                        connect.Close();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Не удалось удалить вир. сервер\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Внесение изменений в таблицу физических серверов.
        /// </summary>
        /// <param name="dg"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Не ликвидировать объекты несколько раз"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Проверка запросов SQL на уязвимости безопасности")]
        public static void UpdateDataPhy(DataGridView dg)
        {
            byte iD = 0;
            string serverName = string.Empty;
            string iP = string.Empty;
            string domain = string.Empty;
            string description = string.Empty;

            using (SqlConnection connect = new SqlConnection(Connection()))
            {
                if (dg.RowCount != 0)
                {

                    for (int i = 0; i < dg.RowCount; i++)
                    {
                        iD = (byte)dg[0, i].Value;
                        serverName = dg[1, i].Value.ToString();
                        iP = dg[2, i].Value.ToString();
                        domain = dg[3, i].Value.ToString();
                        description = dg[4, i].Value.ToString();

                        string requstUpadate = string.Format(@"UPDATE PhyServers 
                                        SET ServerName = '{0}', IP = '{1}', Domain = '{2}', Description = '{3}'
                                        Where ID = '{4}'", serverName, iP, domain, description, iD);
                        try
                        {
                            using (SqlCommand commandUpadate = new SqlCommand(requstUpadate, connect))
                            {
                                connect.Open();
                                commandUpadate.ExecuteNonQuery();
                                connect.Close();
                            }
                        }
                        catch (SqlException ex)
                        {
                            MessageBox.Show("Не удалось обновить данные в таблице физ. серверов\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Таблица пустая.", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        /// <summary>
        /// Внесение изменений в таблицу виртуальных серверов.
        /// </summary>
        /// <param name="dg"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Проверка запросов SQL на уязвимости безопасности")]
        public static void UpdateDataVir(DataGridView dg)
        {
            byte ID = 0;
            byte parrentID = 0;
            string serverName = string.Empty;
            string iP = string.Empty;
            string domain = string.Empty;
            string description = string.Empty;

            using (SqlConnection connect = new SqlConnection(Connection()))
            {
                if (dg.RowCount != 0)
                {

                    for (int i = 0; i < dg.RowCount; i++)
                    {

                        ID = (byte)dg[6, i].Value;
                        parrentID = (byte)dg[0, i].Value;
                        serverName = dg[1, i].Value.ToString();
                        iP = dg[2, i].Value.ToString();
                        domain = dg[3, i].Value.ToString();
                        description = dg[4, i].Value.ToString();

                        string requstUpadate = string.Format(@"UPDATE VirServers 
                                        SET ParrentServerID = '{0}', ServerName = '{1}', IP = '{2}', Domain = '{3}', Description = '{4}'
                                        Where ID = '{5}'", parrentID, serverName, iP, domain, description, ID);
                        try
                        {
                            using (SqlCommand commandUpadate = new SqlCommand(requstUpadate, connect))
                            {
                                connect.Open();
                                commandUpadate.ExecuteNonQuery();
                                connect.Close();
                            }
                        }
                        catch (SqlException ex)
                        {
                            MessageBox.Show("Не удалось обновить данные в таблице вир. серверов\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Таблица пустая.", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        /// <summary>
        /// Пометить как архив выделенный сервер.
        /// </summary>
        /// <param name="ServerID"></param>
        public static void UpdateStatusPhy(byte ServerID)
        {
            string command = string.Format("UPDATE PhyServers SET [Archive] = 1 where ID = '{0}'", ServerID);
            using (SqlConnection connect = new SqlConnection(Connection()))
            {
                using (SqlCommand cmd = new SqlCommand(command, connect))
                {
                    try
                    {
                        connect.Open();
                        cmd.ExecuteNonQuery();
                        connect.Close();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Сервер не удалось пометить как временно неиспользуемый.\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Пометить как архив выделенный сервер.
        /// </summary>
        /// <param name="ServerID"></param>
        public static void UpdateStatusVir(byte ServerID)
        {
            string command = string.Format("UPDATE VirServers SET [Archive] = 1 where ID = '{0}'", ServerID);
            using (SqlConnection connect = new SqlConnection(Connection()))
            {
                using (SqlCommand cmd = new SqlCommand(command, connect))
                {
                    try
                    {
                        connect.Open();
                        cmd.ExecuteNonQuery();
                        connect.Close();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Сервер не удалось пометить как временно неиспользуемый.\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Снять метку неактивности.
        /// </summary>
        /// <param name="ServerID"></param>
        public static void UpdateStatusPhy2(byte ServerID)
        {
            string command = string.Format("UPDATE PhyServers SET [Archive] = 0 where ID = '{0}'", ServerID);
            using (SqlConnection connect = new SqlConnection(Connection()))
            {
                using (SqlCommand cmd = new SqlCommand(command, connect))
                {
                    try
                    {
                        connect.Open();
                        cmd.ExecuteNonQuery();
                        connect.Close();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Сервер не удалось пометить как временно неиспользуемый.\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Снять метку неактивности.
        /// </summary>
        /// <param name="ServerID"></param>
        public static void UpdateStatusVir2(byte ServerID)
        {
            string command = string.Format("UPDATE VirServers SET [Archive] = 0 where ID = '{0}'", ServerID);
            using (SqlConnection connect = new SqlConnection(Connection()))
            {
                using (SqlCommand cmd = new SqlCommand(command, connect))
                {
                    try
                    {
                        connect.Open();
                        cmd.ExecuteNonQuery();
                        connect.Close();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Сервер не удалось пометить как временно неиспользуемый.\n" + ex.Message, "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }
        #endregion
    }
}
