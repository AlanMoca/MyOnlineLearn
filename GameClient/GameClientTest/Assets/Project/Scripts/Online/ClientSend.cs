using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Esta clase se encarga de crear los distintos tipos de paquetes de información que se le enviarán al servidor. Por eso la clase ServerHandle del lado del SERVIDOR tiene los mismos nombres que esta clase. 
//Y por eso los enum ClientPacket de la clase Packet contiene los mismos nombres de los métodos de esta clase.
public class ClientSend : MonoBehaviour
{
    //Este método se encarga de preparar el paquete para ser enviado al servidor.
    private static void SendTCPData( Packet _packet )
    {
        _packet.WriteLength();                                                                  //Tomará el largo de la lista de bytes que queremos enviar. Y lo inserta al inicio del paquete. Es muy importante porque sino no podrás manipular la data del cliente adecuadamente.
        Client.Instance.tcp.SendData( _packet );                                                //Hacemos que nuestro jugador/cliente envíe el paquete de información por TCP.
    }

    //Este método se encarga de preparar el paquete para ser enviado al servidor.
    private static void SendUDPData( Packet _packet )
    {
        _packet.WriteLength();                                                                  //Tomará el largo de la lista de bytes que queremos enviar. Y lo inserta al inicio del paquete. Es muy importante porque sino no podrás manipular la data del cliente adecuadamente.
        Client.Instance.udp.SendData( _packet );                                                //Hacemos que nuestro jugador/cliente envíe el paquete de información por TCP.
    }

    #region Packets

    //Método que crea el paquete que queremos enviar al servidor
    //NOTA: Cuando creas un paquete va a ser una cadena de bytes lo que se va a enviar. Cuando tu agregas los parametros que va a contener el paquete (el buffer), estos se codificarán con ese orden por lo que cuando el cliente lea el paquete
    //se debe de decodificar en el mismo orden. De esta manera si son 3 bytes de string, se leerá que es un string de 3 bytes. Si leo antes por ejemplo un int, el mensaje será distinto porque no tomará la misma canditad de bytes y se perderá o modificará la data.
    public static void WelcomeReceived()
    {
        using ( Packet _packet = new Packet( (int)ClientPackets.welcomeReceived ) )
        {
            _packet.Write( Client.Instance.myId );                                              //Se envía el ID para que el servidor pueda confirmar que el cliente reclamo el correcto ID (o sea el que le correspondía).
            _packet.Write( UIManager.Instance.userNameField.text );                                  //El username pues para que el server sepa quién es :V jaja

            SendTCPData( _packet );
        }
    }

    public static void PlayerMovement( bool[] _inputs )
    {
        using ( Packet _packet = new Packet( (int)ClientPackets.playerMovement ) )
        {
            _packet.Write( _inputs.Length );
            //Escribimos la información de los inputs registrados por nuestro jugador y lo agregamos al paquete que acabamos de crear para enviarselo al servidor.
            foreach ( bool _input in _inputs )
            {
                _packet.Write( _input );
            }
            //Le agregamos la rotación de nuestro jugador también al paquiete.
            _packet.Write( GameManager.players[Client.Instance.myId].transform.rotation );

            //Lo enviamos por UDP porque en esto no nos afecta que se pierda poca o nula información y queremos que sea rápido.
            SendUDPData( _packet );
        }
    }

    #endregion
}
