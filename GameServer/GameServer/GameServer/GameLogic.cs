using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer {

    class GameLogic
    {
        //Simulamos el update del juego. En este debe estar corriendo el hilo principal.
        public static void Update()
        {
            foreach ( Client _client in Server.clients.Values )
            {
                //Se lo pasamos a todos los jugadores conectados que tienen un jugador.
                if ( _client.player != null )
                {
                    _client.player.Update();
                }
            }


            ThreadManager.UpdateMain();
        }
    }
}
