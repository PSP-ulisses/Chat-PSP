using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class BasicWebSocketClient : MonoBehaviour
{
    // Instancia del cliente WebSocket
    private WebSocket ws;
    private int id;
    private string color;

    public TMP_Text chatDisplay;  // Texto donde se muestra el historial del chat
    public TMP_InputField inputField; // Input donde el usuario escribe
    public Button sendButton; // Botón para enviar mensajes
    public ScrollRect scrollRect; // Scroll View para manejar el desplazamiento
    private readonly Queue<Action> _actionsToRun = new();

    private const int maxRetries = 5; // Número máximo de reintentos
    private const float retryDelay = 2f; // Tiempo de espera entre reintentos en segundos

    // Se ejecuta al iniciar la escena
    void Start()
    {
        StartCoroutine(Conectar());

        sendButton.onClick.AddListener(SendMessageToServer);
        inputField.onSubmit.AddListener(delegate { SendMessageToServer(); });

        // Dar foco automático al input al iniciar
        inputField.Select();
        inputField.ActivateInputField();

        // Limpiar el chatDisplay
        chatDisplay.text = "";
    }

    private IEnumerator Conectar()
    {
        int attempt = 0;
        while (attempt < maxRetries)
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
                    System.Random random = new();
                    color = string.Format("#{0:X6}", random.Next(0x1000000));
                    LogCliente("Soy el cliente con ID: " + id);
                    EnqueueUIAction(() => chatDisplay.text += "\nBienvenido <color=" + color + ">Cliente" + id + "</color>.");
                }
                else
                {
                    LogCliente("Mensaje recibido: " + e.Data);
                    EnqueueUIAction(() => chatDisplay.text += "\n" + e.Data);

                    // Limpiar input y mantener el foco
                    inputField.text = "";
                    inputField.ActivateInputField();

                    // Forzar actualización del Layout para el Scroll
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatDisplay.rectTransform);

                    // Hacer que el Scroll se desplace hasta el final
                    ScrollToBottom();
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
            };

            // Conectar de forma asíncrona al servidor WebSocket
            ws.ConnectAsync();

            // Esperar un tiempo antes de intentar reconectar
            yield return new WaitForSeconds(retryDelay);

            if (ws.ReadyState == WebSocketState.Open)
            {
                LogCliente("Conexión establecida después de " + (attempt + 1) + " intentos.");
                yield break;
            }

            attempt++;
            LogCliente("Reintentando conexión... Intento " + attempt);
        }

        LogCliente("No se pudo establecer la conexión después de " + maxRetries + " intentos.");
    }

    // Método para enviar un mensaje al servidor (puedes llamarlo, por ejemplo, desde un botón en la UI)
    public void SendMessageToServer()
    {
        string message = inputField.text;

        if (string.IsNullOrEmpty(message))
        {
            LogCliente("No se puede enviar el mensaje. El mensaje está vacío.");
            return;
        }

        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            if (!message.StartsWith("NewID:"))
            {
                ws.Send(id + ":" + color + ": " + message);
            }
            else
            {
                LogCliente("No se puede enviar el mensaje. El mensaje no puede comenzar con 'NewID:'.");
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

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
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
}
