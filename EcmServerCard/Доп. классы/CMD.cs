using System.Collections.Generic;
using System.Data;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace ServersView
{
    /// <summary>
    /// Проверка доступа по сети для заданного списка хостов.
    /// </summary>
    class CMD
    {
        #region Методы

        /// <summary>
        /// Возвращает статус машин в локальной сети.
        /// Асинхронная версия.
        /// </summary>
        public static async Task<bool> PingAsync(string host)
        {
            try
            {
                PingReply reply = await new Ping().SendPingAsync(host);
                if (reply.Status == IPStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Перебирает значения столбца с IP-адресами в DataTable.
        /// Проверяет состояние в сети командой ping.send(host).
        /// </summary>
        /// <param name="dg">DataTable, переданный bindingSourse.</param>
        /// <param name="tableIndex">индекс таблицы 1-физических,2-виртуальных серверов</param>
        public async void DgStatus(DataTable dg, int tableIndex)
        {
            int label;

            List<Task<byte[]>> tasks = new List<Task<byte[]>>();

            if (dg.Rows.Count > 0)
            {
                foreach (DataRow row in dg.Rows)
                {

                    label = tableIndex == 1 ? 6 : 7;

                    string metka = row[label].ToString();     // метка об архиващии

                    string elemStr = row[2].ToString();       // пингуемые IP


                    if (metka == "0")   // т.е., проверяемый сервер должен быть пропингован
                    {

                        if (elemStr != string.Empty)
                        {
                            row[5] = await PingAndUpdateAsync(new Ping(), elemStr);
                        }
                    }
                    else row[5] = ConvertImageToByte._disconnec();
                }
            }
        }

        private async Task<byte[]> PingAndUpdateAsync(Ping ping, string ip)
        {
            var reply = await ping.SendPingAsync(ip);
            byte[] x;

            if (reply.Status == IPStatus.Success)
                x = ConvertImageToByte._connection();
            else
                x = ConvertImageToByte._disconnec();
            return x;
        }
        #endregion
    }
}