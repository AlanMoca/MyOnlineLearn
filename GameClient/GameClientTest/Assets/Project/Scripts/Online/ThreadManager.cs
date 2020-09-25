using System;
using System.Collections.Generic;
using UnityEngine;

//NOTA: Writing es un tema muy complejo con un montón de cosas que pueden ir mal, puede producer muchos errores de inconsistencia que hará muy difícil debuggear así que para mantener las cosas simples correremos todo en el mismo thread (hilo).
//Esta clase nos ayuda a programar el código que corra sólo en un especifico Thread (hilo). Que nos ayudará a evitar errores que no veríamos.
public class ThreadManager : MonoBehaviour
{
    private static readonly List<Action> executeOnMainThread = new List<Action>();
    private static readonly List<Action> executeCopiedOnMainThread = new List<Action>();
    private static bool actionToExecuteOnMainThread = false;

    private void Update()
    {
        UpdateMain();
    }

    /// <summary>Sets an action to be executed on the main thread.</summary>
    /// <param name="_action">The action to be executed on the main thread.</param>
    public static void ExecuteOnMainThread( Action _action )
    {
        if ( _action == null )
        {
            Debug.Log( "No action to execute on main thread!" );
            return;
        }

        lock ( executeOnMainThread )
        {
            executeOnMainThread.Add( _action );
            actionToExecuteOnMainThread = true;
        }
    }

    /// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
    public static void UpdateMain()
    {
        if ( actionToExecuteOnMainThread )
        {
            executeCopiedOnMainThread.Clear();
            lock ( executeOnMainThread )
            {
                executeCopiedOnMainThread.AddRange( executeOnMainThread );
                executeOnMainThread.Clear();
                actionToExecuteOnMainThread = false;
            }

            for ( int i = 0; i < executeCopiedOnMainThread.Count; i++ )
            {
                executeCopiedOnMainThread[i]();
            }
        }
    }
}
