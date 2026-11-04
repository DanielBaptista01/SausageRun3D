using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Coleta a bolinha pressionando E perto dela e cria uma representacao visual
/// junto a camera do Scooby. Nao altera o ScoobyController nem a ScoobyEscalada.
/// </summary>
public class BolinhaColetavel : MonoBehaviour
{
    [Header("Referencias")]
    public Transform scooby;
    public Camera cameraScooby;
    public SausageRunGameManager jogo;

    [Header("Interacao")]
    [Min(0.2f)] public float distanciaColeta = 1.25f;
    public bool mostrarDica = true;

    public bool Carregada { get; private set; }

    private Vector3 posicaoOriginal;
    private Quaternion rotacaoOriginal;
    private Renderer renderizador;
    private Collider colisor;
    private GameObject bolinhaNaBoca;

    private void Awake()
    {
        posicaoOriginal = transform.position;
        rotacaoOriginal = transform.rotation;
        renderizador = GetComponent<Renderer>();
        colisor = GetComponent<Collider>();

        if (scooby == null)
        {
            GameObject go = GameObject.Find("Scooby");
            if (go != null) scooby = go.transform;
        }
        if (cameraScooby == null && scooby != null)
            cameraScooby = scooby.GetComponentInChildren<Camera>();
        if (jogo == null)
            jogo = FindFirstObjectByType<SausageRunGameManager>();
    }

    private void Update()
    {
        if (Carregada || scooby == null || (jogo != null && jogo.JogadorBloqueado))
            return;

        if (EstaAoAlcance() && TeclaInteracaoPressionada())
            Coletar();
    }

    private bool EstaAoAlcance()
    {
        Vector3 posScooby = scooby.position + Vector3.up * 0.35f;
        return Vector3.Distance(posScooby, transform.position) <= distanciaColeta;
    }

    private static bool TeclaInteracaoPressionada()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.E);
#else
        return false;
#endif
    }

    private void Coletar()
    {
        Carregada = true;
        if (renderizador != null) renderizador.enabled = false;
        if (colisor != null) colisor.enabled = false;

        if (cameraScooby != null)
        {
            bolinhaNaBoca = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolinhaNaBoca.name = "Bolinha_Visual_Na_Boca";
            Collider colliderVisual = bolinhaNaBoca.GetComponent<Collider>();
            if (colliderVisual != null) Destroy(colliderVisual);

            bolinhaNaBoca.transform.SetParent(cameraScooby.transform, false);
            bolinhaNaBoca.transform.localPosition = new Vector3(0.22f, -0.20f, 0.55f);
            bolinhaNaBoca.transform.localRotation = Quaternion.identity;
            bolinhaNaBoca.transform.localScale = Vector3.one * 0.16f;

            Renderer visualNaBoca = bolinhaNaBoca.GetComponent<Renderer>();
            if (renderizador != null && visualNaBoca != null)
                visualNaBoca.sharedMaterial = renderizador.sharedMaterial;
        }

        Debug.Log("Scooby pegou a bolinha! Volte pela janela para o corredor.");
    }

    public void RestaurarBolinha()
    {
        Carregada = false;
        transform.SetPositionAndRotation(posicaoOriginal, rotacaoOriginal);
        if (renderizador != null) renderizador.enabled = true;
        if (colisor != null) colisor.enabled = true;
        if (bolinhaNaBoca != null)
        {
            Destroy(bolinhaNaBoca);
            bolinhaNaBoca = null;
        }
    }

    private void OnGUI()
    {
        if (!mostrarDica || Carregada || scooby == null ||
            (jogo != null && jogo.JogadorBloqueado) || !EstaAoAlcance())
            return;

        const float largura = 260f;
        Rect caixa = new Rect((Screen.width - largura) * 0.5f, Screen.height - 95f,
            largura, 38f);
        GUI.Box(caixa, "[E] Pegar bolinha");
    }
}
