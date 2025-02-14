using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

/// <summary>
/// Clase que se adjunta a un GameObject en Unity para iniciar el cliente WebSocket.
/// </summary>
public class ChatWebSocketClient : MonoBehaviour
{
    private WebSocket ws; // WebSocket del cliente
    private int id; // ID del cliente
    private string color; // Color del cliente

    // Colores para los mensajes
    private readonly List<string> colores = new() { "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF" };

    public TMP_Text chatDisplay;  // Texto donde se muestra el historial del chat
    public TMP_InputField inputField; // Input donde el usuario escribe
    public Button sendButton; // Botón para enviar mensajes
    public ScrollRect scrollRect; // Scroll View para manejar el desplazamiento
    public TMP_Text textID; // Texto para mostrar el ID del cliente
    private readonly Queue<Action> _actionsToRun = new(); // Cola de acciones para ejecutar en el hilo principal

    private const int maxRetries = 5; // Número máximo de reintentos de conexión
    private const float retryDelay = 2f; // Tiempo de espera entre reintentos en segundos

    /// <summary>
    /// Se ejecuta al iniciar la escena.
    /// </summary>
    void Start()
    {
        StartCoroutine(Conectar()); // Conectar al servidor WebSocket

        sendButton.onClick.AddListener(SendMessageToServer); // Asignar el método al botón
        inputField.onSubmit.AddListener(delegate { SendMessageToServer(); }); // Asignar el método al input

        // Dar foco automático al input al iniciar
        inputField.Select();
        inputField.ActivateInputField();

        chatDisplay.text = ""; // Limpiar el chat
    }

    /// <summary>
    /// Método para conectar al servidor WebSocket.
    /// </summary>
    /// <returns>Corrutina para la conexión.</returns>
    private IEnumerator Conectar()
    {
        int attempt = 0; // Intento actual de conexión
        while (attempt < maxRetries) // Mientras no se alcance el número máximo de reintentos
        {
            // Crear una instancia del WebSocket apuntando a la URI del servidor
            ws = new WebSocket("ws://127.0.0.1:7777/");

            // Evento OnOpen: se invoca cuando se establece la conexión con el servidor
            ws.OnOpen += (sender, e) => EnqueueUIAction(() =>
            {
                inputField.enabled = true; // Habilitar el input
                ToastNotification.Hide(); // Ocultar notificación de desconexión
                ToastNotification.Show("Conectado al servidor", "success"); // Mostrar notificación de conexión
            });

            // Evento OnMessage: se invoca cuando se recibe un mensaje del servidor
            ws.OnMessage += (sender, e) =>
            {
                if (e.Data.StartsWith("NewID:")) // Si el mensaje es una señal de nuevo ID
                {
                    id = int.Parse(e.Data[6..]); // Obtener el ID del cliente

                    // Asignar un color aleatorio al cliente
                    System.Random random = new();
                    color = colores[random.Next(0, colores.Count)];

                    EnqueueUIAction(() => textID.text = "Cliente" + id); // Mostrar el ID en la UI
                }
                else if (e.Data.StartsWith("desc:")) // Si el mensaje es una señal de desconexión
                {
                    EnqueueUIAction(() =>
                        {
                            ToastNotification.Show("Cliente" + e.Data.Split(':')[1] + " se ha desconectado", "info");
                        }); // Mostrar notificación de desconexión
                }
                else // Si el mensaje es un mensaje de chat
                {
                    PintarMensaje(e.Data); // Mostrar el mensaje en la UI
                }
            };

            // Evento OnError: se invoca cuando ocurre un error en la conexión
            ws.OnError += (sender, e) => Debug.LogError("Error: " + e.Message);

            // Evento OnClose: se invoca cuando se cierra la conexión con el servidor
            ws.OnClose += (sender, e) => EnqueueUIAction(() =>
            {
                inputField.enabled = false; // Deshabilitar el input
                ToastNotification.Show("La conexión está cerrada", 0, "error"); // Mostrar notificación de desconexión
            });

            // Conectar de forma asíncrona al servidor WebSocket
            ws.ConnectAsync();

            // Esperar un tiempo antes de intentar reconectar
            yield return new WaitForSeconds(retryDelay);

            if (ws.ReadyState == WebSocketState.Open) // Si la conexión se estableció con éxito
            {
                yield break; // Salir del bucle
            }

            attempt++; // Incrementar el número de intentos
        }
    }

    /// <summary>
    /// Método para enviar un mensaje al servidor.
    /// </summary>
    public void SendMessageToServer()
    {
        string message = inputField.text; // Obtener el mensaje del input

        if (string.IsNullOrEmpty(message)) // Si el mensaje está vacío
        {
            return; // Salir del método
        }

        if (ws != null && ws.ReadyState == WebSocketState.Open) // Si el cliente está conectado
        {
            if (!message.StartsWith("NewID:") && !message.StartsWith("desc:")) // Si el mensaje no es una señal
            {
                ws.Send(id + ":" + color + ": " + message); // Enviar el mensaje al servidor
            }
            else // Si el mensaje es una señal
            {
                ToastNotification.Show("¡No puedes escribir eso!", 1f, "alert"); // Mostrar notificación de error
            }
        }
        else // Si el cliente no está conectado
        {
            ToastNotification.Show("El cliente no está conectado.", 5f, "error"); // Mostrar notificación de error
            Debug.LogError("El cliente no está conectado.");
        }
    }

    /// <summary>
    /// Método para desconectar del servidor WebSocket.
    /// </summary>
    void OnDestroy()
    {
        if (ws != null) // Si el cliente está conectado
        {
            ws.Send("desc:" + id); // Enviar señal de desconexión
            ws.Close(); // Cerrar la conexión
            ws = null;
        }
    }

    /// <summary>
    /// Encolar una acción para ejecutar en el hilo principal.
    /// </summary>
    /// <param name="action">Acción a encolar.</param>
    private void EnqueueUIAction(Action action)
    {
        lock (_actionsToRun) // Bloquear la cola para evitar problemas de concurrencia
        {
            _actionsToRun.Enqueue(action); // Encolar la acción
        }
    }

    /// <summary>
    /// Método que se ejecuta en cada frame.
    /// </summary>
    void Update()
    {
        if (_actionsToRun.Count > 0) // Si hay acciones en la cola
        {
            Action action;

            lock (_actionsToRun) // Bloquear la cola para evitar problemas de concurrencia
            {
                action = _actionsToRun.Dequeue(); // Desencolar la acción
            }

            action?.Invoke(); // Ejecutar la acción
        }
    }

    /// <summary>
    /// Método para mostrar un mensaje en el chat.
    /// </summary>
    /// <param name="mensaje">Mensaje a mostrar.</param>
    void PintarMensaje(string mensaje) // Método para mostrar un mensaje en el chat
    {
        // Ejecutar en el hilo principal
        EnqueueUIAction(() =>
            {
                chatDisplay.text += "\n" + mensaje; // Añadir el mensaje al chat

                Canvas.ForceUpdateCanvases(); // Forzar la actualización de los Canvas
                scrollRect.verticalNormalizedPosition = 0f; // Mover el Scroll al final

                inputField.text = ""; // Limpiar el input
                inputField.ActivateInputField(); // Activar el input

                // Forzar actualización del Layout para el Scroll
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatDisplay.rectTransform);
            });
    }
}
