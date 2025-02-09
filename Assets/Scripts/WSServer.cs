using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

// Clase que se adjunta a un GameObject en Unity para iniciar el servidor WebSocket.
public class WSServer : MonoBehaviour
{
    // Instancia del servidor WebSocket.
    private WebSocketServer wss;

    private static List<ClienteDelChat> clientes = new List<ClienteDelChat>();

    // Cola de acciones para ejecutar en el hilo principal de Unity.
    private readonly Queue<Action> _actionsToRun = new Queue<Action>();

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

    private void EnqueueUIAction(Action action)
    {
        lock (_actionsToRun)
        {
            _actionsToRun.Enqueue(action);
        }
    }

    void Update()
    {
        if (_actionsToRun.Count > 0)
        {
            Action action;

            lock (_actionsToRun)
            {
                action = _actionsToRun.Dequeue();
            }

            action?.Invoke();
        }
    }

    public static void nuevoCliente() // TODO: Cambiar a privado
    {
        string id = Guid.NewGuid().ToString();
        string color = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV());
        clientes.Add(new ClienteDelChat(id, color));

        Debug.Log("Nuevo cliente conectado: " + id);
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
        WSServer.nuevoCliente();
    }
}

public class ClienteDelChat
{
    string id;
    string color;

    public ClienteDelChat(string id, string color)
    {
        this.id = id;
        this.color = color;
    }
}
