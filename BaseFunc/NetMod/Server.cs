using Comms;
using Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SCMPG
{
    //@
    public class Server : IDisposable
    {
        public const int PortNumber = 40102;

        public const int StepsPerSecond = 60;

        public const float StepDuration = 0.0166666675f;

        public const int StepsPerTick = 30;

        public const float TickDuration = 0.5f;

        private Thread Thread;

        private ServerConfig Config;

        private byte[] CachedGameListMessageBytes;

        private double CachedGameListMessageTime;

        private double TotalLoadTime;

        private double LastLoadReportTime;

        private Dictionary<IPEndPoint, double> RecentlySeenUsers = new Dictionary<IPEndPoint, double>();

        private DynamicArray<IPEndPoint> ToRemove = new DynamicArray<IPEndPoint>();

        public Peer Peer
        {
            get;
        }

        public bool IsDedicatedServer
        {
            get;
        }

        public bool IsUsingInProcessTransmitter
        {
            get;
        }

        public bool IsPaused
        {
            get;
            set;
        }

        public bool IsDisposing
        {
            get;
            private set;
        }

        public DynamicArray<ServerGame> Games
        {
            get;
        } = new DynamicArray<ServerGame>();


        public Server(bool isDedicatedServer, bool useInProcessTransmitter)
        {
            Log.Information($"Starting RuthlessConquest server on {DateTime.Now.ToUniversalTime():dd/MM/yyyy HH:mm:ss} UTC, version {Assembly.GetExecutingAssembly().GetName().Version}");
            IsDedicatedServer = isDedicatedServer;
            IsUsingInProcessTransmitter = useInProcessTransmitter;
            Config = new ServerConfig(this);
            IPacketTransmitter packetTransmitter = (!useInProcessTransmitter) ? new DiagnosticPacketTransmitter(new UdpPacketTransmitter(40102)) : new DiagnosticPacketTransmitter(new InProcessPacketTransmitter());
            Log.Information($"Server address is {packetTransmitter.Address}");
            Peer = new Peer(packetTransmitter);
            Peer.Settings.SendPeerConnectDisconnectNotifications = false;
            Peer.Comm.Settings.ResendPeriods = new float[5]
            {
                0.5f,
                0.5f,
                1f,
                1.5f,
                2f
            };
            Peer.Comm.Settings.MaxResends = 20;
            Peer.Settings.KeepAlivePeriod = (IsUsingInProcessTransmitter ? float.PositiveInfinity : 5f);
            Peer.Settings.KeepAliveResendPeriod = (IsUsingInProcessTransmitter ? float.PositiveInfinity : 2f);
            Peer.Settings.ConnectionLostPeriod = (IsUsingInProcessTransmitter ? float.PositiveInfinity : 30f);
            Peer.Settings.ConnectTimeOut = 6f;
            Peer.Error += delegate (Exception e)
            {
                Log.Error(e);
            };
            Peer.PeerDiscoveryRequest += delegate (Packet p)
            {
                if (!IsDisposing)
                {
                    if (p.Data.Length < 4)
                    {
                        throw new InvalidOperationException("Unrecognized message.");
                    }

                    HandlePeerDiscovery(p.Address);
                }
            };
            Peer.ConnectRequest += delegate (PeerPacket p)
            {
                if (!IsDisposing)
                {
                    Message message2 = Message.Read(p.Data);
                    CreateGameMessage createGameMessage = message2 as CreateGameMessage;
                    if (createGameMessage == null)
                    {
                        JoinGameMessage joinGameMessage = message2 as JoinGameMessage;
                        if (joinGameMessage == null)
                        {
                            throw new InvalidOperationException("Unrecognized message.");
                        }

                        Handle(joinGameMessage, p.Peer);
                    }
                    else
                    {
                        Handle(createGameMessage, p.Peer);
                    }
                }
            };
            Peer.PeerDisconnected += delegate (PeerData peerData)
            {
                if (!IsDisposing)
                {
                    HandleDisconnect(peerData);
                }
            };
            Peer.DataMessageReceived += delegate (PeerPacket p)
            {
                if (!IsDisposing)
                {
                    Message message = Message.Read(p.Data);
                    StartGameMessage startGameMessage = message as StartGameMessage;
                    if (startGameMessage == null)
                    {
                        PlayerOrdersMessage playerOrdersMessage = message as PlayerOrdersMessage;
                        if (playerOrdersMessage == null)
                        {
                            GameImageMessage gameImageMessage = message as GameImageMessage;
                            if (gameImageMessage == null)
                            {
                                GameStateMessage gameStateMessage = message as GameStateMessage;
                                if (gameStateMessage == null)
                                {
                                    GameStateHashMessage gameStateHashMessage = message as GameStateHashMessage;
                                    if (gameStateHashMessage == null)
                                    {
                                        throw new InvalidOperationException("Unrecognized message.");
                                    }

                                    Handle(gameStateHashMessage, p.Peer);
                                }
                                else
                                {
                                    Handle(gameStateMessage, p.Peer);
                                }
                            }
                            else
                            {
                                Handle(gameImageMessage, p.Peer);
                            }
                        }
                        else
                        {
                            Handle(playerOrdersMessage, p.Peer);
                        }
                    }
                    else
                    {
                        Handle(startGameMessage, p.Peer);
                    }
                }
            };
            if (IsDedicatedServer)
            {
                Run();
                return;
            }

            Window.Activated += WindowActivated;
            Window.Deactivated += WindowDeactivated;
            Window.Closed += WindowClosed;
            Thread = new Thread(new ThreadStart(Run));
            Thread.IsBackground = true;
            Thread.Start();
        }

        public void Dispose()
        {
            Window.Activated -= WindowActivated;
            Window.Deactivated -= WindowDeactivated;
            Window.Closed -= WindowClosed;
            IsDisposing = true;
        }

        public ServerHumanPlayer GetPlayer(PeerData peerData)
        {
            return peerData.Tag as ServerHumanPlayer;
        }

        public string GetStatsString()
        {
            int count = Games.Count;
            int num = Games.SelectMany((ServerGame gd) => gd.Players).Count();
            return $"{count} games with {num} players";
        }

        private void Run()
        {
            double num = Time.RealTime;
            while (!IsDisposing)
            {
                double realTime = Time.RealTime;
                Config.Run();
                lock (Peer.Lock)
                {
                    double realTime2 = Time.RealTime;
                    ToRemove.Clear();
                    foreach (KeyValuePair<IPEndPoint, double> recentlySeenUser in RecentlySeenUsers)
                    {
                        if (realTime2 > recentlySeenUser.Value + 60.0)
                        {
                            ToRemove.Add(recentlySeenUser.Key);
                        }
                    }

                    foreach (IPEndPoint item in ToRemove)
                    {
                        RecentlySeenUsers.Remove(item);
                    }
                }

                while (Time.RealTime >= num)
                {
                    num += 0.5;
                    if (IsPaused)
                    {
                        continue;
                    }

                    lock (Peer.Lock)
                    {
                        ServerGame[] array = Games.ToArray();
                        for (int i = 0; i < array.Length; i++)
                        {
                            array[i].Run();
                        }
                    }
                }

                double realTime3 = Time.RealTime;
                double num2 = num - Time.RealTime;
                if (num2 > 0.0)
                {
                    Thread.Sleep(MathUtils.Clamp((int)(0.5 * num2 * 1000.0), 10, 1000));
                }

                double realTime4 = Time.RealTime;
                TotalLoadTime += realTime3 - realTime;
                if (LastLoadReportTime != 0.0 && !(realTime4 >= LastLoadReportTime + 60.0))
                {
                    continue;
                }

                if (LastLoadReportTime > 0.0)
                {
                    DiagnosticPacketTransmitter diagnosticPacketTransmitter = Peer.Comm.Transmitter as DiagnosticPacketTransmitter;
                    if (diagnosticPacketTransmitter != null)
                    {
                        Log.Information(string.Concat($"Server load {TotalLoadTime / (realTime4 - LastLoadReportTime):0.000000} ({GetStatsString()}), " + $"{diagnosticPacketTransmitter.BytesSent / 1024} kB sent, {diagnosticPacketTransmitter.BytesReceived / 1024} kB received, ", $"{RecentlySeenUsers.Count} users in the last 60 seconds"));
                    }
                }

                LastLoadReportTime = realTime4;
                TotalLoadTime = 0.0;
            }

            Peer.DisconnectAllPeers();
            Task.Run(delegate
            {
                Thread.Sleep(1000);
                Peer.Dispose();
            });
        }

        private void HandlePeerDiscovery(IPEndPoint address)
        {
                double realTime = Time.RealTime;
                RecentlySeenUsers[address] = realTime;
                if (CachedGameListMessageBytes == null || realTime > CachedGameListMessageTime + 0.5)
                {
                    GameListMessage gameListMessage = new GameListMessage
                    {
                        ServerPriority = ((!Config.ShutdownSequence) ? Config.ServerPriority : 0),
                        ServerName = Config.ServerName
                    };
                    int num = 0;
                    foreach (ServerGame item in Games.OrderBy((ServerGame g) => g.Tick))
                    {
                        if (num > 40)
                        {
                            break;
                        }

                        gameListMessage.GameDescriptions.Add(new GameDescription
                        {
                            GameId = item.GameId,
                            HumanPlayersCount = item.Players.Count((ServerHumanPlayer p) => p.Faction != Faction.None),
                            SpectatorsCount = item.Players.Count((ServerHumanPlayer p) => p.Faction == Faction.None),
                            TicksSinceStart = ((item.GameStartTick >= 0) ? (item.Tick - item.GameStartTick) : (-1)),
                            CreationParameters = item.CreationParameters,
                            GameImage = item.GameImage
                        });
                        num++;
                    }

                    CachedGameListMessageBytes = Message.Write(gameListMessage);
                    CachedGameListMessageTime = realTime;
                }

                Peer.RespondToDiscovery(address, DeliveryMode.Unreliable, CachedGameListMessageBytes);
            }
        }

        private void Handle(CreateGameMessage message, PeerData peerData)
        {
            if (VerifyPlayerName(message.CreationParameters.CreatingPlayerName))
            {
                RecentlySeenUsers[peerData.Address] = Time.RealTime;
                if (Config.ShutdownSequence)
                {
                    Peer.RefuseConnect(peerData, Message.Write(new RefusedMessage
                    {
                        Reason = "Server restarting, please wait a while and try again."
                    }));
                }
                else if (Games.Count < 400)
                {
                    ServerGame serverGame = new ServerGame(this, message, peerData);
                    Games.Add(serverGame);
                    Peer.AcceptConnect(peerData, Message.Write(new GameCreatedMessage
                    {
                        GameId = serverGame.GameId,
                        CreationParameters = message.CreationParameters
                    }));
                    Log.Information($"Player {message.CreationParameters.CreatingPlayerName} at {peerData.Address} ({message.CreationParameters.CreatingPlayerPlatform}, {message.ReceivedVersion}) created game {serverGame.GameId} ({GetStatsString()})");
                }
                else
                {
                    Peer.RefuseConnect(peerData, Message.Write(new RefusedMessage
                    {
                        Reason = "Too many games in progress, please wait a while and try again."
                    }));
                }
            }
            else
            {
                Peer.RefuseConnect(peerData, Message.Write(new RefusedMessage
                {
                    Reason = "Please change your nickname in Settings."
                }));
            }
        }

        private void Handle(JoinGameMessage message, PeerData peerData)
        {
            if (VerifyPlayerName(message.PlayerName))
            {
                RecentlySeenUsers[peerData.Address] = Time.RealTime;
                ServerGame serverGame = Games.FirstOrDefault((ServerGame g) => g.GameId == message.GameId);
                if (serverGame != null)
                {
                    serverGame.Handle(message, peerData);
                    return;
                }

                Peer.RefuseConnect(peerData, Message.Write(new RefusedMessage
                {
                    Reason = "Game does not exist"
                }));
            }
            else
            {
                Peer.RefuseConnect(peerData, Message.Write(new RefusedMessage
                {
                    Reason = "Please change your nickname in Settings."
                }));
            }
        }

        private void Handle(StartGameMessage message, PeerData peerData)
        {
            RecentlySeenUsers[peerData.Address] = Time.RealTime;
            ServerHumanPlayer player = GetPlayer(peerData);
            if (player != null)
            {
                player.Game.Handle(message, player);
                return;
            }

            throw new InvalidOperationException($"Received StartGameMessage from unknown player at {peerData.Address}.");
        }

        private void Handle(PlayerOrdersMessage message, PeerData peerData)
        {
            RecentlySeenUsers[peerData.Address] = Time.RealTime;
            ServerHumanPlayer player = GetPlayer(peerData);
            if (player != null)
            {
                player.Game.Handle(message, player);
                return;
            }

            throw new InvalidOperationException($"Received PlayerOrdersMessage from unknown player at {peerData.Address}.");
        }

        private void Handle(GameImageMessage message, PeerData peerData)
        {
            RecentlySeenUsers[peerData.Address] = Time.RealTime;
            ServerHumanPlayer player = GetPlayer(peerData);
            if (player != null)
            {
                player.Game.Handle(message, player);
                return;
            }

            throw new InvalidOperationException($"Received GameImageMessage from unknown player at {peerData.Address}.");
        }

        private void Handle(GameStateMessage message, PeerData peerData)
        {
            RecentlySeenUsers[peerData.Address] = Time.RealTime;
            ServerHumanPlayer player = GetPlayer(peerData);
            if (player != null)
            {
                player.Game.Handle(message, player);
                return;
            }

            throw new InvalidOperationException($"Received GameStateMessage from unknown player at {peerData.Address}.");
        }

        private void Handle(GameStateHashMessage message, PeerData peerData)
        {
            RecentlySeenUsers[peerData.Address] = Time.RealTime;
            ServerHumanPlayer player = GetPlayer(peerData);
            if (player != null)
            {
                player.Game.Handle(message, player);
                return;
            }

            throw new InvalidOperationException($"Received GameStateHashMessage from unknown player at {peerData.Address}.");
        }

        private void HandleDisconnect(PeerData peerData)
        {
            ServerHumanPlayer player = GetPlayer(peerData);
            if (player != null)
            {
                player.Game.HandleDisconnect(player);
                return;
            }

            throw new InvalidOperationException($"Received GameStateMessage from unknown player at {peerData.Address}.");
        }

        private void WindowActivated()
        {
            if (IsUsingInProcessTransmitter && !IsDedicatedServer)
            {
                IsPaused = false;
            }
        }

        private void WindowDeactivated()
        {
            if (IsUsingInProcessTransmitter && !IsDedicatedServer)
            {
                IsPaused = true;
            }
        }

        private void WindowClosed()
        {
            Dispose();
            if (!IsUsingInProcessTransmitter)
            {
                Thread.Sleep(750);
            }
        }

        private static bool VerifyPlayerName(string playerName)
        {
            if (!(playerName == "Kaalus"))
            {
                return playerName.Replace(" ", "").ToLower() != "kaalus";
            }

            return true;
        }
    }
}