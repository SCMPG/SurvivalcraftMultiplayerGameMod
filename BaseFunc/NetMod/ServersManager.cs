using Comms;
using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SCMPG
{
    //@
    public static class ServersManager
    {
        private static string[] ServerNames = new string[]
        {
            /*"ruthlessconquest.kaalus.com",
            "ruthlessconquest1.kaalus.com",
            "ruthlessconquest2.kaalus.com",
            "ruthlessconquest3.kaalus.com"*/
        };

        private static Peer Peer;

        private static List<IPEndPoint> ServerAddresses = new List<IPEndPoint>();

        private static double LastLocalDiscoveryTime = double.MinValue;

        private static double LastInternetDiscoveryTime = double.MinValue;

        private static double LocalDiscoveryPeriod;

        private static double InternetDiscoveryPeriod;

        private static DynamicArray<ServerDescription> InternalDiscoveredServers = new DynamicArray<ServerDescription>();

        private static Dictionary<IPEndPoint, double> GameListRequestTimes = new Dictionary<IPEndPoint, double>();

        private static double GameListLocalRequestTime;

        public static bool IsDiscoveryStarted
        {
            get;
            private set;
        }

        public static ReadOnlyList<ServerDescription> DiscoveredServers => new ReadOnlyList<ServerDescription>(InternalDiscoveredServers);

        public static Version NewVersionServerDiscovered
        {
            get;
            private set;
        }

        public static void StartServerDiscovery(double internetDiscoveryPeriod = 3.0, double localDiscoveryPeriod = 1.0)
        {
            LocalDiscoveryPeriod = localDiscoveryPeriod;
            InternetDiscoveryPeriod = internetDiscoveryPeriod;
            if (!IsDiscoveryStarted)
            {
                IsDiscoveryStarted = true;
                try
                {
                    CreatePeer();
                    ServerAddresses.Clear();
                    InternalDiscoveredServers.Clear();
                    NewVersionServerDiscovered = null;
                    LastLocalDiscoveryTime = double.MinValue;
                    LastInternetDiscoveryTime = double.MinValue;
                }
                catch (Exception arg)
                {
                    Log.Warning($"Unable to start server discovery. Reason: {arg}");
                }
            }
        }

        public static void StopServerDiscovery()
        {
            if (IsDiscoveryStarted)
            {
                IsDiscoveryStarted = false;
                DisposePeer();
            }
        }

        public static void Update()
        {
            if (IsDiscoveryStarted && Time.FrameStartTime >= LastLocalDiscoveryTime + LocalDiscoveryPeriod)
            {
                LastLocalDiscoveryTime = Time.FrameStartTime;
                SendLocalDiscoveryRequest();
            }

            if (IsDiscoveryStarted && Time.FrameStartTime >= LastInternetDiscoveryTime + InternetDiscoveryPeriod)
            {
                LastInternetDiscoveryTime = Time.FrameStartTime;
                Task.Run(delegate
                {
                    List<IPEndPoint> addresses = DnsQueryServerAddresses();
                    Dispatcher.Dispatch(delegate
                    {
                        SendInternetDiscoveryRequests(addresses);
                    });
                });
            }

            if (Time.PeriodicEvent(0.25, 0.0))
            {
                InternalDiscoveredServers.RemoveAll((ServerDescription s) => Time.FrameStartTime > s.DiscoveryTime + (double)(s.IsLocal ? 3 : 7));
            }
        }

        private static List<IPEndPoint> DnsQueryServerAddresses()
        {
            lock (ServerAddresses)
            {
                _ = Time.RealTime;
                if (ServerAddresses.Count == 0)
                {
                    Task<List<IPEndPoint>>[] array = new Task<List<IPEndPoint>>[ServerNames.Length];
                    for (int i = 0; i < ServerNames.Length; i++)
                    {
                        int num = i;
                        string name = ServerNames[num];
                        array[num] = Task.Run(delegate
                        {
                            List<IPEndPoint> list = new List<IPEndPoint>();
                            try
                            {
                                IPAddress[] addressList = Dns.GetHostEntry(name).AddressList;
                                foreach (IPAddress iPAddress in addressList)
                                {
                                    if (iPAddress.AddressFamily == AddressFamily.InterNetwork || iPAddress.AddressFamily == AddressFamily.InterNetworkV6)
                                    {
                                        list.Add(new IPEndPoint(iPAddress, 40102));
                                    }
                                }

                                return list;
                            }
                            catch
                            {
                                return list;
                            }
                        });
                    }

                    Task[] tasks = array;
                    Task.WaitAll(tasks, 5000);
                    ServerAddresses = array.Select((Task<List<IPEndPoint>> t) => t.Result).SelectMany((List<IPEndPoint> l) => l).ToList();
                    Log.Information("Servers DNS query completed: " + ServerAddresses.Aggregate("", (string s, IPEndPoint a) => s = s + a.ToString() + " "));
                }

                return ServerAddresses.ToList();
            }
        }

        private static void CreatePeer()
        {
            DisposePeer();
            //连接到服务器
            Peer = new Peer(new DiagnosticPacketTransmitter(new UdpPacketTransmitter()));
            Peer.Error += delegate (Exception e)
            {
                Log.Error(e);
            };
            Peer.PeerDiscovered += delegate (Packet p)
            {
                
                    if (!(Message.Read(p.Data) is GameListMessage gameListMessage))
                    {
                        throw new InvalidOperationException("Unrecognized message.");
                    }

                    Handle(gameListMessage, p.Address);
            };
        }

        private static void DisposePeer()
        {
            Peer peer = Peer;
            Peer = null;
            Task.Run(delegate
            {
                peer?.Dispose();
            });
        }
        //多人加载网络
        //如果服务器没有
        private static void SendLocalDiscoveryRequest()
        {
            //@
            if (Peer != null)
            {
                //saomiao40102端口
                Peer.DiscoverLocalPeers(40102);
                GameListLocalRequestTime = Time.RealTime;
            }
        }

        private static void SendInternetDiscoveryRequests(IEnumerable<IPEndPoint> addresses)
        {
            if (Peer == null)
            {
                return;
            }

            foreach (IPEndPoint address in addresses)
            {
                Peer.DiscoverPeer(address);
                GameListRequestTimes[address] = Time.RealTime;
            }
        }

        private static void Handle(GameListMessage message, IPEndPoint address)
        {
           // Game.WorldsManager.m_worldInfos.Add(message.WorldInfo);
            Log.Information(message.ServerName);
            Log.Information(message.ServerPriority);
            foreach (var item in message.WorldInfo)
            {
                Log.Information(item.Value.DirectoryName);
            }
           
            if (!ServerAddresses.Contains(address))
            {
                ServerAddresses.Add(address);
            }
            foreach (var item in ServerAddresses)
            {
                
                Log.Information(item.ToString());
            }
            
        }
    }
}