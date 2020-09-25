using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

//Esta clase se encarga de manipular los paquetes de la clase ClientSend del cliente del lado del Cliente. Por eso tiene los mismos nombres que esa clase.
//Y por eso los enum ClientPacket de la clase Packet contiene los mismos nombres de los métodos de esta clase.
namespace GameServer {

    class ServerHandle
    {
        //Metodo que recibe el mensaje del cliente de confirmación que se conecto. Recibe el id del cliente que manda el mensaje.
        //NOTA: Poner en el mismo orden los parametros del paquete que el Welcome del cliente que envía la información.
        public static void WelcomeReceived( int _fromClient, Packet _packet )
        {
            int _clientIdCheck = _packet.ReadInt();
            string _userName = _packet.ReadString();

            Console.WriteLine( $"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}" );  //Muestro la IP del cliente y el numero de jugador que es :V

            if ( _fromClient != _clientIdCheck )                                                //Verifico si el cliente reclamo (claimed) el correcto ID.
            {
                Console.WriteLine( $"Player \"{_userName}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!" );
            }

            //Cuando se recibí el welcome del cliente, le mandaremos o le permitiremos visualizar a los otros jugadores y que ellos nos vean spawnear. :V
            Server.clients[_fromClient].SendIntoGame( _userName );
        }

        //Metodo que manipulará la información enviada por el cliente respecto a los inputs del jugador.
        //NOTA: Hay que notar que leemos la información tal cual el orden que el jugador nos la envió... 
        public static void PlayerMovement( int _fromClient, Packet _packet )
        {
            //Creamos un arreglo con el largo que tiene nuestro paquete.
            bool[] _inputs = new bool[_packet.ReadInt()];

            //Haremos un loop para populate our array (llenar nuestro arreglo)
            for ( int i = 0; i < _inputs.Length; i++ )
            {
                _inputs[i] = _packet.ReadBool();
            }

            //Leeremos la rotación que nos envía el cliente.
            Quaternion _rotation = _packet.ReadQuaternion();

            //Aquí lo que estamos haciendo es setteandole a un especifico cliente (que fue el que nos mando su input) sus inputs. La posición que calcula el propio servidor y luego envía a otros players, se hace en otro método. Aquí sólo inputs se almacenan.
            //Como nota, sólo setteamos el input, no es la posición... El servidor hace el calculo de la posición a través de los inputs (las veces que oprimió cierta tacla de movimiento) enviados por el cliente.
            Server.clients[_fromClient].player.SetInput( _inputs, _rotation );

        }

    }
}
