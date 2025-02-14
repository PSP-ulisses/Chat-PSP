using System;
using System.IO;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

/// <summary>
/// Clase que se adjunta a un GameObject en Unity para iniciar el servidor WebSocket.
/// </summary>
public class BasicWebSocketServer : MonoBehaviour
{
    private WebSocketServer wss; // Servidor WebSocket.

    /// <summary>
    /// Se ejecuta al iniciar el script.
    /// </summary>
    void Start()
    {
        if (wss == null) // Si el servidor no está en ejecución.
        {
            lock (this) // Se bloquea el hilo para evitar que se cree más de un servidor.
            {
                wss = new WebSocketServer(7777); // Se crea el servidor en el puerto 7777.
                wss.AddWebSocketService<ChatBehavior>("/"); // Se añade el servicio de chat.

                wss.Start(); // Se inicia el servidor.
                Debug.Log("Servidor WebSocket iniciado en ws://127.0.0.1:7777/");
            }
        }
        else // Si el servidor ya está en ejecución
        {
            Debug.Log("El servidor WebSocket ya está en ejecución.");
        }
    }

    /// <summary>
    /// Se ejecuta al destruir el script, se detiene el servidor WebSocket.
    /// </summary>
    void OnDestroy()
    {
        if (wss != null) // Si el servidor está en ejecución.
        {
            wss.Stop(); // Se detiene el servidor limpiamente.
            wss = null;
            Debug.Log("Servidor WebSocket detenido.");
        }
    }
}

/// <summary>
/// Clase que se encarga de gestionar la comunicación con los clientes.
/// </summary>
public class ChatBehavior : WebSocketBehavior
{
    private static int _numOfIDs = 0; // Número de IDs asignados.
    private static int _numOfClients = 0; // Número de clientes conectados en el momento.
    private static readonly object _lock = new(); // Objeto para bloquear el hilo.
    private static string historial = ""; // Historial de mensajes.


    /// <summary>
    /// Se ejecuta cuando se recibe un mensaje de un cliente.
    /// </summary>
    /// <param name="e">Argumentos del mensaje.</param>
    protected override void OnMessage(MessageEventArgs e)
    {
        if (e.Data.StartsWith("desc:")) // Si el mensaje es una señal de desconexión.
        {
            Sessions.Broadcast(e.Data); // Se reenvía la señal a todos los clientes.
        }
        else // Si el mensaje es un mensaje de chat.
        {
            // Se formatea el mensaje y se reenvía a todos los clientes.
            Sessions.Broadcast("<color=" + e.Data.Split(':')[1] + ">Cliente" + e.Data.Split(':')[0] + ":</color> " + e.Data.Split(':')[2]);

            // Se añade el mensaje al historial.
            historial += "[" + DateTime.Now.ToString("dd-MM-yyyy HH:mm") + "] Cliente" + e.Data.Split(':')[0] + ":" + e.Data.Split(':')[2] + "\n";
        }
    }

    /// <summary>
    /// Se ejecuta cuando conecta un cliente.
    /// </summary>
    protected override void OnOpen()
    {
        int clientId; // ID del cliente.
        lock (_lock) // Se bloquea el hilo para evitar problemas de concurrencia.
        {
            _numOfIDs++; // Se incrementa el número de IDs asignados.
            clientId = _numOfIDs; // Se asigna el ID al cliente.
            _numOfClients++; // Se incrementa el número de clientes conectados.
        }
        Send("NewID:" + clientId); // Se envía el ID al cliente.

        // Se envía un mensaje de bienvenida a todos los clientes.
        Sessions.Broadcast("--- Bienvenido Cliente" + clientId + " ---");

        Debug.Log("Cliente con id: " + clientId + " conectado.");
    }

    /// <summary>
    /// Se ejecuta cuando se desconecta un cliente.
    /// </summary>
    /// <param name="e">Argumentos de la desconexión.</param>
    protected override void OnClose(CloseEventArgs e)
    {
        lock (_lock) // Se bloquea el hilo para evitar problemas de concurrencia.
        {
            _numOfClients--; // Se decrementa el número de clientes conectados.
        }
        if (_numOfClients == 0) // Si no hay más clientes conectados.
        {
            // Se guarda el historial en un archivo.
            File.WriteAllText(Path.Combine(Application.dataPath, "../..", DateTime.Now.ToString("dd-MM-yyyy_HH-mm") + "_hist.txt"), historial);
        }

        Debug.Log("Se ha desconectado un cliente.");
    }
}
