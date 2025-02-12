using System.Collections.Generic;
using Unity.VisualScripting;
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

    public static void ClienteConectado(Cliente cliente)
    {
        Debug.Log("Cliente conectado: " + cliente.ToString() + ".");
    }
}

public class ChatBehavior : WebSocketBehavior
{

    private static List<Cliente> clientes = new List<Cliente>();

    // Se invoca cuando se recibe un mensaje desde un cliente.
    protected override void OnMessage(MessageEventArgs e)
    {
        // Envía de vuelta el mismo mensaje recibido.
        Send(e.Data);
    }

    protected override void OnOpen()
    {
        clientes.Add(new Cliente(clientes.Count, "#" + Random.ColorHSV().ToHexString()));
        BasicWebSocketServer.ClienteConectado(clientes[^1]);
    }
}

public class Cliente
{
    public int id;
    public string color;

    public Cliente(int id, string color)
    {
        this.id = id;
        this.color = color;
    }

    public override string ToString()
    {
        return "Cliente #" + id + " (" + color + ")";
    }
}
