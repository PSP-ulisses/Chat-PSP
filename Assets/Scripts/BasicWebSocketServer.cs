using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

// Clase que se adjunta a un GameObject en Unity para iniciar el servidor WebSocket.
public class BasicWebSocketServer : MonoBehaviour
{
    // Instancia del servidor WebSocket.
    private WebSocketServer wss;

    // Se ejecuta al iniciar la escena.
    void Start()
    {
        // Verificar si ya hay un servidor WebSocket en ejecución.
        if (wss == null)
        {
            lock (this)
            {
                // Crear un servidor WebSocket que escucha en el puerto 7777.
                wss = new WebSocketServer(7777);

                wss.AddWebSocketService<ChatBehavior>("/");

                // Iniciar el servidor.
                wss.Start();

                Debug.Log("Servidor WebSocket iniciado en ws://127.0.0.1:7777/");
            }
        }
        else
        {
            Debug.Log("El servidor WebSocket ya está en ejecución.");
        }
    }

    // Se ejecuta cuando el objeto se destruye (por ejemplo, al cerrar la aplicación o cambiar de escena).
    void OnDestroy()
    {
        // Si el servidor está activo, se detiene de forma limpia.
        if (wss != null)
        {
            wss.Stop();
            wss = null;
            Debug.Log("Servidor WebSocket detenido.");
        }
    }
}

public class ChatBehavior : WebSocketBehavior
{
    private static int _numOfClients = 0;
    private static readonly object _lock = new();

    // Se invoca cuando se recibe un mensaje desde un cliente.
    protected override void OnMessage(MessageEventArgs e)
    {
        if (e.Data.StartsWith("desc:"))
        {
            Sessions.Broadcast("--- Hasta la vista Cliente" + e.Data.Split(':')[1] + " ---");
        }
        else
        {
            Sessions.Broadcast("<color=" + e.Data.Split(':')[1] + ">Cliente" + e.Data.Split(':')[0] + ":</color> " + e.Data.Split(':')[2]);
        }
    }

    protected override void OnOpen()
    {
        int clientId;
        lock (_lock)
        {
            _numOfClients++;
            clientId = _numOfClients;
        }
        Debug.Log("Cliente con id: " + clientId + " conectado.");
        Send("NewID:" + clientId);
        Sessions.Broadcast("--- Bienvenido Cliente" + clientId + " ---");
    }

    // Se invoca cuando se cierra la conexión con un cliente.
    protected override void OnClose(CloseEventArgs e)
    {
        Debug.Log("Se ha desconectado un cliente.");
    }
}
