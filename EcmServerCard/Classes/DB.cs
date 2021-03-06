﻿using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Configuration;

namespace ServersView
{
    /// <summary>
    /// Класс для взаимодействия с базой данных.
    /// </summary>
    internal static class DB
    {
        /// <summary>
        /// Делегат для обработки события успешного завершения загрузки данных.
        /// Наиболее актуально при частом обновлении данных.
        /// </summary>
        public delegate void DataCompleted();
        /// <summary>
        /// Событие об успешной загрузке данных.
        /// </summary>
        public static event DataCompleted GetDataCompleted;

        #region Поля

        /// <summary>
        /// SQL команда для выборки всех физических серверов.
        /// </summary>
        public static string CommandMain =
        @"Select ID, ServerName as 'Имя сервера', IP as 'Адрес', Domain as 'Домен', Description as 'Описание', Status as 'Состояние', Archive from PhyServers";

        /// <summary>
        /// SQL команда для выборки виртуальных серверов.
        /// </summary>
        public static string CommandDetail =
        @"Select ParrentServerID as 'ID', ServerName as 'Имя сервера', IP as 'Адрес', Domain as 'Домен', Description as 'Описание', Status as 'Состояние', ID as 'PK', Archive from VirServers";

        private static CMD cmd = new CMD();

        public static string Connection => ConfigurationManager.ConnectionStrings["ServersView.Properties.Settings.ServerListConnectionString"]?.ConnectionString;
        #endregion

        #region Методы
        //public static string Connection()
        //{
        //    return Connection1;
        //}

        private async static System.Threading.Tasks.Task<bool> GetServerPing()
        {
            bool p = await CMD.PingAsync("192.168.2.51");
            return p;
        }

        /// <summary>
        /// Получение данных из двух таблиц в базе.
        /// Взято с MSDN.
        /// </summary>
        /// <param name="main">Главная.</param>
        /// <param name="detail">Дочерняя.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Проверка запросов SQL на уязвимости безопасности")]
        public static async void GetData(BindingSource main, BindingSource detail)
        {
            DataSet db = new DataSet
            {
                Locale = System.Globalization.CultureInfo.InvariantCulture
            };
            if (await GetServerPing())
            {
                using (SqlConnection cn = new SqlConnection(Connection))
                {
                    try
                    {
                        cn.Open();
                        SqlCommand sCommandMain = new SqlCommand(CommandMain, cn)
                        {
                            CommandTimeout = 5
                        };
                        SqlCommand sCommandDetail = new SqlCommand(CommandDetail, cn)
                        {
                            CommandTimeout = 5
                        };

                        SqlDataAdapter AdapterMain = new SqlDataAdapter(sCommandMain);
                        SqlDataAdapter AdapterDetail = new SqlDataAdapter(sCommandDetail);

                        AdapterMain.Fill(db, "PhyServers");
                        AdapterDetail.Fill(db, "VirServers");

                        //Связать отношением
                        DataRelation relation = new DataRelation("AllServers",
                        db.Tables["PhyServers"].Columns["ID"],
                        db.Tables["VirServers"].Columns["ID"]);     //Он же ParrentServer

                        db.Relations.Add(relation);

                        cmd.DgStatus(db.Tables["PhyServers"], 1);   // 1 - таблица физических серверов
                        cmd.DgStatus(db.Tables["VirServers"], 2);   // 2 - таблица виртуальных сер

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
                MessageBox.Show("Не создано подключение к базе данных. Проверьте, что вы база доступна по сети.\n", "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        /// <summary>
        /// Добавление нового сервера в таблицу физических серверов.
        /// </summary>
        /// <param name="serv">имя сервера</param>
        /// <param name="ip">адрес</param>=х 
        /// <param name="dc">домен</param>
        /// <param name="desc">описание</param>
        /// <param name="InArchive">в архиве?</param> 
        public static void InsertData(string serv, string ip, string dc, string desc, int InArchive = 1)
        {
            using (SqlConnection connect = new SqlConnection(Connection))
            {
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = connect;
                    command.CommandText = "INSERT INTO PhyServers" +
                                          "(ServerName, IP, Domain, Description, Archive)" +
                                          "VALUES (@ServerName, @IP, @Domain, @Description, @Archive)";

                    command.Parameters.Add(new SqlParameter("@ServerName", SqlDbType.NVarChar));
                    command.Parameters.Add(new SqlParameter("@IP", SqlDbType.NVarChar));
                    command.Parameters.Add(new SqlParameter("@Domain", SqlDbType.NVarChar));
                    command.Parameters.Add(new SqlParameter("@Description", SqlDbType.NVarChar));
                    command.Parameters.Add(new SqlParameter("@Archive", SqlDbType.TinyInt));
                    try
                    {
                        connect.Open();
                        command.Parameters["@ServerName"].Value = serv;
                        command.Parameters["@IP"].Value = ip;
                        command.Parameters["@Domain"].Value = dc;
                        command.Parameters["@Description"].Value = desc;
                        command.Parameters["@Archive"].Value = InArchive;
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        MessageBox.Show("Не удалось добавить физический сервер\n" + ex.Message, "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            using (SqlConnection connect = new SqlConnection(Connection))
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
                        MessageBox.Show("Не удалось добавить виртуальный сервер\n" + ex.Message, "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            using (SqlConnection connect = new SqlConnection(Connection))
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
                        MessageBox.Show("Не удалось удалить физ. сервер\n" + ex.Message, "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            using (SqlConnection connect = new SqlConnection(Connection))
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
                        MessageBox.Show("Не удалось удалить вир. сервер\n" + ex.Message, "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            using (SqlConnection connect = new SqlConnection(Connection))
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
                            MessageBox.Show("Не удалось обновить данные в таблице физ. серверов\n" + ex.Message, "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Таблица пустая.", "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            using (SqlConnection connect = new SqlConnection(Connection))
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
                            MessageBox.Show("Не удалось обновить данные в таблице вир. серверов\n" + ex.Message, "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Таблица пустая.", "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            using (SqlConnection connect = new SqlConnection(Connection))
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
                        MessageBox.Show("Сервер не удалось пометить как временно неиспользуемый.\n" + ex.Message, "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            using (SqlConnection connect = new SqlConnection(Connection))
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
                        MessageBox.Show("Сервер не удалось пометить как временно неиспользуемый.\n" + ex.Message, "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            using (SqlConnection connect = new SqlConnection(Connection))
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
                        MessageBox.Show("Не удалось снять метку неактивности.\n" + ex.Message, "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            using (SqlConnection connect = new SqlConnection(Connection))
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
                        MessageBox.Show("Не удалось снять метку неактивности..\n" + ex.Message, "Production servers", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
        }
        #endregion
    }
}
