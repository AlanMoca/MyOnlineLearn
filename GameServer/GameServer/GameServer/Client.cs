using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace GameServer
{
    //Clase para guardar la información del cliente
    class Client
    {
        public static int dataBufferSize = 4096;

        public int id;
        public Player player;
        public TCP tcp;
        public UDP udp;

        public Client(int _clientId)
        {
            id = _clientId;
            tcp = new TCP( id );
            udp = new UDP( id );
        }

        //La clase TCP es la que permitirá el protocolo y control por el cual se hará la transmisión de datos.
        //Tanto el cliente del lado del servidor como el cliente del lado del cliente necesitan implementar su protocolo de transmisión de datos.
        //Es una clase interna de ambos clientes para que sólo el cliente pueda acceder a su información
        //La clase TCP debe brindarle a los dos clientes métodos u acciones para realizar ciertas acciones como la de conectarse.
        public class TCP
        {
            public TcpClient socket;                                                //Al ser el cliente del servidor requiere tener como sockets para que clientes se conecten. El socket como tal recibirá al mismo cliente pero casteado a la clase TCPClient para poder ser leído.

            private readonly int id;
            private NetworkStream stream;                                           //El stream es como la línea de corriente de información, el conducto o bien el medio por donde se enviará o transmitirá la información. Requiere de la información que tiene el socket
                                                                                    //o bien el punto final para permitir el acceso a la red.
            private Packet receivedData;
            private byte[] receiveBuffer;

            //Nota:
            //Imagina una Stadia como una consola. La nube es el servidor y necesita un hoyo (hembra) para conectar el mando del cliente. Este hoyo de la consola, de la nube es el socket del cliente de la parte del servidor.
            //El socket del lado del cliente es el pin (la punta macho del control alambrico para meter al hoyo de la consola).
            //Stram es el cable alambrico donde el mando envía la información y tanto como el mando (cliente) como servidor (nube) se comunican por esta corriente de datos con ciertas caracteristicas que ambos sockets (Cliente y servidor) tienen que tener igualmente configurados.

            public TCP(int _id)
            {
                id = _id;
            }

            //Metodo que tomara una instancia de la clase TCPClient del Cliente.. Y Setteara los parametros necesarios para la conexión.
            //El connect del cliente del lado del servidor settea las caracteristicas y comienza con la lectura de información que envien los clientes del lado de los clientes. :V
            public void Connect(TcpClient _socket)
            {
                socket = _socket;                                                   //El servidor tiene que permitir que el cliente se conecte. Este cliente es de la parte del servidor. Cuando se conecte pedirá  un socket (que en sí es el mismo cliente pero casteado a TCPClient).
                                                                                    //Este socket ya tiene o se le ingresará las caracteristicas que tendrá del servidor y que el socket del cliente debería tener igual.
                socket.ReceiveBufferSize = dataBufferSize;                          //El servidor settea el tamaño maximo de recibimiento de información en bytes que aceptará de parte del cliente.
                socket.SendBufferSize = dataBufferSize;                             //El servidor settea el tamaño maximo de transmisión de información en bytes que le enviará al cliente.
                stream = socket.GetStream();                                        //Se obtiene la información del stream (o linea de corriente de información) del socket configurado, para configurar el stream del protocolo de transmición de información (TCP).

                receivedData = new Packet();
                receiveBuffer = new byte[dataBufferSize];                           //Se crea un arreglo que será el que almacenará la información o bien recibirá la cantidad de información que le llegué al servidor..

                stream.BeginRead( receiveBuffer, 0, dataBufferSize, ReceiveCallback, null );        //Método que permite empezar de manera asyncrona la lectura de datos que le llega al servidor.
                                                                                                    //Ejeecuta un metodo que debe permitir la lectura de información 
                                                                                                    //Almacenara la información que recibe de la llamada en el arreglo de recibimiento de información (receiveBuffer).

                ServerSend.Welcome( id, "Welcome to the server" );                                  //Aquí se comienza a enviar data desde el servidor. (Tutorial 2).
            }

            //Método que envía un paquete al cliente desde el servidor.
            public void SendData( Packet _packet )
            {
                try
                {
                    if ( socket != null )                                                       //Nos aseguramos que nuestro campo socket tenga un valor asignado. (En caso que no sería como que este cliente no tiene un endPoint o una conexión con el servidor por lo que entiendo).
                    {
                        stream.BeginWrite( _packet.ToArray(), 0, _packet.Length(), null, null );  //Comenzamos a escribir. Si te fijas el packet tiene su lista buffer y arreglo readleableBuffer. Convertimos la información almacenada de la lista y la guardamos en el arreglo
                                                                                                  //gracias al _packet.ToArray(), ya que en el método Welcome de la clase ServerSend, cuando creamos el paquete de información que queremos enviar, ese paquete guarda la información
                                                                                                  //en la lista buffer y cuando se llama a este método se nos pasa ese paquete, sólo que el método BeginWrite de stream (Class NetworkStream), lo escribe en un arreglo.
                    }
                }
                catch(Exception _ex)
                {
                    Console.WriteLine( $"Error sending data to player {id} via TCP: {_ex}" );
                }
            }

            //Método que recibe la llamada de regreso de la información de los request que ha tenido el servidor por parte de los clientes del lado de los clientes :V
            //NOTA: Antes pensaba que este es como un listener escuchando a cada rato si el cliente hizo otra petición o bien mando información. Por eso que constantemente vuelva a ejecutar la acción de stream.BeginRead y por eso también la condicional del if menor a cero,
            //para que no haga nada mientras no haya nueva información. -> Por algo son sockets, no? Vendrán de webSockets que constantemente están escuchando a diferencia de las promesas.
            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = stream.EndRead( _result );                                //Una vez que se ejecuta el método, va a esperar a que la lectura asyncrona de datos termine.
                                                                                                //Una vez terminado, se almacena el largo de información que contiene el arreglo de receiveBuffer.
                    if ( _byteLength <= 0 )                                                     //Verificamos que hay recibido bien la información en caso que sea menor a cero hubo un error y salimos.
                    {
                        //Desconectamos la conexión TCP y UDP:
                        Server.clients[id].Disconnect();
                        return;
                    }
                    byte[] _data = new byte[_byteLength];                                       //Creamos un arreglo del tamaño que recibio la llamada.
                    Array.Copy( receiveBuffer, _data, _byteLength );                            //Copiamos la información que almaceno el arreglo reciveBuffer cuando se comenzó a leer en data, con su largo.

                    //Handle data
                    receivedData.Reset( HandleData( _data ) );

                    stream.BeginRead( receiveBuffer, 0, dataBufferSize, ReceiveCallback, null );    //Comenzamos de nuevo la lectura para ver si hay más request o peticiones de otros clientes. 
                                                                                                    //(O acaso anidamos la petición de lectura de información por si no cabe la información en ese arreglo estar constantemente leyendola hasta que haya terminado?
                                                                                                    //porque sino sería casi un ciclo infinito y por eso está la condición if para que cuando ya no haya más información pueda salir del método? )

                }
                catch(Exception _ex )
                {
                    Console.WriteLine( $"Error receiving TCP data: {_ex}" );
                    //Aquí debería ir un método que desconcte
                }
            }

            //NOTA: Nuestro servidor y clientes se están comunicando a través del protocolo TCP que se basa en el continuo flujo de envio de información. 
            //Esto permite que se asegure que todos los paquetes que enviemos serán recibidos en el orden correcto pero no garantiza que se entreguen en una sola pieza (o en un solo envió o en un mismo envio).
            //Una vez que se acumula cierta cantidad de datos (bytes) se envían, por lo que TCP nos permite manipular los casos donde el paquete está dividido en 2 distintas entregas por esta razon no siempre renviamos los bytes recibidos.
            //Puede haber una pieza del paquete que no ha sido manipulada porque aún no ha sido entregada o bien el paquete sigue en camino o aún no llega, si se renvia pues resultará en perdida de información.
            private bool HandleData( byte[] _data )
            {
                int _packetLenght = 0;
                receivedData.SetBytes( _data );                                                      //Setteamos los bytes de la data recibida por el stream en el paquete de recibimiento de datos de nuestro cliente para poder manipularla. 

                //Tenemos que revisar si nuestra información recibida contiene más de 4 bytes sin leer. 
                //Si es así significa que tenemos que iniciar uno de nuestros paquetes ya sino puede existir una INT consistencia de 4 bytes. 
                //La primera data de cualquier paquete que enviamos  es una representación del largo del paquete
                if ( receivedData.UnreadLength() >= 4 )
                {
                    _packetLenght = receivedData.ReadInt();                                          //Por esa razón queremos guardar el largo de ese paquete.
                    if ( _packetLenght <= 0 )                                                       //Verificamos si ese paquete tiene un largo menor a 1. En ese caso queremos retornar true porque queremos resetear la data recibida. receivedData.Reset().
                    {
                        return true;
                    }
                }

                //El ciclo checará si el largo del paquete es mayor que cero pero menor del largo de bytes sin leer.
                //Tan largo como se este ejecutando este While-Loop, significa que receivedData contiene otro paquete completo que tenemos que manipular.
                while ( _packetLenght > 0 && _packetLenght <= receivedData.UnreadLength() )
                {
                    byte[] _packetBytes = receivedData.ReadBytes( _packetLenght );               //Leeremos esos paquetes dentro de un nuevo arreglo de bytes
                    ThreadManager.ExecuteOnMainThread( () =>                                    //En el código que estamos escribiendo No es necesario estar corriendo sobre el mismo Thread por lo que llamamos al MainThread.
                    {
                        using ( Packet _packet = new Packet( _packetBytes ) )
                        {
                            int _packetId = _packet.ReadInt();                                  //Es como la obtención del tipo de paquete que se esta recibiendo... En este caso es el Welcome...?
                           Server.packetHandlers[_packetId](id, _packet );                      //Ahora agarramos el apropiado paquete gracias al diccionario packetHAndler usando el id correspondiente a ese paquete
                        }
                    } );

                    _packetLenght = 0;

                    if ( receivedData.UnreadLength() >= 4 )
                    {
                        _packetLenght = receivedData.ReadInt();
                        if ( _packetLenght <= 0 )                                                       //Verificamos si ese paquete tiene un largo menor a 1. En ese caso queremos retornar true porque queremos resetear la data recibida. receivedData.Reset().
                        {
                            return true;
                        }
                    }
                }

                //Supongo y creooo por lo que entiendo es que está creo revisando algo del Welcome pero sino sólo verifica que la cantidad de bytes ya sea cero, por lo que no hay que esperar otro paquete y se puede resetear sin problemas el array sin perder data.
                if ( _packetLenght <= 1 )
                {
                    return true;
                }

                //Si después de toodooo esto aún es mayor a 1, significa que aún hay información o llego parcialmente el paquete y falta la otra parte y no querremos que se resetee, por lo que se regresa false.
                return false;
            }

            //
            public void Disconnect()
            {
                //Cerramos Socket y setteamos el stream como los valores que almacenan el recibimiento de información
                socket.Close();
                stream = null;                                                                  //Comunicación
                receivedData = null;                                                            //Paquete
                receiveBuffer = null;                                                           //Bytes del paquete
                socket = null;
            }

        }

        public class UDP {
            public IPEndPoint endPoint;
            private int id;

            public UDP(int _id)
            {
                id = _id;
            }

            //
            public void Connect( IPEndPoint _endPoint )
            {
                endPoint = _endPoint;
            }

            public void SendData( Packet _packet )
            {
                Server.SendUDPData( endPoint, _packet );
            }

            public void HandleData( Packet _packetData )
            {
                //Aunque desde el cliente forza que el ID esté al inicio del arreglo o bien del paquete, cuando el mismo protocolo TCP hace el envió agrega al final la marca inicial de que tan largo es el paquete, forzando que los primeros 4 bytes
                //no sean el ID del cliente sino el largo del paquete y haciendo que los bytes del ID del cliente sean los siguientes 4 en ese orden. Por eso extraemos los 4 iniciales como el largo del paquete.
                int _packetLength = _packetData.ReadInt();
                byte[] _packetBytes = _packetData.ReadBytes( _packetLength );
                ThreadManager.ExecuteOnMainThread( () => {
                    using ( Packet _packet = new Packet( _packetBytes ) )
                    {
                        int _packetId = _packet.ReadInt();
                        Server.packetHandlers[_packetId]( id, _packet );
                    }
                } );
            }

            //
            public void Disconnect()
            {
                endPoint = null;
            }

        }

        //Metodo que envia a nuestro jugador al juego?
        public void SendIntoGame( string _playerName )
        {
            player = new Player( id, _playerName, new Vector3( 0, 0, 0 ) );

            //Se usa para enviar la información de todos los otro jugadores conectados a nuestro nuevo jugador.
            foreach ( Client _client in Server.clients.Values )
            {
                //Si el cliente actual que se esta recorriendo en el diccionario ya tiene una instancia de player asignada o agregada y no es nula.
                if ( _client.player != null )
                {
                    //Es para que cuando recorramos el diccionario, nuestro jugador obtenga la información de los otros jugadores pero no de la de el mismo.
                    if ( _client.id != id )
                    {
                        //Aquí se usa mi ID pero todos los otros jugadores.
                        ServerSend.SpawnPlayer( id, _client.player );
                    }
                }
            }

            //Se usa para enviar la información de lo/los nuevos jugadores s todos los otros jugadores como así mismo.
            foreach ( Client _client in Server.clients.Values )
            {
                //Si el cliente actual que se esta recorriendo en el diccionario ya tiene una instancia de player asignada o agregada y no es nula.
                if ( _client.player != null )
                {
                    //A la inversa aquí se usa el ID de todos los clientes con Player (supongo conectados) y mi instancia de player!
                    ServerSend.SpawnPlayer( _client.id, player );
                    
                }
            }
        }

        //NOTA: A diferencia de en el cliente, aquí se llaman a los respectivos médotos de los protocolos y se pide que se desconecten desde el cliente del servidor cuando se llame este método.
        //En el cliente se llama este método del cliente dentro de la clase TCP
        private void Disconnect()
        {
            Console.WriteLine( $"{tcp.socket.Client.RemoteEndPoint} has disconnected" );
            player = null;
            tcp.Disconnect();
            udp.Disconnect();
        }
    
    }

}
