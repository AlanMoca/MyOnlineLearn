using System;
using System.Threading;

namespace GameServer {
    class Program
    {
        private static bool isRunning = false;                                                  //Nos servirá para manejar el update simulado del servidor para la lógica del juego.

        static void Main( string[] args )
        {
            Console.Title = "Game server";
            isRunning = true;

            //Creamos un nuevo thread para correr nuestra GameLoop. Se le crea una nueva instancia que recibirá el hilo inicial (ThreadStart), el cual iniciará con el método MainThread
            Thread mainThread = new Thread( new ThreadStart( MainThread ) );
            mainThread.Start();                                                                 //Se inicia el hilo.

            Server.Start( 50, 26950 );
        }

        //Metodo el cual estará haciendo la simulación del update de Unity un especifico numero de veces por segundo independientemente del framerate. Queremos que el servidor lo corra de manera consistente.
        private static void MainThread()
        {
            Console.WriteLine( $"Main Thread started. Running at {Constants.TICKS_PER_SEC} ticks per second." );
            DateTime _nextLoop = DateTime.Now;                                                   //Nos sirve para saber el tiempo exacto el cual el servidor se ejecuto

            while ( isRunning )
            {
                while ( _nextLoop < DateTime.Now )                                               //Si el momento donde se ejecuto el server es menor a nuestro momento actual (que según yo siempre será true, ejecutará el update simulado).
                {
                    GameLogic.Update();
                    _nextLoop = _nextLoop.AddMilliseconds( Constants.MS_PER_TICK );              //Actualizamos el tiempo donde la siguiente pasada o bien la siguiente ejecución sucederá.

                    //Entre ticks o "pasadas" del update simulado nuestro thread está ahí sentado sin nada que hacer causando una cantidad inesperada de poder de procesamiento
                    //Verificamos si esto está en el futuro porque obviamente por agregar los milisegundos lo superará y tiene que esperar sin nada que hacer hasta que nuestro tiempo actual lo alcance, entonces
                    //verificando si está esperando a que el presente lo alcance, bien, está en el futuro (el server se quedo en el presente necesitas ejecutar multiples pasadas más rápido para que se ponga al día o al corriente.).
                    //lo que hacemos es poner a dormir al Thread hasta que el servidor del presente haya alcanzado el tiempo del futuro y sea tiempo de volver a ejecutar el update simulado.
                    if ( _nextLoop > DateTime.Now )
                    {
                        Thread.Sleep( _nextLoop - DateTime.Now );
                    }
                }
            }
        }
    }
}
