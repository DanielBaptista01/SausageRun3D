using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Etapa 5: gerencia objetivo, vitoria e recuperacao da bolinha apos captura.
/// Compatibilidade: funciona SEM modificar o MariaAI existente. A MariaAI
/// desativa ScoobyController e ScoobyEscalada quando captura o jogador.
/// </summary>
public class SausageRunGameManager : MonoBehaviour
{
    [Header("Referencias")]
    public Transform scooby;
    public ScoobyController movimentoScooby;
    public ScoobyEscalada escaladaScooby;
    public MariaAI maria;
    public BolinhaColetavel bolinha;

    [Header("Area de vitoria: corredor externo")]
    public float zMaximoParaVitoria = -5.4f;
    public float zMinimoDoCorredor = -8.2f;
    public float xMaximoCorredor = 5.8f;

    [Header("Interface")]
    public bool mostrarObjetivo = true;

    public bool Venceu { get; private set; }
    public bool JogadorBloqueado { get { return Venceu || capturadoAgora; } }

    private bool capturadoAgora;
    private Vector3 posicaoInicialScooby;
    private Quaternion rotacaoInicialScooby;
    private Vector3 posicaoInicialMaria;

    private void Awake()
    {
        if (scooby == null)
        {
            GameObject go = GameObject.Find("Scooby");
            if (go != null) scooby = go.transform;
        }
        if (scooby != null)
        {
            if (movimentoScooby == null) movimentoScooby = scooby.GetComponent<ScoobyController>();
            if (escaladaScooby == null) escaladaScooby = scooby.GetComponent<ScoobyEscalada>();
            posicaoInicialScooby = scooby.position;
            rotacaoInicialScooby = scooby.rotation;
        }
        if (maria == null) maria = FindFirstObjectByType<MariaAI>();
        if (bolinha == null) bolinha = FindFirstObjectByType<BolinhaColetavel>();
        if (maria != null) posicaoInicialMaria = maria.transform.position;
    }

    private void Update()
    {
        if (Venceu)
        {
            if (TeclaReiniciarPressionada()) ReiniciarPartida();
            return;
        }

        // A IA existente desativa AMBOS os scripts quando Scooby e capturado.
        // Durante a escalada apenas ScoobyController e desativado, evitando
        // confundir uma escalada normal com a captura pela Maria.
        bool capturadoNesteFrame = maria != null && maria.isActiveAndEnabled &&
            movimentoScooby != null && escaladaScooby != null &&
            !movimentoScooby.enabled && !escaladaScooby.enabled;

        if (capturadoNesteFrame && !capturadoAgora && bolinha != null)
            bolinha.RestaurarBolinha();

        capturadoAgora = capturadoNesteFrame;
        if (capturadoAgora || scooby == null || bolinha == null || !bolinha.Carregada)
            return;

        Vector3 p = scooby.position;
        bool estaNoCorredor = p.z <= zMaximoParaVitoria &&
            p.z >= zMinimoDoCorredor && Mathf.Abs(p.x) <= xMaximoCorredor;

        if (estaNoCorredor) ConcluirPartida();
    }

    private static bool TeclaReiniciarPressionada()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#else
        return false;
#endif
    }

    private void ConcluirPartida()
    {
        Venceu = true;
        if (movimentoScooby != null) movimentoScooby.enabled = false;
        if (escaladaScooby != null) escaladaScooby.enabled = false;

        if (maria != null)
        {
            maria.enabled = false;
            NavMeshAgent agente = maria.GetComponent<NavMeshAgent>();
            if (agente != null && agente.isOnNavMesh)
            {
                agente.isStopped = true;
                agente.ResetPath();
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("VITORIA! Scooby voltou ao corredor com a bolinha!");
    }

    private void ReiniciarPartida()
    {
        Scene cena = SceneManager.GetActiveScene();
        if (cena.buildIndex >= 0)
        {
            SceneManager.LoadScene(cena.buildIndex);
            return;
        }

        // Alternativa para cenas que ainda nao foram incluidas no Build Profile.
        Debug.LogWarning("Cena nao esta no Build Profile: reiniciando objetos manualmente.");
        if (bolinha != null) bolinha.RestaurarBolinha();
        if (scooby != null)
        {
            CharacterController cc = scooby.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            scooby.SetPositionAndRotation(posicaoInicialScooby, rotacaoInicialScooby);
            if (cc != null) cc.enabled = true;
        }
        if (maria != null)
        {
            NavMeshAgent agente = maria.GetComponent<NavMeshAgent>();
            if (agente != null && agente.isOnNavMesh)
            {
                agente.Warp(posicaoInicialMaria);
                agente.isStopped = false;
            }
            maria.estadoAtual = MariaAI.EstadoMaria.Patrulhando;
            maria.enabled = true;
        }
        if (movimentoScooby != null) movimentoScooby.enabled = true;
        if (escaladaScooby != null) escaladaScooby.enabled = true;
        capturadoAgora = false;
        Venceu = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnGUI()
    {
        if (mostrarObjetivo && !Venceu)
        {
            string objetivo = bolinha != null && bolinha.Carregada
                ? "BOLINHA ENCONTRADA!\nVolte pela janela para o corredor."
                : "OBJETIVO\nEntre na sala e encontre a bolinha.";
            GUI.Box(new Rect(Mathf.Max(10f, Screen.width - 310f), 12f, 298f, 64f), objetivo);
        }

        if (!Venceu) return;

        const float w = 390f;
        const float h = 185f;
        Rect painel = new Rect((Screen.width - w) * 0.5f,
            (Screen.height - h) * 0.5f, w, h);
        GUI.Box(painel, "SCOOBY CONSEGUIU!");
        GUI.Label(new Rect(painel.x + 35f, painel.y + 52f, 340f, 55f),
            "Voce resgatou a bolinha e escapou da Maria!\nSausage Run 3D — Vitoria!");
        if (GUI.Button(new Rect(painel.x + 110f, painel.y + 124f, 170f, 36f),
            "Jogar novamente (R)"))
        {
            ReiniciarPartida();
        }
    }
}
