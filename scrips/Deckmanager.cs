using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class DeckManager : MonoBehaviour
{
    [Header("APIs")]
    // Reemplaza <usuario>/<repo> por tu usuario y repo de GitHub que contiene db.json
    [SerializeField] private string fakeApiBaseUrl = "https://my-json-server.typicode.com/<usuario>/<repo>/";
    [SerializeField] private string rickAndMortyUrl = "https://rickandmortyapi.com/api/character/";

    [Header("Usuarios disponibles en db.json")]
    [SerializeField] private int[] userIds = { 1, 2, 3 };
    private int currentUserIndex = 0;

    [Header("UI - Info general")]
    [SerializeField] private TMP_Text projectAuthorText; // "Proyecto por: <Tu Nombre Completo>"
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text statusText; // mensajes de carga / error

    [Header("UI - Mazo de cartas (5 espacios FIJOS en pantalla)")]
    // Arrastra aqui los 5 GameObjects de carta que ya existen en el Canvas,
    // cada uno con su componente CardView. No se instancia nada nuevo,
    // solo se actualiza el contenido de estos 5 slots.
    [SerializeField] private CardView[] cardSlots;

    [Header("UI - Cambio de usuario")]
    [SerializeField] private Button nextUserButton;
    [SerializeField] private Button prevUserButton;

    void Start()
    {
        // Nombre del alumno visible en la interfaz (requisito de la actividad)
        if (projectAuthorText != null)
        {
            projectAuthorText.text = "Proyecto por: Nombre Completo Del Estudiante";
        }

        if (nextUserButton != null) nextUserButton.onClick.AddListener(OnNextUserClicked);
        if (prevUserButton != null) prevUserButton.onClick.AddListener(OnPrevUserClicked);

        LoadCurrentUser();
    }

    // ---------- Cambio de usuario ----------

    private bool isLoading = false;

    public void OnNextUserClicked()
    {
        if (isLoading) return; // evita clicks repetidos mientras carga
        currentUserIndex = (currentUserIndex + 1) % userIds.Length;
        LoadCurrentUser();
    }

    public void OnPrevUserClicked()
    {
        if (isLoading) return;
        currentUserIndex = (currentUserIndex - 1 + userIds.Length) % userIds.Length;
        LoadCurrentUser();
    }

    private void LoadCurrentUser()
    {
        StartCoroutine(GetUserProfile(userIds[currentUserIndex]));
    }

    // ---------- Consulta a la API falsa (db.json) ----------

    private IEnumerator GetUserProfile(int userId)
    {
        isLoading = true;
        SetButtonsInteractable(false);
        SetStatus("Cargando usuario...");

        string url = fakeApiBaseUrl + "users/" + userId;
        using UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
            SetStatus("Error al cargar el usuario: " + www.error);
            isLoading = false;
            SetButtonsInteractable(true);
            yield break;
        }

        UserInfo userInfo = JsonUtility.FromJson<UserInfo>(www.downloadHandler.text);

        if (usernameText != null)
        {
            usernameText.text = "Usuario: " + userInfo.username;
        }

        if (userInfo.deck == null || userInfo.deck.Length == 0)
        {
            SetStatus("Este usuario no tiene cartas en su mazo.");
            isLoading = false;
            SetButtonsInteractable(true);
            yield break;
        }

        SetStatus("Cargando mazo...");

        // Se espera que db.json tenga exactamente 5 IDs por usuario,
        // uno por cada slot fijo en pantalla.
        int count = Mathf.Min(userInfo.deck.Length, cardSlots.Length);

        // IMPORTANTE: en vez de hacer 5 peticiones (una por carta),
        // pedimos los 5 personajes en UNA sola llamada. Esto evita el
        // error 429 (Too Many Requests) por exceso de peticiones simultaneas.
        int[] idsToFetch = new int[count];
        for (int i = 0; i < count; i++)
        {
            idsToFetch[i] = userInfo.deck[i];
        }

        StartCoroutine(GetCharacters(idsToFetch));
    }

    [Header("Reintentos")]
    [SerializeField] private int maxRetries = 5;
    [SerializeField] private float retryDelaySeconds = 4f;

    // ---------- Consulta a la API de terceros (Rick and Morty) ----------

    // Pide varios personajes a la vez: /character/1,2,3,4,5
    private IEnumerator GetCharacters(int[] characterIds)
    {
        string idsParam = string.Join(",", characterIds);
        string url = rickAndMortyUrl + idsParam;

        int attempt = 0;
        UnityWebRequest www = null;

        while (attempt < maxRetries)
        {
            www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                break; // exito, salimos del reintento
            }

            attempt++;

            bool isRateLimited = www.responseCode == 429;
            www.Dispose();

            if (attempt >= maxRetries || !isRateLimited)
            {
                Debug.LogError("No se pudo cargar el mazo tras " + attempt + " intento(s).");
                SetStatus("Error al cargar el mazo (intenta de nuevo en unos segundos).");
                isLoading = false;
                SetButtonsInteractable(true);
                yield break;
            }

            SetStatus("Demasiadas peticiones, reintentando en " + retryDelaySeconds + "s...");
            yield return new WaitForSeconds(retryDelaySeconds);
        }

        // La API devuelve un array JSON "[...]" cuando pides varios IDs.
        // JsonUtility no puede parsear un array directo, asi que lo
        // envolvemos en un objeto antes de parsear.
        string wrappedJson = "{\"characters\":" + www.downloadHandler.text + "}";
        www.Dispose();
        CharacterListWrapper wrapper = JsonUtility.FromJson<CharacterListWrapper>(wrappedJson);

        int count = Mathf.Min(wrapper.characters.Length, cardSlots.Length);
        for (int i = 0; i < count; i++)
        {
            Character character = wrapper.characters[i];
            CardView slot = cardSlots[i];

            slot.SetTextData(character);
            StartCoroutine(GetImage(character.image, slot));
        }

        SetStatus(string.Empty);
        isLoading = false;
        SetButtonsInteractable(true);
    }

    private IEnumerator GetImage(string imageUrl, CardView cardView)
    {
        using UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(www.error);
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(www);
        if (cardView != null)
        {
            cardView.SetImage(texture);
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void SetButtonsInteractable(bool value)
    {
        if (nextUserButton != null) nextUserButton.interactable = value;
        if (prevUserButton != null) prevUserButton.interactable = value;
    }
}