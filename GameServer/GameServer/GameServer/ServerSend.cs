using System;
using System.Collections.Generic;
using System.Text;

//Esta clase se encarga de crear los distintos tipos de paquetes de información que se le enviarán a los clientes. Por eso la clase ClientHandle del lado del cliente tiene los mismos nombres que esta clase. 
//Y por eso los enum ServerPacket de la clase Packet contiene los mismos nombres de los métodos de esta clase.
namespace GameServer {

    class ServerSend
    {
        //Este método se encarga de preparar el paquete TCP para ser enviado.
        private static void SendTCPData( int _toClient, Packet _packet )
        {
            _packet.WriteLength();                                                              //Tomará el largo de la lista de bytes que queremos enviar. Y lo inserta al inicio del paquete. Es muy importante porque sino no podrás manipular la data del cliente adecuadamente.
            Server.clients[_toClient].tcp.SendData( _packet );                                  //Elegimos al jugador que le enviaremos la información por TCP y la enviamos.
        }

        //Este método se encarga de preparar el paquete UDP para ser enviado.
        private static void SendUDPData( int _toClient, Packet _packet )
        {
            _packet.WriteLength();                                                              //Tomará el largo de la lista de bytes que queremos enviar. Y lo inserta al inicio del paquete. Es muy importante porque sino no podrás manipular la data del cliente adecuadamente.
            Server.clients[_toClient].udp.SendData( _packet );                                  //Elegimos al jugador que le enviaremos la información por TCP y la enviamos.
        }

        //Método que envia el paquete a todos los clientes conectados
        private static void SendTCPDataToAll( Packet _packet )
        {
            _packet.WriteLength();                                                              //Recuerda que es muy importante poner la información del paquete al inicio del buffer (lista) porque sino no podrás manipular la data del cliente adecuadamente.
            for ( int i = 1; i <= Server.MaxPlayers; i++ )
            {
                Server.clients[i].tcp.SendData( _packet );
            }
        }

        //Método que envia el paquete a todos los clientes conectados excepto uno.
        private static void SendTCPDataToAll( int _exceptClient, Packet _packet )
        {
            _packet.WriteLength();                                                              //Recuerda que es muy importante poner la información del paquete al inicio del buffer (lista) porque sino no podrás manipular la data del cliente adecuadamente.
            for ( int i = 1; i <= Server.MaxPlayers; i++ )
            {
                if (i != _exceptClient)
                {
                    Server.clients[i].tcp.SendData( _packet );
                }
            }
        }

        //Método que envia el paquete a todos los clientes conectados
        private static void SendUDPDataToAll( Packet _packet )
        {
            _packet.WriteLength();                                                              //Recuerda que es muy importante poner la información del paquete al inicio del buffer (lista) porque sino no podrás manipular la data del cliente adecuadamente.
            for ( int i = 1; i <= Server.MaxPlayers; i++ )
            {
                Server.clients[i].udp.SendData( _packet );
            }
        }

        //Método que envia el paquete a todos los clientes conectados excepto uno.
        private static void SendUDPDataToAll( int _exceptClient, Packet _packet )
        {
            _packet.WriteLength();                                                              //Recuerda que es muy importante poner la información del paquete al inicio del buffer (lista) porque sino no podrás manipular la data del cliente adecuadamente.
            for ( int i = 1; i <= Server.MaxPlayers; i++ )
            {
                if ( i != _exceptClient )
                {
                    Server.clients[i].udp.SendData( _packet );
                }
            }
        }

        #region Packets

        //Es el método que notificará al cliente que se conecto.
        //NOTA: Cuando creas un paquete va a ser una cadena de bytes lo que se va a enviar. Cuando tu agregas los parametros que va a contener el paquete (el buffer), estos se codificarán con ese orden por lo que cuando el cliente lea el paquete
        //se debe de descodificar en el mismo orden. De esta manera si son 3 bytes de string, se leerá que es un string de 3 bytes. Si leo antes por ejemplo un int, el mensaje será distinto porque no tomará la misma canditad de bytes y se perderá o modificará la data.
        public static void Welcome(int _toClient, string _msg)                                  //El entero es a cual cliente le enviaremos el paquete y el string es el mensaje que se le enviará.
        {
            using ( Packet _packet = new Packet( (int)ServerPackets.welcome ) )                 //Cuando creamos un paquete que queremos enviar tenemos que asegurarnos de pasar el ID
            {
                _packet.Write( _msg );                                                          //Es importante el orden en que se envían porque en este orden lo recibirá el cliente. Enviamos msj y ID del cliente
                _packet.Write( _toClient );

                SendTCPData( _toClient, _packet );
            }
        }

        public static void SpawnPlayer( int _toClient, Player _player )
        {
            using ( Packet _packet = new Packet( (int)ServerPackets.spawnPlayer ) )
            {
                _packet.Write( _player.id );
                _packet.Write( _player.userName );
                _packet.Write( _player.position );
                _packet.Write( _player.rotation );

                //Usamos TCP porque sólo se enviará una vez y es importante que este primer envió de información llegué seguro y completo aunque tarde.
                SendTCPData( _toClient, _packet );
            }
        }

        public static void PlayerPosition(Player _player)
        {
            using ( Packet _packet = new Packet( (int)ServerPackets.playerPosition ) )
            {
                _packet.Write( _player.id );
                _packet.Write( _player.position );

                //Se la enviamos a todos porque querremos que nuestro servidor también lleve la cuenta de donde esta este actual jugador para todos los demás.
                SendUDPDataToAll( _packet );
            }
        }

        public static void PlayerRotation( Player _player )
        {
            using ( Packet _packet = new Packet( (int)ServerPackets.playerRotation ) )
            {
                _packet.Write( _player.id );
                _packet.Write( _player.rotation );

                //Se la enviamos a todos excepto a nosotros
                SendUDPDataToAll( _player.id, _packet );
            }
        }

        #endregion

    }
}
