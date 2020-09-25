using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class Client : MonoBehaviour
{
    private static Client _instance;
    public static Client Instance {
        get
        {
            if ( _instance == null )
            {
                _instance = FindObjectOfType<Client>();
                if ( _instance == null )
                {
                    GameObject go = new GameObject();
                    go.name = typeof( Client ).Name;
                    _instance = go.AddComponent<Client>();
                    DontDestroyOnLoad( go );
                }
            }
            return _instance;
        }
        private set
        {
            value = _instance;
        }
    }

    public static int dataBufferSize = 4096;
    
    public string ip = "127.0.0.1";                             //LocalHost
    public int port = 26950;                                                                            //El cliente configura el puerto por el cual hará la labor de permitir la transmición y/o conexión
    public int myId = 0;                                                                                //Supongo esto se crea en el cliente cada que se conecta? :V
    public TCP tcp;                                                                                     //El cliente del lado del cliente también requiere crear su instancia a la clase TCP por la que se permitirá toda la transmición y acceso a información y acciones.
    public UDP udp;                                                                                     //El cliente crea su referencia a los datos UDP

    private bool isConnected = false;                                                                   //Es para que nos auxilie porque la desconexión en el servidor se ve algo distinto en el clientey nos ayudará a ajustarlo.
    private delegate void PacketHandler( Packet _packet );
    private static Dictionary<int, PacketHandler> packetHandlers;                                       //Diccionario que guardará los ID's de nuestros distintos paquetes

    private void Awake()
    {
        SetClientSingleton();
    }

    private void Start()
    {
        tcp = new TCP();
        udp = new UDP();
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    private void SetClientSingleton()
    {
        DontDestroyOnLoad( this.gameObject );
        if ( Instance != null && Instance != this )
        {
            Destroy( Instance.gameObject );
        }
        else if ( Instance == null )
        {
            Instance = this;
            DontDestroyOnLoad( this.gameObject );
        }
    }

    //Aquí el cliente a través de un UI hace la petición a la conexión del servidor.
    public void ConnectToServer()
    {
        InitializeClientData();
        isConnected = true;
        tcp.Connect();
    }

    //La clase TCP es la que permitirá el protocolo y control por el cual se hará la transmisión de datos.
    //Tanto el cliente del lado del servidor como el cliente del lado del cliente necesitan implementar su protocolo de transmisión de datos.
    //Es una clase interna de ambos clientes para que sólo el cliente pueda acceder a su información
    //La clase TCP debe brindarle a los dos clientes métodos u acciones para realizar ciertas acciones como la de conectarse.
    public class TCP
    {
        public TcpClient socket;                                                        //Al ser el cliente del cliente requiere tener configurado su socket (macho) de la misma manera que el socket servidor supongo para que no haya algún problema en la transmisión de información

        private NetworkStream stream;                                                   //El stream es como la línea de corriente de información, el conducto o bien el medio por donde se enviará o transmitirá la información. Requiere de la información que tiene el socket
                                                                                        //o bien el punto final para permitir el acceso a la red.
        private Packet receivedData;
        private byte[] receiveBuffer;

        //Método que setteará los valores que el socket necesita cuando el cliente haga la petición de conexión.
        //También iniciará el request o peticiones de conección al servidor el cual ya habrá iniciado la lectura de peticiónes.
        public void Connect()
        {
            socket = new TcpClient {
                ReceiveBufferSize = dataBufferSize,                                             //Se settean las caracteristicas del socket del cliente del lado del cliente para el recibimiento y envio de datos.
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];                                           //Se crea el arreglo que almacenará la información que se estarán compartiendo
            socket.BeginConnect( Instance.ip, Instance.port, ConnectCallback, socket );         //Aquí el socket del cliente inicia la conexión con el socket del servidor. Ip, puerto a usar, configuración del socket y el método de llamada de regreso del servidor con su información.
        }

        //Método que leerá escuchará la llamada de regreso o bien la respuesta del servidor a la petición del cliente a su conexión.
        //Si logra conectarse comienza a escuclar/leer la lectura de información que le vaya a estar regresando el servidor.
        private void ConnectCallback( IAsyncResult _result )
        {
            socket.EndConnect( _result );                                                       //Una vez que se ejecuta el método, va a esperar a que la lectura asyncrona de datos termine. Y almacena el resultado de la petición(request) en el socket

            if ( !socket.Connected )                                                            //Se verifica que la conexión se haya logrado, sino regresa un mensaje que no fue exitosa la conexión
            {
                return;
            }

            stream = socket.GetStream();                                                        //Si la conexión se logró se obtiene la información del stream (o linea de corriente de información) del socket configurado, para configurar el stream del protocolo de transmición de información (TCP).
            receivedData = new Packet();
            stream.BeginRead( receiveBuffer, 0, dataBufferSize, ReceiveCallback, null );        //Método que permite empezar de manera asyncrona la lectura de datos que el servidor le hará llegar al cliente.
                                                                                                //Ejeecuta un metodo que debe permitir la lectura de información.
                                                                                                //Almacenara la información que recibe de la llamada en el arreglo de recibimiento de información (receiveBuffer).
        }

        //Método que envía un paquete al servidor desde el cliente.
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
            catch( Exception _ex )
            {
                Debug.Log( $"Error sending data to server via TCP: {_ex}" );
            }
        }

        //NOTA: Antes creí que no sé si esté bien que este método es el que constantemente esta preguntandose si hay nueva información. Por eso la condicional if menor a cero. Si no hay información se vuelve a ejcutar y así hasta que haya información que leer. No sé si esté bien.
        //Método que recibe la llamada de regreso del servidor por si el servidor tiene nueva información para que el cliente o el usuario actualice. -> Por algo son sockets, no? Vendrán de webSockets que constantemente están escuchando a diferencia de las promesas.
        private void ReceiveCallback( IAsyncResult _result )
        {
            try
            {
                int _byteLength = stream.EndRead( _result );                                    //Una vez que se ejecuta el método, va a esperar a que la lectura asyncrona de datos termine.
                                                                                                //Una vez terminado, se almacena el largo de información que contiene el arreglo de receiveBuffer.
                if ( _byteLength <= 0 )                                                         //Verificamos que hay recibido bien la información en caso que sea menor a cero hubo un error y salimos.
                {
                    Instance.Disconnect();
                    return;
                }

                byte[] _data = new byte[_byteLength];                                           //Creamos un arreglo del tamaño que recibio la llamada.
                Array.Copy( receiveBuffer, _data, _byteLength );                                //Copiamos la información que almaceno el arreglo reciveBuffer cuando se comenzó a leer en data, con su largo.

                receivedData.Reset( HandleData( _data ) );                                       //Dependiendo de como se hizo la manipulación de la información reiniciamos el buffer del paquete o... 


                stream.BeginRead( receiveBuffer, 0, dataBufferSize, ReceiveCallback, null );    //Comenzamos de nuevo la lectura para ver si hay más request o peticiones de otros clientes. 
                                                                                                //(O acaso anidamos la petición de lectura de información por si no cabe la información en ese arreglo estar constantemente leyendola hasta que haya terminado?
                                                                                                //porque sino sería casi un ciclo infinito y por eso está la condición if para que cuando ya no haya más información pueda salir del método? )

            }
            catch ( Exception _ex )
            {
                Disconnect();
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
            //Los primeros 4 bytes de todos los paquetes son el largo del paquete... Por lo que siempre al inicio será un entero que leeremos.
            if ( receivedData.UnreadLength() >= 4 )
            {
                _packetLenght = receivedData.ReadInt();                                          //Por esa razón queremos guardar el largo de ese paquete y el primer entero o los primeros 4 bytes nos dicen el largo del paquete.
                if ( _packetLenght <= 0 )                                                       //Verificamos si ese paquete tiene un largo menor a 1. En ese caso queremos retornar true porque queremos resetear la data recibida. receivedData.Reset().
                {
                    return true;
                }
            }

            //El ciclo checará si el largo del paquete es mayor que cero pero menor del largo de bytes sin leer.
            //Tan largo como se este ejecutando este While-Loop, significa que receivedData contiene otro paquete completo que tenemos que manipular.
            //Con el largo del paquete vemos si falta obtener más información del paquete u otros paquetes
            while ( _packetLenght > 0 && _packetLenght <= receivedData.UnreadLength() )      
            {
                byte[] _packetBytes = receivedData.ReadBytes( _packetLenght );               //Leeremos esos paquetes dentro de un nuevo arreglo de bytes
                ThreadManager.ExecuteOnMainThread( () =>                                    //En el código que estamos escribiendo No es necesario estar corriendo sobre el mismo Thread por lo que llamamos al MainThread.
                {
                    using ( Packet _packet = new Packet( _packetBytes ) )
                    {
                        int _packetId = _packet.ReadInt();                                 //Es como la obtención del tipo de paquete que se esta recibiendo... En este caso es el Welcome...?
                        packetHandlers[_packetId]( _packet );                              //Ahora agarramos el apropiado paquete gracias al diccionario packetHAndler usando el id correspondiente a ese paquete
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
        private void Disconnect()
        {
            Instance.Disconnect();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }

    }

    //User Datagram Protocol. 
    //A diferencia de TCP que es un protocolo seguro, UDP no. TCP si falta se pierde paquete se encarga automáticamente de reenviar el paquete o lo restante pero pesa más por la información que contiene lo que lo hace más lento.
    //Es mejor para situaciones como chat de voz o transmisión de imagen porque aunque se pierdan paquetes son pocos o casi nulos los que se pierden y no se nota a alto nivel como en chats de voz.
    //UDP es unidireccional
    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP()
        {
            endPoint = new IPEndPoint( IPAddress.Parse( Instance.ip ), Instance.port );         //Se le asigna al endPoint los valores ip del cliente que soy y el puerto por el cual me estoy comunicando.
        }

        //Método que setteará los valores que el socket necesita cuando el cliente haga la petición de conexión.
        //También iniciará el request o peticiones de conexión al servidor el cual ya habrá iniciado la lectura de peticiónes.
        //El localPort que se le pasa por parametro es distinto al del server. Es el que usa el cliente para comunicarse
        public void Connect(int _localPort)
        {
            socket = new UdpClient( _localPort );                                               //Se le asigna al protocolo UDP del cliente el puerto que estará usando el socket.
            socket.Connect( endPoint );                                                         //El socket o bien el puerto se conectara con la IP del cliente que ya se le había pasado.
            socket.BeginReceive( ReceiveCallback, null );                                       //Comienza la petición de recibir datos de manera asyncrona.

            //Crearemos un paquete y lo enviaremos con el objetivo de iniciar la conexión con el server y abrir el localport para que el cliente pueda empezar a recibir mensajes.
            //Desde que el metodo SendData se encarga de escribir el cliente ID en el paquete no tenemos que agregarlo manualmente.
            using ( Packet _packet = new Packet() )
            {
                SendData( _packet );
            }
        }

        public void SendData( Packet _packet )
        {
            try
            {
                //Insertaremos el ID del cliente dentro del paquete antes de enviarlo (supongo al inicio), para que el servidor pueda leer quien es el sender.
                //Esto porque en UDP no podemos darle a cada cliente su propia instancia UDP, no sin al menos presenter algunos errores con puertos... :'V}
                //Es por esto que todas las comunicaciones por UDP son hechas por una sola instancia del cliente UDP. Y al menos que no incluyamos el ID, el server no podrá determinar quién envió el paquete.
                _packet.InsertInt( Instance.myId );                                             //Lo insertamos al inicio del arreglo buffer.

                if ( socket != null )                                                           //Nos aseguramos que el socket no este vació
                {
                    socket.BeginSend( _packet.ToArray(), _packet.Length(), null, null );        //El cliente empieza a mandar el paquete al servidor
                }

            }
            catch(Exception _ex )
            {
                Debug.Log( $"Error sending data to server via UDP: {_ex}" );
            }
        }

        //Metodo que se encarga de obtener la llamada de regreso del servido o bien el resultado de lo que el servidor mando.
        //El _result, es lo que me devuelve el servidor
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                byte[] _data = socket.EndReceive( _result, ref endPoint );                      //Asigna el valor retornado por el socket y hace la petición de dejar de recibir datos.
                socket.BeginReceive( ReceiveCallback, null );                                   //Comenzamos luego luego otra vez la petición o aceptación de datos para evitar que se pierdan la mayor cantidad de datos.

                //Antes de manipular la data, eenemos que asegurarnos que actualmente hay un paquete que manipular, por lo que hay que ver si el arreglo de bytes es menor a 4 en longitud para asegurarnos que sí hay.
                if ( _data.Length < 4 )
                {
                    Instance.Disconnect();
                    return;
                }

                //Manipulamos la data
                HandleData( _data );
            }
            catch(Exception _ex )
            {
                Disconnect();
            }
        }

        //Metodo que manipulará la información convertida en bytes que regreso el servidor
        private void HandleData( byte[] _data )
        {
            using ( Packet _packet = new Packet( _data ) )
            {
                //Los primeros 4 bytes del paquete representan el largo del paquete por eso es importante primero retirarlos para saber cuánto seguiremos leyendo después.
                int _packetLength = _packet.ReadInt();                                          //Obtenemos los primeros 4 bytes del arreglo y lo recorremos 4 casillas para leer la información.
                _data = _packet.ReadBytes( _packetLength );                                     //Leemos ahora sí lo que es sólo la información de bytes devuelta de la respuesta del servidor.
            }

            ThreadManager.ExecuteOnMainThread( () => {
                using ( Packet _packet = new Packet( _data ) )
                {
                    int _packetId = _packet.ReadInt();                                          //Aquí supongo que como tenemos un diccionario de acciones de paquetes, autosetteamos siempre que el segundo valor de nuestros paquetes también sea un entero 
                                                                                                //y sea el ID identificador de nuestro diccionario de paquetes. PacketHandlers.
                    packetHandlers[_packetId]( _packet );                                       //Aquí a nuestro diccionario setteamos cuál será el paquete u accion que haremos/ejecutaremos y se llama con el id idenficador pal diccionario y el paquete respectivo para el método.
                }
            } );

        }

        private void Disconnect()
        {
            Instance.Disconnect();
            endPoint = null;
            socket = null;
        }

    }

    //Metodo que inicializa nuestro diccionario
    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.spawnPlayer, ClientHandle.SpawnPlayer},
            { (int)ServerPackets.playerPosition, ClientHandle.PlayerPosition},
            { (int)ServerPackets.playerRotation, ClientHandle.PlayerRotation},
        };
        Debug.Log( "Initialized packets" );
    }

    //NOTA: A diferencia de en el servidor, aquí se llama este método dentro de la clase Disconnect del protocolo/clase TCP!
    //Se cierran ambos sockets pero bueno en el servidor el UDP no tiene socket sino EndPoint.
    private void Disconnect()
    {
        if ( isConnected )
        {
            //Nos aseguramos que se desconecte una vez cerrada la aplicación.
            isConnected = false;

            //Supongo una vez se cierran estos sockets, automáticamente en sus respectivas clases entrarán al catch que manda su desconexión.
            tcp.socket.Close();
            udp.socket.Close();

            Debug.Log( "Disconnected from server." );
        }
    }

}

