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
        // Crear un servidor WebSocket que escucha en el puerto 7777.
        wss = new WebSocketServer(7777);

        wss.AddWebSocketService<ChatBehavior>("/");

        // Iniciar el servidor.
        wss.Start();

        Debug.Log("Servidor WebSocket iniciado en ws://127.0.0.1:7777/");
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
    private int _numOfClients = 0;

    // Se invoca cuando se recibe un mensaje desde un cliente.
    protected override void OnMessage(MessageEventArgs e)
    {
        // Envía de vuelta el mismo mensaje recibido.
        Send(e.Data);
    }

    // Se invoca cuando se establece la conexión con un cliente.
    protected override void OnOpen()
    {
        _numOfClients++;
        LogServidor("Cliente con id: " + _numOfClients + " conectado.");
        Send("NewID:" + _numOfClients);
    }

    // Se invoca cuando se cierra la conexión con un cliente.
    protected override void OnClose(CloseEventArgs e)
    {
        LogServidor("Se ha desconectado un cliente.");
    }

    private void LogServidor(string data)
    {
        Debug.Log("<color=yellow>SERVb:</color> " + data);
    }
}
