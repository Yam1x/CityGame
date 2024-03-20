using GameClient.ServiceGame;
using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace GameClient
{

    public partial class MainWindow : Window, IGameServiceCallback
    {
        bool isConnected = false;
        bool isConnectedToLobby = false;
        GameServiceClient client;
        Guid UserId;
        Guid LobbyId;
        public MainWindow()
        {
            InitializeComponent();
        }
        void ConnectUser()
        {
            if(!isConnected)
            {
                var endpointAddress = new EndpointAddress($"net.tcp://{tbServerAdress.Text}/");
                try
                {
                    client = new GameServiceClient(new InstanceContext(this), "NetTcpBinding_IGameService", endpointAddress);
                    UserId = client.Connect(tbUserName.Text);
                    tbUserName.IsEnabled = false;
                    btnConnectToLobby.IsEnabled = true;
                    btnConnect.Content = "Отключиться от сервера";
                    isConnected = true;
                    tbServerAdress.IsEnabled = false;
                    btnConnectToLobby.IsEnabled = true;
                }
                catch(Exception ex)
                {
                    lbChat.Items.Add(ex.Message);
                }
                
                
            }
        }
        void DisconnectUser()
        {
            if (isConnected)
            {
                client.Disconnect(UserId);
                client = null;
                tbUserName.IsEnabled = true;
                btnConnect.Content = "Подключиться к серверу";
                isConnected = false;
                isConnectedToLobby = false;
                LobbyId = Guid.Empty;
                lLobbyId.Content = LobbyId;
                tbServerAdress.IsEnabled = true;
                btnConnectToLobby.Content = "Подключиться к лобби";
                btnConnectToLobby.IsEnabled = false;
            }
        }
        void ConnectToLobby()
        {
            if (!isConnectedToLobby)
            {
                client.ConnectToLobby(UserId);
                btnConnectToLobby.Content = "Отключиться от лобби";
                isConnectedToLobby = true;
            }
        }
        void DisconnectFromLobby()
        {
            if(isConnectedToLobby)
            {
                client.DisconnectFromLobby(UserId);
                btnConnectToLobby.Content = "Подключиться к лобби";
                LobbyId = Guid.Empty;
                lLobbyId.Content = LobbyId;
                isConnectedToLobby = false;
            }
        }

        private void ConnectToServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                ConnectUser();
            }
            else
            {
                DisconnectUser();
            }
        }

        public void MsgCallBack(string msg)
        {
            lbChat.Items.Add(msg);
            lbChat.ScrollIntoView(lbChat.Items[lbChat.Items.Count - 1]);
        }
        public void LobbyCallBack(string msg, Guid lobbyid)
        {
            LobbyId = lobbyid;
            lbChat.Items.Add(msg);
            lLobbyId.Content = lobbyid;
            lbChat.ScrollIntoView(lbChat.Items[lbChat.Items.Count - 1]);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisconnectUser();
        }

        private void tbMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                if (client != null)
                {
                    client.SendMsg(tbMessage.Text, UserId, LobbyId);
                    tbMessage.Text = string.Empty;
                }

            }
        }

        private void btnConnectToLobby_Click(object sender, RoutedEventArgs e)
        {
            if(!isConnectedToLobby)
            {
                ConnectToLobby();
            }
            else
            {
                DisconnectFromLobby();
            }
        }

        public void LeaderSwitchCallBack(Guid leaderId)
        {
            if (leaderId != Guid.Empty)
            {
                if (leaderId == UserId)
                {
                    tbMessage.IsEnabled = true;
                }
                else
                {
                    tbMessage.IsEnabled = false;
                }
            }
        }

        public void PlayerKickedCallBack()
        {
            LobbyId = Guid.Empty;
            lLobbyId.Content = LobbyId;
            isConnectedToLobby = false;
            btnConnectToLobby.Content = "Подключиться к лобби";
        }
    }
}
