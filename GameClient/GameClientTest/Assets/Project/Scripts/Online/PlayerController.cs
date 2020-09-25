using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//El input será enviado al servidor y el servidor será el que calculará la nueva posisión de los jugadores y lo reenviará a todos los clientes.
public class PlayerController : MonoBehaviour
{
    private void FixedUpdate()
    {
        //Según yo se usa Fix y no update para no depender de FRAMERATE, ya que el fix no depende de eso, además que en el servidor le asignamos cierto número de pasadas que con el fixupdate podemos también ajustar o asignar para que sean equivalentes.
        //Aquí estaremos enviando según las pasadas la posición para que de esta forma se esté calculando y reactualizando.
        //UnityEditor -> Edit -> Projectsettings -> Time: Fixed TimeStep and MaximumParticleTimeStep 0.03333. Maximun Allowed TimeStep 0.1 and TimeScale 1. ESTO OBVIO PORQUE LOS TICKES DE NUESTRO SERVIDOR ESTÁN CONFIGURADOS ASÍ 30 por second -> 1/30.
        SendInputToServer();
    }

    //No queremos que los clientes envíen su posición al server porque eso es una puerta abierta para los cheaters (hackers o que usen cheats), y podrían moverse a donde ellos quieran volando o teletransportandose o moviendose rápido. (Por eso el server hace ese calculo).
    //No lidiaremos con las físicas porque cada juego necesita distintas físicas.
    //Metodo que se encarga de enviarle los inputs (no la posición del jugador) al servidor.
    private void SendInputToServer()
    {
        bool[] _inputs = new bool[]                                                             //Es importante cuidar el orden porque el servidor recibirá este orden y hará calculos con esto.
        {
            Input.GetKey(KeyCode.W),
            Input.GetKey(KeyCode.S),
            Input.GetKey(KeyCode.A),
            Input.GetKey(KeyCode.D),
        };

        //Este será el cliente/jugador enviando al servidor la nueva posición de mi jugador.
        ClientSend.PlayerMovement( _inputs );
    }
}
