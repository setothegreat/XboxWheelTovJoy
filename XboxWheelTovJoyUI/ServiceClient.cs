using ServiceWire.TcpIp;
using System.Net;
using XboxWheelWorker;

namespace XboxWheelTovJoyUI
{
    public class ServiceClient
    {
        public readonly TcpClient<IWheelInputService> tcpClient;

        public ServiceClient()
        {
            tcpClient = new TcpClient<IWheelInputService>(
                new TcpEndPoint(
                    new IPEndPoint(
                        IPAddress.Parse("127.0.0.1"),  // assuming the service is running on the same machine
                        16581                         // use the same port number as the service
                    )
                )
            );
        }

        public void Start()
        {
            tcpClient.Proxy.StartWheelInput();
        }

        public void Stop()
        {
            tcpClient.Proxy.StopWheelInput();
        }
    }
}
