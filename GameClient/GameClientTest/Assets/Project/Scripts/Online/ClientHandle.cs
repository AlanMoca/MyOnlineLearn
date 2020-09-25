using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

//Esta clase se encarga de manipular los paquetes de la clase ServerSend del servidor. Por eso tiene los mismos nombres que esa clase del servidor.
//Y por eso los enum ServerPacket de la clase Packet contiene los mismos nombres de los métodos de esta clase.
public class ClientHandle : MonoBehaviour
{
    //NOTA: Cuando recibes un paquete va a ser una cadena de bytes lo que se va leer. Cuando tu agregas los parametros desde el servidor que va a contener el paquete (el buffer), estos se codificarán con ese orden por lo que cuando el cliente lea el paquete
    //se debe de descodificar en el mismo orden. De esta manera si son 3 bytes de string, se leerá que es un string de 3 bytes. Si leo antes por ejemplo un int, el mensaje será distinto porque no tomará la misma canditad de bytes y se perderá o modificará la data.
    public static void Welcome( Packet _packet )                                                //Es muy importante leer los valores de los paquetes en la misma forma que los escribimos (envíamos del servidor desde el método Welcome clase SendData).
    {
        string _msg = _packet.ReadString();                                                     //Mismo orden que en el servidor.
        int _myId = _packet.ReadInt();

        Debug.Log( $"Message from server {_msg}" );
        Client.Instance.myId = _myId;                                                           //Aquí le asigno el ID al cliente que va a tener y que el servidor guardo para él.

        //Send welcome received packet to the server.
        ClientSend.WelcomeReceived();

        Client.Instance.udp.Connect( ( (IPEndPoint)Client.Instance.tcp.socket.Client.LocalEndPoint ).Port );    //El puerto de mi IPLocal y sólo casteada

    }

    //Metodo que nos ayuda a manipular The spawn player packet. Este método se ejecuta una vez que el server ya leyo nuestra confirmación de welcome y nos regresa la información de los otros jugadores y yo.
    public static void SpawnPlayer( Packet _packet )
    {
        int _id = _packet.ReadInt();
        string _userName = _packet.ReadString();
        Vector3 _position = _packet.ReadVector3();
        Quaternion _rotation = _packet.ReadQuaternion();

        //Pues spawmea la info ya sea de los otros clientes o la mía, dependiendo lo que regresa el server.
        GameManager.Instance.SpawnPlayer( _id, _userName, _position, _rotation );

    }

    //Metodo que nos ayuda a settear por lo que veo la posición de nuestro propio jugador. Supongo esto es lo que causa a veces el laggaso! Y te regresa a otra posición que tal vez no era la que tú veías en pantalla! :O
    public static void PlayerPosition( Packet _packet )
    {
        int _id = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();

        GameManager.players[_id].transform.position = _position;
    }

    //Metodo que nos ayuda a settear por lo que veo la rotación de nuestro propio jugador. En este caso se mantendrá la misma porque no estamos moviendosela en el servidor. Tal vez por eso se usa el client prediction y reconcilation para ayudar a que no se vaya a marear el jugador.
    public static void PlayerRotation( Packet _packet )
    {
        int _id = _packet.ReadInt();
        Quaternion _rotation = _packet.ReadQuaternion();

        GameManager.players[_id].transform.rotation = _rotation;
    }

}
