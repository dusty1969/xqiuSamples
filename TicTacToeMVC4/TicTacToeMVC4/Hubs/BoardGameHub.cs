using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TicTacToeMVC.Models;

namespace TicTacToeMVC.Hubs
{
    public class Person
    {
        /// <summary>
        /// UserName, if empty, it will be generated randomly
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 0 or 1 as the current player Number, the ones matching BoardModel's Turn will be able to play next step
        /// </summary>
        public int PlayerNumber { get; set; }
    }

    public class Room
    {
        public string Name { get; set; }
        public int PlayerCount { get; set; }
        public Room(string name, int playerCount)
        {
            Name = name;
            PlayerCount = playerCount;
        }
    }

    public class Game
    {
        public string Room { get; set; }
        public Dictionary<string, Person> Players = new Dictionary<string, Person>();
        
        public BoardModel Board = new BoardModel();
    }

    public class Games
    {
        public static Dictionary<string, Game> GameList = new Dictionary<string, Game>();
        public static Dictionary<string, Room> RoomList = new Dictionary<string, Room>();
        public static Dictionary<string, string> ConnectionUserDict = new Dictionary<string, string>();

        public static void InitializeRooms()
        {
            if (GameList.Count == 0)
            {
                for (int i = 1; i <= 4; i++)
                {
                    string roomName = string.Format("Room {0}", i);
                    GameList.Add(roomName, new Game() { Room = roomName });
                    RoomList.Add(roomName, new Room(roomName, 0));
                }
            }
        }
    }
    
    public class BoardGameHub : Hub
    {
        public Task Disconnect()
        {
            if (Games.ConnectionUserDict.ContainsKey(Context.ConnectionId))
            {
                string userName = Games.ConnectionUserDict[Context.ConnectionId];
                string room = GetCurrentRoom(userName);
                if (!string.IsNullOrEmpty(room))
                {
                    LeaveRoomAction(room, userName);
                }
                Games.ConnectionUserDict.Remove(Context.ConnectionId);
            }

            //force all clients to update room information
            return Clients.All.UpdateRooms();
        }
        
        public IEnumerable<Room> GetRooms()
        {
            Games.InitializeRooms();
            return Games.RoomList.Values;
        }

        private string GetCurrentRoom(string ConnectionId)
        {
            foreach(string gameKey in Games.GameList.Keys)
            {
                if (Games.GameList[gameKey].Players.ContainsKey(ConnectionId))
                {
                    return gameKey;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// For the client to get its PlayerNumber when entering a room
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public int GetMyPlayNumber(string room)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                Clients.Client(Context.ConnectionId).ShowError("Please log on before playing a game");
                return -4;
            }
            string userName = HttpContext.Current.User.Identity.Name;
            if (!Games.GameList.ContainsKey(room))
            {
                return -1;
            }

            string currentRoom = GetCurrentRoom(userName);
            if (string.IsNullOrEmpty(currentRoom))
            {
                return -2;
            }

            if (!Games.GameList[room].Players.ContainsKey(userName))
            {
                return -3;
            }

            return Games.GameList[room].Players[userName].PlayerNumber;
        }

        /// <summary>
        /// For a player to enter a room to join game
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public bool JoinRoom(string room)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                Clients.Client(Context.ConnectionId).ShowError("Please log on before playing a game");
                return false;
            }
            string userName = HttpContext.Current.User.Identity.Name;

            if (!Games.GameList.ContainsKey(room))
            {
                return false;
            }
            
            if (!Games.ConnectionUserDict.ContainsKey(Context.ConnectionId))
            {
                Games.ConnectionUserDict.Add(Context.ConnectionId, userName);
            }

            //Check if the user already in the room, if not, do more action
            if (!Games.GameList[room].Players.ContainsKey(userName))
            {
                //if the current room already has more than 2 players, don't allow to enter
                if (Games.GameList[room].Players.Count >= 2)
                {
                    return false;
                }

                //leave other rooms first if that is the case
                string currentRoom = GetCurrentRoom(userName);
                if (!string.IsNullOrEmpty(currentRoom))
                {
                    LeaveRoomAction(currentRoom, userName);
                }

                Games.GameList[room].Players.Add(userName, 
                    new Person(){
                        UserName = userName,
                        PlayerNumber = Games.GameList[room].Players.Count()
                    }
                );
                Games.RoomList[room].PlayerCount++;
            }

            //Hub add to the new room (via Context.ConnectionId)
            //AddToGroup(room);
            this.Groups.Add(Context.ConnectionId, room);

            //show board to the new room, to indicating the user names 
            Clients.Group(room).ShowGame(Games.GameList[room]);
            
            //announce someone enters
            Clients.Group(room).ShowMsg(string.Format("{0} enters {1}", userName, room));

            Clients.All.UpdateRooms();

            return true;
        }

        /// <summary>
        /// Player leaves a room
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public bool LeaveRoom(string room)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                Clients.Client(Context.ConnectionId).ShowError("Please log on before playing a game");
                return false;
            }
            string userName = HttpContext.Current.User.Identity.Name;
            bool success = LeaveRoomAction(room, userName);

            //force all clients to update room information
            Clients.All.UpdateRooms();

            return success;
        }

        private bool LeaveRoomAction(string room, string userName)
        {
            if (!Games.GameList.ContainsKey(room))
            {
                return false;
            }

            Game game = Games.GameList[room];

            //todo: server side check: it not current player, cannot leave room

            //Clients[Context.ConnectionId].ShowBoards(game.Board);
            //RemoveFromGroup(room);
            this.Groups.Remove(Context.ConnectionId, room);

            Games.GameList[room].Players.Remove(userName);
            Games.RoomList[room].PlayerCount--;

            Clients.Group(room).ShowMsg(string.Format("{0} leaves {1}", userName, room));

            //let's reset the board
            game.Board.Reset();
            Clients.Group(room).ShowGame(game);

            return true;
        }

        public bool ResetGame(string room)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                Clients.Client(Context.ConnectionId).ShowError("Please log on before playing a game");
                return false;
            }
            string userName = HttpContext.Current.User.Identity.Name;

            if (!Games.GameList.ContainsKey(room))
            {
                return false;
            }

            //todo: it not current player, cannot reset the board

            Game game = Games.GameList[room];
            Clients.Group(room).ShowMsg(string.Format("{0} reset game in {1}", userName, room));

            //let's reset the board
            game.Board.Reset();
            Clients.Group(room).ShowGame(game);

            return true;
        }

        /// <summary>
        /// Player make a move
        /// </summary>
        /// <param name="room"></param>
        /// <param name="turn"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool MakeMove(string room, int turn, int index)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                Clients.Client(Context.ConnectionId).ShowError("Please log on before playing a game");
                return false;
            }
            string userName = HttpContext.Current.User.Identity.Name;

            if(!Games.GameList.ContainsKey(room))
            {
                return false;
            }

            Game game = Games.GameList[room];

            //todo, server side check turn, and show who's playing what

            bool isOK = game.Board.SetStep(turn, index);
            
            //todo: server side show success / fail
            //todo: server side show illegal move


            Clients.Group(room).ShowMsg(string.Format("{0} click {1}", userName, index));
            Clients.Group(room).ShowGame(game);

            //Question: What's the difference between using Task.Factory.StartNew(() and not using it?  Seems all working for me.

            //Task.Factory.StartNew(() =>
            //{
            //    Clients[room].ShowMsg(string.Format("{0} click {1}", userName, index));
            //});

            //if (isOK)
            //{
            //    Task.Factory.StartNew(() =>
            //    {
            //        Clients[room].ShowGame(game);
            //    });
            //}

            return isOK;
        }


    }
}