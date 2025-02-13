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

    private readonly List<string> colores = new() { "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF" };

    public TMP_Text chatDisplay;  // Texto donde se muestra el historial del chat
    public TMP_InputField inputField; // Input donde el usuario escribe
    public Button sendButton; // Botón para enviar mensajes
    public ScrollRect scrollRect; // Scroll View para manejar el desplazamiento
    public TMP_Text textID;
    public TMP_Text conStatus;
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
            ws.OnOpen += (sender, e) => EnqueueUIAction(() => conStatus.enabled = false);

            // Evento OnMessage: se invoca cuando se recibe un mensaje del servidor
            ws.OnMessage += (sender, e) =>
            {
                if (e.Data.StartsWith("NewID:"))
                {
                    id = int.Parse(e.Data[6..]);
                    System.Random random = new();
                    color = colores[random.Next(0, colores.Count)];
                    EnqueueUIAction(() => textID.text = "Cliente" + id);
                }
                else
                {
                    EnqueueUIAction(() =>
                {
                    chatDisplay.text += "\n" + e.Data;
                    Canvas.ForceUpdateCanvases();
                    scrollRect.verticalNormalizedPosition = 0f;
                    inputField.text = "";
                    inputField.ActivateInputField();
                    // Forzar actualización del Layout para el Scroll
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chatDisplay.rectTransform);
                });
                }
            };

            // Evento OnError: se invoca cuando ocurre un error en la conexión
            ws.OnError += (sender, e) => EnqueueUIAction(() => Debug.LogError("Error: " + e.Message));

            // Evento OnClose: se invoca cuando se cierra la conexión con el servidor
            ws.OnClose += (sender, e) => EnqueueUIAction(() => conStatus.enabled = true);

            // Conectar de forma asíncrona al servidor WebSocket
            ws.ConnectAsync();

            // Esperar un tiempo antes de intentar reconectar
            yield return new WaitForSeconds(retryDelay);

            if (ws.ReadyState == WebSocketState.Open)
            {
                yield break;
            }

            attempt++;
        }
    }

    // Método para enviar un mensaje al servidor (puedes llamarlo, por ejemplo, desde un botón en la UI)
    public void SendMessageToServer()
    {
        string message = inputField.text;

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            if (!message.StartsWith("NewID:") && !message.StartsWith("desc:"))
            {
                ws.Send(id + ":" + color + ": " + message);
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }
    }

    // Se ejecuta cuando el objeto se destruye (por ejemplo, al cambiar de escena o cerrar la aplicación)
    void OnDestroy()
    {
        if (ws != null)
        {
            ws.Send("desc:" + id);
            ws.Close();
            ws = null;
        }
    }

    void ScrollToBottom()
    {

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
