using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace GameServer {
    class Player
    {
        public int id;
        public string userName;
        public Vector3 position;
        public Quaternion rotation;

        //Como esto será aplicado cada tick, dividimos la velocidad de movimiento por los ticks per seconds. Esto tendrá el mismo efecto que multiplicar por Time.DeltaTime en unity.
        private float moveSpeed = 5f / Constants.TICKS_PER_SEC;
        //Almacenará los inputs que recibiremos del cliente.
        private bool[] inputs;

        public Player( int _id, string _userName, Vector3 _spawnPosition )
        {
            this.id = _id;
            this.userName = _userName;
            this.position = _spawnPosition;

            inputs = new bool[4];
        }

        //Este es el método interno de player que usaremos para calcular el movimiento que tuvo el jugador.
        public void Update()
        {
            //Usaremos los inputs para convertirlos en vector2 para representar la localDirection del jugador que se quiere mover.
            Vector2 _inputDirection = Vector2.Zero;
            if ( inputs[0] )
            {
                _inputDirection.Y += 1;
            }
            if ( inputs[1] )
            {
                _inputDirection.Y -= 1;
            }
            if ( inputs[2] )
            {
                _inputDirection.X += 1;
            }
            if ( inputs[3] )
            {
                _inputDirection.X -= 1;
            }

            Move( _inputDirection );

        }

        private void Move( Vector2 _inputDirection )
        {
            //Esta es la dirección donde el jugador estará mirando. Para calcularlo se usa el vector unitario de a donde estará viendo nuestro jugador (en este caso z) y la rotación obtenida o que nos mando el cliente... (Lee la descripción del método Transform).
            Vector3 _forward = Vector3.Transform( new Vector3( 0, 0, 1 ), rotation );
            //También necesitamos un Vector3 que sea perpendicular al forward del jugador. Se obtiene normalizando un vector y haciendo el producto cruz de donde mira el jugador y el eje Y.
            Vector3 _right = Vector3.Normalize( Vector3.Cross( _forward, new Vector3( 0, 1, 0 ) ) );

            //Ahora calcularemos la dirección a donde moveremos el jugador. Multiplicando la InputComponent en el eje X * el vector right y la InputComponent en el eje y * el vector forward.
            Vector3 _moveDirection = _inputDirection.X * _right + _inputDirection.Y * _forward;

            //Finalmente agregamos la dirección de movimiento multiplicada por la velocidad de movimiento.
            position += _moveDirection * moveSpeed;

            //Ahora enviaremos la posición y rotación del jugador
            //No estamos enviando posición y rotación en el mismo paquete porque although (a pesar que) queremos enviarle la posición a todos, nosotros tenemos que enviar la rotación a todos excepto al jugador que le (belongs) pertenece (que estamos haciendo ahorita).
            ServerSend.PlayerPosition( this );
            ServerSend.PlayerRotation( this );

            //Nota: El cliente tiene la autoria sobre todas las cosas de rotación (a diferencia del movimiento que esto lo calcula el server)
            //No estamos checando constantemente si la rotación cambio, tomamos la que el servidor recibió como si fuese la correcta.
            //Si quisieramos hacer que el servidor tuviera esa autoria porque tal vez es necesario, necesitariamos investigar sobre client prediction and reconciliation para (avoid) evitar un (jittering) temblor en la pantalla del jugador.


        }

        //Simplemente almacenaremos los valores en los aprametros creados arriba.
        public void SetInput( bool[] _inputs, Quaternion _rotation )
        {
            inputs = _inputs;
            rotation = _rotation;
        }


    }
}
