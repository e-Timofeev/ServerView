using System;
using System.Windows.Forms;
using MSTSCLib;

namespace EcmServerCard
{
    public partial class MSTSC : Form
    {
        #region Поля
        private string _server;
        private string _login;
        private string _pass;
        #endregion

        /// <summary>
        /// Инициализация
        /// </summary>
        public MSTSC()
        {
            InitializeComponent();
        }

        /// <summary>
        ///  Определяет параметры для подключения
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="domain"></param>
        public MSTSC(string ip, string domain)
            : this()
        {
            _server = ip;
            switch (domain)
            {
                case "ecm.ecmgroup.pro": //для машины с договорами есм
                case "ecm":
                    _login = @"ecm\administrator";
                    _pass = @"123qweASD";
                    //result = true;
                    break;

                case "vt":
                    _login = @"vt\administrator";
                    _pass = @"123qweASDzxc";
                    break;
            }
        }

        private void MSTSC_Load(object sender, EventArgs e)
        {
            CMD cmd = new CMD();
            this.Text = " " + _server;
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
                MessageBox.Show("Ошибка при подключении к удаленному серверу.", "ECMServersActive", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

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

        private void rdp_OnDisconnected(object sender, AxMSTSCLib.IMsTscAxEvents_OnDisconnectedEvent e)
        {
            this.Close();
        }
    }
}
