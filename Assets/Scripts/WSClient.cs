using UnityEngine;
using WebSocketSharp;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class WSClient : MonoBehaviour
{
    // Instancia del cliente WebSocket
    private WebSocket ws;

    public TMP_Text chatDisplay;  // Texto donde se muestra el historial del chat
    public TMP_InputField inputField; // Input donde el usuario escribe
    public Button sendButton; // Botón para enviar mensajes
    public ScrollRect scrollRect; // Scroll View para manejar el desplazamiento

    private const int maxRetries = 5;
    private const float retryDelay = 2f;

    // Se ejecuta al iniciar la escena
    void Start()
    {
        // Crear una instancia del WebSocket apuntando a la URI del servidor
        ws = new WebSocket("ws://127.0.0.1:7777/");

        // Evento OnOpen: se invoca cuando se establece la conexión con el servidor
        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("WebSocket conectado correctamente.");
        };

        // Evento OnMessage: se invoca cuando se recibe un mensaje del servidor
        ws.OnMessage += (sender, e) =>
        {
            Debug.Log("Mensaje recibido: " + e.Data);
        };

        // Evento OnError: se invoca cuando ocurre un error en la conexión
        ws.OnError += (sender, e) =>
        {
            Debug.LogError("Error en el WebSocket: " + e.Message);
        };

        // Evento OnClose: se invoca cuando se cierra la conexión con el servidor
        ws.OnClose += (sender, e) =>
        {
            Debug.Log("WebSocket cerrado. Código: " + e.Code + ", Razón: " + e.Reason);
        };

        StartCoroutine(ConectarAlServidor());

        // Dar foco automático al input al iniciar
        inputField.Select();
        inputField.ActivateInputField();

        sendButton.onClick.AddListener(OnSendButtonClick);
        inputField.onSubmit.AddListener(delegate { OnSendButtonClick(); });
    }

    public void OnSendButtonClick()
    {
        SendMessageToServer(inputField.text);
        inputField.text = "";
        inputField.ActivateInputField();
    }

    // Método para enviar un mensaje al servidor (puedes llamarlo, por ejemplo, desde un botón en la UI)
    public void SendMessageToServer(string message)
    {
        if (ws != null && ws.ReadyState == WebSocketState.Open)
        {
            ws.Send(message);
        }
        else
        {
            Debug.LogError("No se puede enviar el mensaje. La conexión no está abierta.");
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

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private IEnumerator ConectarAlServidor()
    {
        int attempt = 0;
        while (attempt < maxRetries)
        {
            ws.ConnectAsync();
            yield return new WaitForSeconds(retryDelay);

            if (ws.ReadyState == WebSocketState.Open)
            {
                Debug.Log("Conexión establecida después de " + (attempt + 1) + " intentos.");
                yield break;
            }

            attempt++;
            Debug.Log("Reintentando conexión... Intento " + attempt);
        }

        Debug.LogError("No se pudo establecer la conexión después de " + maxRetries + " intentos.");
    }
}
