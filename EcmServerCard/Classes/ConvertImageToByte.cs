using System.Drawing;

namespace ServersView
{
    static class ConvertImageToByte
    {
        private static ImageConverter converter = new ImageConverter();
        private static readonly Image connect = Properties.Resources.connect;
        private static readonly Image disconnect = Properties.Resources.disconnect;
        private static readonly Image none = Properties.Resources.none;


        public static byte[] _connection()
        {
            byte[] res = (byte[])converter.ConvertTo(connect, typeof(byte[]));
            return res;
        }

        public static byte[] _disconnec()
        {
            byte[] res = (byte[])converter.ConvertTo(disconnect, typeof(byte[]));
            return res;
        }
    }
}
