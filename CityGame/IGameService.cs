using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace CityGame
{
    // ПРИМЕЧАНИЕ. Можно использовать команду "Переименовать" в меню "Рефакторинг", чтобы изменить имя интерфейса "IGameService" в коде и файле конфигурации.
    [ServiceContract(CallbackContract =typeof(IServerChatCallback))]
    public interface IGameService
    {
        [OperationContract]
        Guid Connect(string name);
        [OperationContract(IsOneWay = true)]
        void Disconnect(Guid id);
        [OperationContract(IsOneWay = true)]
        void SendMsg(string msg, Guid userId, Guid lobbyId);
        [OperationContract(IsOneWay = true)]
        void ConnectToLobby(Guid id);
        [OperationContract (IsOneWay = true)]
        void DisconnectFromLobby(Guid id);
        [OperationContract(IsOneWay = true)]
        void ReadData();

    }

    public interface IServerChatCallback
    {
        [OperationContract(IsOneWay = true)]
        void MsgCallBack(string msg);

        [OperationContract(IsOneWay = true)]
        void LobbyCallBack(string msg, Guid lobbyId);
        [OperationContract(IsOneWay = true)]
        void LeaderSwitchCallBack(Guid leaderId);
        [OperationContract(IsOneWay = true)]
        void PlayerKickedCallBack();
    }

}
