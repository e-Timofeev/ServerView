using MSTSCLib;

using System;
using System.Windows.Forms;

namespace ServersView
{
    public partial class MSTSC : Form
    {
        #region Свойства
        private string _server { get; set; }
        private string _login { get; set; }
        private string _pass { get; set; }
        #endregion

        #region Конструкторы
        /// <summary>
        /// Конструктор по-умолчанию. Инициализация формы.
        /// </summary>
        public MSTSC() => InitializeComponent();

        /// <summary>
        ///  Определяет параметры для подключения.
        /// </summary>
        /// <param name="ip">Адрес проверяемой машины.</param>
        /// <param name="domain">Домен, в котором расположен сервер.</param>
        public MSTSC(string ip, string domain)
            : this()
        {
            _server = ip;
            switch (domain)
            {
                case "ecm.ecmgroup.pro":
                case "ecm":
                    _login = @"ecm\administrator";
                    _pass = @"123qweASD";
                    break;

                case "vt":
                    _login = @"vt\administrator";
                    _pass = @"123qweASDzxc";
                    break;
            }
        }
        #endregion

        #region Обработчики

        /// <summary>
        /// При загрузке формы.
        /// </summary>
        private void MSTSC_Load(object sender, EventArgs e)
        {
            CMD cmd = new CMD();
            Text = " " + _server;
            try
            {
                rdp.Dock = DockStyle.Fill;
                rdp.Server = _server;
                rdp.UserName = _login;
                IMsTscNonScriptable secured = (IMsTscNonScriptable)rdp.GetOcx();
                secured.ClearTextPassword = _pass;
                rdp.Connect();
            }
            catch
            {
                MessageBox.Show("Ошибка при подключении к удаленному серверу.", "Servers View", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        /// <summary>
        /// При закрытии формы.
        /// </summary>
        private void MSTSC_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (rdp.Connected.ToString() == "1")
                {
                    rdp.Disconnect();
                }
            }
            catch
            {
                rdp.Dispose();
            }
        }

        /// <summary>
        /// Завершение сеанса при закрытии формы.
        /// </summary>
        private void Rdp_OnDisconnected(object sender, AxMSTSCLib.IMsTscAxEvents_OnDisconnectedEvent e) => Close();
        #endregion
    }
}
