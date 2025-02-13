using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class BasicWebSocketClient : MonoBehaviour
{
    // Instancia del cliente WebSocket
    private WebSocket ws;
    private int id;

    public TMP_Text chatDisplay;  // Texto donde se muestra el historial del chat
    public TMP_InputField inputField; // Input donde el usuario escribe
    public Button sendButton; // Botón para enviar mensajes
    public ScrollRect scrollRect; // Scroll View para manejar el desplazamiento

    // Se ejecuta al iniciar la escena
    void Start()
    {
        // Crear una instancia del WebSocket apuntando a la URI del servidor
        ws = new WebSocket("ws://127.0.0.1:7777/");

        // Evento OnOpen: se invoca cuando se establece la conexión con el servidor
        ws.OnOpen += (sender, e) =>
        {
            LogCliente("WebSocket conectado correctamente.");
        };

        // Evento OnMessage: se invoca cuando se recibe un mensaje del servidor
        ws.OnMessage += (sender, e) =>
        {
            if (e.Data.StartsWith("NewID:"))
            {
                id = int.Parse(e.Data[6..]);
                LogCliente("Soy el cliente con ID: " + id);
                ws.Close();
            }
            else
            {
                LogCliente("Mensaje recibido: " + e.Data);
            }
        };

        // Evento OnError: se invoca cuando ocurre un error en la conexión
        ws.OnError += (sender, e) =>
        {
            LogCliente("Error en el WebSocket: " + e.Message);
        };

        // Evento OnClose: se invoca cuando se cierra la conexión con el servidor
        ws.OnClose += (sender, e) =>
        {
            LogCliente("Soy el cliente con ID: " + id + ", y me voy a cerrar.");
            SendMessageToServer("closing:" + id);
        };

        // Conectar de forma asíncrona al servidor WebSocket
        ws.ConnectAsync();
    }

    // Método para enviar un mensaje al servidor (puedes llamarlo, por ejemplo, desde un botón en la UI)
    public void SendMessageToServer(string message)
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            if (!message.StartsWith("NewID:"))
            {
                ws.Send(id + ": " + message);
            }
            else
            {
                // TODO: Hacerlo más serio.
                LogCliente("Vas de listo pero he capturado tu trampa.");
            }
        }
        else
        {
            LogCliente("No se puede enviar el mensaje. La conexión no está abierta.");
        }
    }

    // Se ejecuta cuando el objeto se destruye (por ejemplo, al cambiar de escena o cerrar la aplicación)
    void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }
    }

    public static void LogCliente(string data)
    {
        Debug.Log("<color=blue>CLI:</color> " + data);
    }
}
