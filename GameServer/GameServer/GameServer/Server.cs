using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace GameServer
{
    class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();                  //El int seguramente es el id que recibiremos para pasarlo al constructor del cliente.

        //Al igual que en el cliente el server necesita una forma de decidir cual packet method va a llamar/enviar basado en el diccionario de ID's de nuestros distintose paquetes que recibimos. El delegado del server tiene un parametro más para identificar de qué cliente viene el paquete.
        public delegate void PacketHandler( int _fromClient, Packet _packet );
        public static Dictionary<int, PacketHandler> packetHandlers;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;                                                           //Se encargará de manejar todo lo referente a la comunicación UDP con los servidores.

        //Método que setupear el server. Recibe el máximo de jugadores y el puerto por el que se conectará.
        public static void Start( int maxPlayers, int port )
        {
            MaxPlayers = maxPlayers;
            Port = port;

            Console.WriteLine( "Starting server.." );

            InitializeServerData();                                                                     //Crea las instancias de los clientes

            tcpListener = new TcpListener( IPAddress.Any, Port );                                       //Se settean los valores que el protocolo de transmisión. Este estará escuchando las peticiones que estos requerimientos.
            tcpListener.Start();                                                                        //Iniciamos que empiece a leer(escuchar) requerimientos de coneccion de los TCP clients
            tcpListener.BeginAcceptTcpClient( new AsyncCallback( TCPConnectCallback ), null );          //Aquí es donde aunque ya está escuchando aún no había empezado a aceptar sólo hasta este punto y de manera asyncrona.

            udpListener = new UdpClient( Port );
            udpListener.BeginReceive( UDPReceiveCallback, null );

            Console.WriteLine( $"Server started on {Port}." );
        }

        //Método asyncrono que recibe la llamada de regreso de la petición de conección de los clientes. 
        //Estará verificando que haya terminado esta verificación/aceptación y convertirá los datos a la clase TcpCliente. Y volerá a aceptar requerimientos de clientes.
        //Hará un recorrimiento del diccionario de clientes al máximo permitido. Si estos clientes ya tienen una conección (socket), no hará nada con ellos... Si no la tienen creará la conexión en el index actual del diccionario,
        //pasandole el actual requerimiento del tipo clase TcpClient. Y se saldrá del método para que el mismo cliente no tenga más de un slot en el diccionario.
        private static void TCPConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListener.EndAcceptTcpClient( _result );                              //Una vez que se ejecuta el método, va a esperar a que la lectura asyncrona de datos termine.
                                                                                                        //La información es convertida en la clase TCPClient el cual será es como el socket del cliente y se lo pasamos al cliente del lado del servidor para que conceda o deniegue la conexión.
            tcpListener.BeginAcceptTcpClient( new AsyncCallback( TCPConnectCallback ), null );          //Queremos asegurarnos que una vez terminado siga escuchando peticiones de conneciones por ello volvemos a llamarlo como anteriormente para que se haga un bucle. Este creara una ejecución infinita por si alguno se sale..?
            Console.WriteLine( $"Incoming connection from {_client.Client.RemoteEndPoint}..." );

            for ( int i = 1; i <= MaxPlayers; i++ )                                                     //Verificamos si no se ha llenado el máximo de jugadores que el servidor soporta y sino se agrega a la lista de clientes que actualmente están conectados al servidor.
            {
                if ( clients[i].tcp.socket == null )                                                    //Si es NO ES null significa supongo que ese cliente ya está conectado o bien tiene asignado un socket o lugar tipo whitelist pero no es una whiteList. x'D
                {
                    clients[i].tcp.Connect( _client );                                                  //Agrega al cliente a los clientes conectados en el servidor.
                    //Console.WriteLine( $"Nuevo cliente {i}" );
                    return;
                }
            }
            Console.WriteLine( $"{_client.Client.RemoteEndPoint} failed to connect: Server full!" );
        }

        private static void UDPReceiveCallback( IAsyncResult _result )
        {
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint( IPAddress.Any, 0 );                        //Supongo este puerto es cero porque es el del servidor y puede ser cualquiera? El decia que no tenía que ser uno en especifico.
                byte[] _data = udpListener.EndReceive( _result, ref _clientEndPoint );                  //este metodo no sólo retorna cualquier byte que recibimos sino que también settea nuestro IP Endpoint al endpoint de donde viene la data.
                udpListener.BeginReceive( UDPReceiveCallback, null );                                   //Inmediatamente se prende para no perder más información.

                if ( _data.Length < 4 )                                                                 //Verificamos que ya no haya más información en el arreglo en caso contrario
                {
                    return;
                }

                //Creamos un nuevo paquete usando nuestro arreglo de bytes que recibimos del cliente. Leeremos el ID del cliente y verificamos que el ID del cliente no sea igual a cero , este caso nunca debería suceder en teoría pero en caso de no deberíamos usarlo o pasarlo
                //porque esto podría causar que el server crasheara por el codigo de nuestro diccionario de clientes.
                using ( Packet _packet = new Packet( _data ) )
                {
                    int _clientId = _packet.ReadInt();

                    if ( _clientId == 0 )
                    {
                        return;
                    }

                    if ( clients[_clientId].udp.endPoint == null )                                      //Verificamos que si es nulo, es una nueva conexión y el paquete que recibimos debería estar vacio y sólo abre el puerto del cliente en cuestión.
                    {
                        clients[_clientId].udp.Connect( _clientEndPoint );                              //Hacemos o dejamos que se conecte con el endpoint que le asignamos. (casi de manera random?)
                        return;
                    }

                    //Ahora verificamos que el endpoint que acabmos de guardar para el cliente haga match con el endpoint de donde viene el paquete. Sin esto un hacker podría facilmente suplantar a otro cliente simplemente mandando un diferente ID.
                    //La razon que se convierte a string antes de compararlos es porque sin convertirlos estaba regresando un false aunque la IP y el port estuvieran haciendo match adecuadamanete.
                    if ( clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString() )
                    {
                        clients[_clientId].udp.HandleData( _packet );                                  //Finalmente manipulamos la data.
                    }

                }

            }
            catch(Exception _ex )
            {
                Console.WriteLine( $"Error receiving UDP data: {_ex}" );
            }
        }

        public static void SendUDPData( IPEndPoint _clientEndPoint, Packet _packet )
        {
            try
            {
                if ( _clientEndPoint != null )                                                          //Nos aseguramos que el endPoint no es nulo para llamar the UDP cliente, o sea que empiece a mandar.
                {
                    udpListener.BeginSend( _packet.ToArray(), _packet.Length(), _clientEndPoint, null, null );
                }
            }
            catch(Exception _ex ) 
            {
                Console.WriteLine( $"Error sending data to {_clientEndPoint} via UDP {_ex} " );
            }
        }

        //Rellena nuestro diccionario
        private static void InitializeServerData()
        {
            for ( int i = 1; i <= MaxPlayers; i++ )
            {
                clients.Add( i, new Client( i ) );
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                { (int)ClientPackets.playerMovement, ServerHandle.PlayerMovement },
            };
            Console.WriteLine( "Initialized packets" );
        }

    }
}
