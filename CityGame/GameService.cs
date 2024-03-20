using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace CityGame
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class GameService : IGameService
    {
        
        List<ServerUser> Users = new List<ServerUser>();
        List<ServerLobby> Lobbys = new List<ServerLobby>();
        Dictionary<Guid, Timer> lobbyTimers = new Dictionary<Guid, Timer>();
        Dictionary<Guid, int> intervals = new Dictionary<Guid, int>();
        Dictionary<Guid, int> times = new Dictionary<Guid, int>();
        List<string> cities = new List<string>();
        int LobbyWaitingTime = 10; // секунды
        int TimeForStep = 20;
        public Guid Connect(string name)
        {
            var user = new ServerUser()
            {
                ID = Guid.NewGuid(),
                Name = name,
                operationContext = OperationContext.Current
            };
            Users.Add(user);

            return user.ID;

        }
        private void LobbyTimerCallback(Object id)
        {
            Guid lobbyId = (Guid)id;
            StartGame(lobbyId);
            lobbyTimers.Remove(lobbyId);
        }
        public void StartGame(Guid lobbyId)
        {
            var lobby = Lobbys.FirstOrDefault(x => x.Id == lobbyId);
            if (lobby != null && lobby.Players.Count > 0)
            {
                lobby.gameProcess.GameState = "IN_PROCESS";
                sendLobbyMsg("Игра началась", lobbyId);
                SelectRandomLeader(lobbyId);
                intervals[lobby.Id] = 10;
                times[lobby.Id] = TimeForStep;
                lobby.gameProcess.Timer = new Timer(CheckTimer, lobby.Id, 0, 1000);            
            }
        }
        public void SelectRandomLeader(Object Id)
        {
            
            var lobbyId = (Guid)Id;
            
            var lobby = Lobbys.FirstOrDefault(u => u.Id == lobbyId);
            if(lobby != null && lobby.gameProcess.GameState == "IN_PROCESS")
            if (lobby.Players != null)
            {
                var currentLeader = lobby.Players.FirstOrDefault(u => u.ID == lobby.gameProcess.CurrentLeaderId);
                var players = lobby.Players.ToList(); 
                if (currentLeader != null)
                {
                    players.Remove(currentLeader);
                }

                if (players.Count > 0)
                {
                    var randomLeader = players[new Random().Next(0, players.Count)];
                    lobby.gameProcess.CurrentLeaderId = randomLeader.ID;
                    sendLobbyMsg($"{randomLeader.Name} стал ведущим", lobbyId);
                    foreach (var item in lobby.Players)
                    {
                        item.operationContext.GetCallbackChannel<IServerChatCallback>().LeaderSwitchCallBack(randomLeader.ID);
                    }
                    times[lobbyId] = TimeForStep;
                    sendLobbyMsg($"Осталось {TimeForStep} секунд", lobbyId);

                }
            }
        }
        
        public void ConnectToLobby (Guid id)
        {
            var user = Users.FirstOrDefault(x => x.ID == id);
            if (user != null)
            {
                ServerLobby lobby = Lobbys.FirstOrDefault(u => u.Players.Count < 5 && u.gameProcess.GameState == "WAITING_FOR_PLAYERS");
                if (lobby != null)
                {
                    lobby.Players.Add(user);
                    sendLobbyMsg($"{user.Name} зашёл в лобби", lobby.Id);
                    if (lobby.Players.Count == 3)
                    {
                        Timer timer = new Timer(LobbyTimerCallback, lobby.Id, LobbyWaitingTime * 1000, Timeout.Infinite);
                        lobbyTimers[lobby.Id] = timer;
                        sendLobbyMsg($"Игра начнётся через {LobbyWaitingTime} секунд", lobby.Id);
                    }
                    else if (lobby.Players.Count >= 4)
                    {
                        if (lobbyTimers.ContainsKey(lobby.Id))
                        {
                            sendLobbyMsg($"Игра начнётся через {LobbyWaitingTime} секунд", lobby.Id);
                            lobbyTimers[lobby.Id].Change(LobbyWaitingTime * 1000, Timeout.Infinite);
                        }
                        
                    }
                }
                else
                {
                    lobby = new ServerLobby()
                    {
                        Id = Guid.NewGuid(),
                        Players = new List<ServerUser> { user },
                        gameProcess = new GameProcess() { GameState = "WAITING_FOR_PLAYERS" }
                    };
                    Lobbys.Add(lobby);
                    sendLobbyMsg($"{user.Name} зашёл в лобби", lobby.Id);
                }
                
            }
        }
        public void CheckTimer(object id)
        {
            Guid lobbyId = (Guid)id;
            var lobby = Lobbys.FirstOrDefault(u => u.Id == lobbyId);
            if(lobby != null)
            {
                if(lobby.gameProcess.GameState == "IN_PROCESS")
                {
                    times[lobbyId]--;
                    if (times[lobbyId] <= 0)
                    {
                        OutOfTime(lobbyId);
                    }
                    else if (times[lobbyId] <= intervals[lobbyId])
                    {
                        sendLobbyMsg($"Осталось {times[lobbyId]} секунд", lobbyId);
                    }
                    else if (times[lobbyId] % intervals[lobbyId] == 0)
                    {
                        sendLobbyMsg($"Осталось {times[lobbyId]} секунд", lobbyId);
                    }
                }

            }
        }
        public void OutOfTime(Object Id)
        {
            var lobbyId = (Guid)Id;
            var lobby = Lobbys.FirstOrDefault(u => u.Id == lobbyId);
            if(lobby != null)
            {
                
                var player = lobby.Players.FirstOrDefault(u => u.ID == lobby.gameProcess.CurrentLeaderId);
                if(player != null)
                {
                    lobby.Players.Remove(player);
                    player.operationContext.GetCallbackChannel<IServerChatCallback>().MsgCallBack("Вы проиграли и были исключены из игры.");
                    player.operationContext.GetCallbackChannel<IServerChatCallback>().PlayerKickedCallBack();
                    sendLobbyMsg($"Игрок {player.Name} проиграл и был исключён из игры", lobbyId);
                    if (lobby.Players.Count > 1)
                    {
                        SelectRandomLeader(lobbyId);
                    }
                    else
                    {
                        lobby.gameProcess.GameState = "ENDED";
                        lobby.gameProcess.Timer.Change(Timeout.Infinite, Timeout.Infinite);
                        sendLobbyMsg($"Поздравляю, {lobby.Players[0].Name}, вы победили! Лобби будет удалено через 10 секунд.", lobbyId);
                        lobby.gameProcess.Timer = new Timer(DeleteLobby, lobbyId, 10000, Timeout.Infinite);
                    }
                }
                
            }
            
            
        }
        public void DeleteLobby(Object Id)
        {
            var lobbyId = (Guid)Id;
            var lobby = Lobbys.FirstOrDefault(u => u.Id == lobbyId);

            if (lobby != null)
            {
                if(lobby.Players.Count > 0)
                {
                    sendLobbyMsg("Игровой сеанс был завершен", lobbyId);
                    foreach (var player in lobby.Players)
                    {
                        player.operationContext.GetCallbackChannel<IServerChatCallback>().PlayerKickedCallBack();
                    }
                    lobby.Players.Clear();
                }
                Lobbys.Remove(lobby);
            }
        }
        public void DisconnectFromLobby(Guid id)
        {
            var user = Users.SingleOrDefault(x => x.ID == id);

            if (user != null)
            {
                var lobby = Lobbys.FirstOrDefault(u => u.Players.Contains(user));
                if (lobby != null)
                {
                    if(lobby.gameProcess.GameState == "IN_PROCESS")
                    {
                        if (lobby.gameProcess.CurrentLeaderId == user.ID)
                        {
                            lobby.Players.Remove(user);
                            sendLobbyMsg($"Ведущий {user.Name} отключился", lobby.Id);
                            if (lobby.Players.Count > 1)
                            {
                                SelectRandomLeader(lobby.Id);
                            }
                            else
                            {
                                lobby.gameProcess.GameState = "ENDED";
                                lobby.gameProcess.Timer.Change(Timeout.Infinite, Timeout.Infinite);
                                sendLobbyMsg($"Поздравляю, {lobby.Players[0].Name}, вы победили! Лобби будет удалено через 10 секунд.", lobby.Id);
                                lobby.gameProcess.Timer = new Timer(DeleteLobby, lobby.Id, 10000, Timeout.Infinite);
                            }
                        }
                        else
                        {
                            lobby.Players.Remove(user);
                            sendLobbyMsg($"Игрок {user.Name} отключился", lobby.Id);
                            if (lobby.Players.Count == 1)
                            {
                                lobby.gameProcess.GameState = "ENDED";
                                lobby.gameProcess.Timer.Change(Timeout.Infinite, Timeout.Infinite);
                                sendLobbyMsg($"Поздравляю, {lobby.Players[0].Name}, вы победили! Лобби будет удалено через 10 секунд.", lobby.Id);
                                lobby.gameProcess.Timer = new Timer(DeleteLobby, lobby.Id, 10000, Timeout.Infinite);
                            }
                        }
                    }
                    
                    else if (lobby.gameProcess.GameState == "WAITING_FOR_PLAYERS")
                    {
                        lobby.Players.Remove(user);
                        sendLobbyMsg($"Игрок {user.Name} отключился", lobby.Id);
                        if (lobbyTimers.ContainsKey(lobby.Id))
                        {
                            lobbyTimers[lobby.Id].Change(Timeout.Infinite, Timeout.Infinite);
                            lobbyTimers.Remove(lobby.Id);
                            sendLobbyMsg("Недостаточно игроков для начала игры", lobby.Id);
                        }
                    }
                    else if (lobby.gameProcess.GameState == "ENDED")
                    {
                        lobby.Players.Remove(user);
                        
                    }
                }
            }
        }
        public void Disconnect(Guid id)
        {
            var user = Users.SingleOrDefault(x => x.ID == id);

            if (user != null) 
            {
                DisconnectFromLobby(id);
                Users.Remove(user);
            }
        }
        
        public void SendMsg(string msg, Guid userId, Guid lobbyId)
        {
            var lobby = Lobbys.FirstOrDefault(u => u.Id == lobbyId);
            msg = msg.ToLower();
            if (lobby != null)
            {
                var user = lobby.Players.FirstOrDefault(x => x.ID == userId);
                if (user.ID == lobby.gameProcess.CurrentLeaderId)
                {
                    if (string.IsNullOrEmpty(lobby.gameProcess.LastCity)) // Проверка того, что это первый город в сессии
                    {
                        if (cities.Contains(msg))
                        {
                            lobby.gameProcess.LastCity = msg;
                            lobby.gameProcess.playedCities.Add(msg);
                            if (msg[msg.Length - 1] != 'ь')
                            {
                                lobby.gameProcess.CurrentLetter = msg[msg.Length - 1];
                            }
                            else
                            {
                                lobby.gameProcess.CurrentLetter = msg[msg.Length - 2];
                            }

                            sendLobbyMsg($"Ведущий ввёл первый город: {msg}", lobbyId);
                            SelectRandomLeader(lobbyId);
                        }
                        else
                        {
                            sendLobbyMsg("Такого города не существует", lobbyId);
                        }
                        
                    }
                    else
                    {
                        if (cities.Contains(msg))
                        {
                            if (!lobby.gameProcess.playedCities.Contains(msg)) // Проверка того, что город используется 1 раз
                            {
                                if(lobby.gameProcess.CurrentLetter == msg[0]) 
                                {
                                    lobby.gameProcess.LastCity = msg;
                                    lobby.gameProcess.playedCities.Add(msg);
                                    if (msg[msg.Length - 1] != 'ь')
                                    {
                                        lobby.gameProcess.CurrentLetter = msg[msg.Length - 1];
                                    }
                                    else
                                    {
                                        lobby.gameProcess.CurrentLetter = msg[msg.Length - 2];
                                    }
                                    sendLobbyMsg($"Ведущий угадал город: {msg}", lobbyId);
                                    SelectRandomLeader(lobbyId);
                                }
                                else
                                {
                                    sendLobbyMsg($"Город должен начинаться на букву {lobby.gameProcess.LastCity[lobby.gameProcess.LastCity.Length - 1]}", lobby.Id);
                                }

                            }
                            else
                            {
                                sendLobbyMsg($"Город {msg} уже был разыгран, попробуйте ввести другой город", lobby.Id);
                            }
                        }
                        else
                        {
                            sendLobbyMsg($"Город {msg} не найден, попробуйте ввести новый город", lobby.Id);
                        }
                    }
                    
                }
            }
        }

        public void ReadData ()
        {
            try
            {
                var data = File.ReadAllText("../../cities.txt");
                
                string[] words = data.Split(';');
                foreach (string word in words)
                {
                    cities.Add(word.ToLower());
                }
                
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void sendLobbyMsg(string msg, Guid lobbyId)
        {
            var lobby = Lobbys.FirstOrDefault(u => u.Id == lobbyId);

            foreach (var item in lobby.Players)
            {
                item.operationContext.GetCallbackChannel<IServerChatCallback>().LobbyCallBack(msg, lobbyId);
            }
        }
    }
}
