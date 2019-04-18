using System.Drawing;
using System.Threading.Tasks;

namespace EcmServerCard
{
    static class ConvertImageToByte
    {
        private static ImageConverter converter = new ImageConverter();
        private static Image connect = Properties.Resources.connect;
        private static Image disconnect = Properties.Resources.disconnect;
        private static Image none = Properties.Resources.none;


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
