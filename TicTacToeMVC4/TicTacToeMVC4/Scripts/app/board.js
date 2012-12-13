/// <reference path="~/signalr/hubs"/>

$(function () {

    $("#currentRoomDiv").hide();
    $("#gameBoardDiv").hide();

    var myHub = $.connection.boardGameHub;
    var myCurrentRoom = "";
    var myPlayerNumber = 0; //X is 0, O is 1
    var myCurrentGameSate = 0;

    var i;
    for (i = 0; i < 9; i++) {
        $("#block" + i.toString()).click(i, function (e) {
            //a button is clicked, e.data is indicating the button #
            //alert(e.data);

            if (myCurrentGameSate != 0) {
                alert("Game over, please reset the game");
                return;
            }

            myHub.server.makeMove(myCurrentRoom, myPlayerNumber, $(this).attr("index")).done(function (result) {
                if (result == false) {
                    alert("Illegal Move, please try again");
                }
                writeEvent("makeMove done: " + result.toString(), "makeMove done info");
            }).fail(function (error) {
                writeEvent("makeMove error: " + error.toString(), "makeMove fail error");
            });

            //need to check if dev11 can break here
            //alert(e);
        });
    }

    $("#leaveCurrentRoom").click(function () {
        myHub.server.leaveRoom(myCurrentRoom);
        $('#currentRoom').text('Not in a room');

        $("#currentRoomDiv").hide();
        $("#gameBoardDiv").hide();

        ShowRooms();
    });

    $("#ResetGame").click(function () {
        ResetGame();
    });

    myHub.client.ShowMsg = function (msg) {
        $("#msg").prepend('<br/>' + msg);
    };

    myHub.client.ShowError = function (msg) {
        alert(msg);
    };

    myHub.client.ShowGame = function (game) {

        board = game.Board;

        //board:
        //    Turn: 1
        //    BoardState: [1,0,0,0,0,0,0,0,0]
        for (var i = 0; i < board.BoardState.length; i++) {
            var buttonId = "#block" + i.toString();
            if (board.BoardState[i] == 0) {
                $(buttonId).attr("value", "_");
            }
            else if (board.BoardState[i] == 1) {
                $(buttonId).attr("value", "X");
            }
            else if (board.BoardState[i] == 2) {
                $(buttonId).attr("value", "O");
            }
        }

        var gameState = $("#GameState");
        myCurrentGameSate = board.GameState;

        // gamestate: 0 for game still going, 1 for X win, 2 for O win, 3 for tie
        switch (myCurrentGameSate) {
            case 0:
                if (board.Turn == myPlayerNumber) {
                    gameState.text("My turn");
                }
                else {
                    gameState.text("Opponent turn");
                }
                HideResetGame();
                break;
            case 1:
                gameState.text("X win");
                ShowResetGame();
                break;
            case 2:
                gameState.text("O win");
                ShowResetGame();
                break;
            case 3:
                gameState.text("Tie");
                ShowResetGame();
                break;
            default:
                gameState.text("Unknown game state: " + game.board.GameState);
                ShowResetGame();
                break;
        }

        // update player information
        $.each(game.Players, function (index, player) {
            //alert(player.UserName);
            //alert(player.PlayerNumber);
            if (player.PlayerNumber == myPlayerNumber) {
                $("#MyName").text(player.UserName);
            }
            else {
                $("#OpponentName").text(player.UserName);
            }
        });
    };

    myHub.client.UpdateRooms = function () {
        ShowRooms();
    }

    writeEvent("Starting...", "info");

    // Start the connection to get events, getting all the rooms
    $.connection.hub.start(function () {
        writeEvent("Getting rooms...", "info");
        ShowRooms();
    });

    //A function to write events to the page
    function writeEvent(eventLog, logClass) {
        var now = new Date();
        var nowStr = now.getHours() + ':' + now.getMinutes() + ':' + now.getSeconds();
        $('#EventsList').prepend('<li class="' + logClass + '"><b>' + nowStr + '</b>: ' + eventLog + '.</li>');
    }

    function ShowResetGame() {
        $("#ResetGameDiv").show();
    }

    function HideResetGame() {
        $("#ResetGameDiv").hide();
    }

    function ResetGame() {
        myHub.server.resetGame(myCurrentRoom);
    }

    function ShowRooms() {
        myHub.server.getRooms().done(function (result) {
            // result is [[object Object],[object Object],[object Object],[object Object]] {
            // [0] : {...},
            // [1] : {...},
            //    ...
            // } 
            //  where each object is type of Room
            // 

            $.each(result, function (index, room) {

                var roomName = room.Name;
                var playerCount = room.PlayerCount;

                var roomId = "room" + index.toString();

                if ($("#" + roomId).length != 0) {
                    //exists already, just update
                    $("#" + roomId + "Cnt").text(playerCount);
                }
                else {
                    $("#Rooms").append('<li><a href="#" class="room" id="' + roomId + '">' + roomName +
                        '</a> (<span id="' + roomId + 'Cnt">' + playerCount + '</span>/2)</li>');

                    $("#" + roomId).click(function () {
                        $('#currentRoom').text(roomName);
                        myCurrentRoom = roomName;
                        myHub.server.joinRoom(roomName).done(function (joinRoomResult) {

                            if (joinRoomResult == false) return;

                            myHub.server.getMyPlayNumber(roomName).done(function (playerNumber) {
                                myPlayerNumber = playerNumber;
                                if (myPlayerNumber == 0) {
                                    //X
                                    $("#MyColor").text("X");
                                    $("#OpponentColor").text("O");
                                }
                                else {
                                    //O
                                    $("#MyColor").text("O");
                                    $("#OpponentColor").text("X");
                                }
                            });

                            $("#currentRoomDiv").show();
                            $("#gameBoardDiv").show();
                            HideResetGame();

                            $(".room").css("background", "");
                            $("#" + roomId).css("background", "yellow");
                        });

                    });
                }

            });
        });
    }
});

