using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

// Clase que se adjunta a un GameObject en Unity para iniciar el servidor WebSocket.
public class WSServer : MonoBehaviour
{
    // Instancia del servidor WebSocket.
    private WebSocketServer wss;

    // Se ejecuta al iniciar la escena.
    void Start()
    {
        // Crear un servidor WebSocket que escucha en el puerto 7777.
        wss = new WebSocketServer(7777);

        // Añadir un servicio en la ruta "/" que utiliza el comportamiento EchoBehavior.
        wss.AddWebSocketService<EchoBehavior>("/");

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

// Comportamiento básico del servicio WebSocket: simplemente devuelve el mensaje recibido.
public class EchoBehavior : WebSocketBehavior
{
    // Se invoca cuando se recibe un mensaje desde un cliente.
    protected override void OnMessage(MessageEventArgs e)
    {
        // Envía de vuelta el mismo mensaje recibido.
        Send(e.Data);
    }

    // Se invoca cuando se establece la conexión con un cliente.
    protected override void OnOpen()
    {
        Debug.Log("Cliente conectado.");
    }
}
