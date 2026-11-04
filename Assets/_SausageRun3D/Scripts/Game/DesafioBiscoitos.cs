using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Missao: coletar os 5 biscoitos para liberar a bolinha.
/// O SausageRunGameManager original continua responsavel pela vitoria.
/// Na captura pela Maria, os biscoitos retornam a seus lugares.
/// </summary>
public class DesafioBiscoitos : MonoBehaviour
{
    [Header("Referencias")]
    public BiscoitoColetavel[] biscoitos = new BiscoitoColetavel[0];
    public BolinhaColetavel bolinha;
    public GameObject portaoDaBolinha;
    public SausageRunGameManager jogo;
    public ScoobyController movimentoScooby;
    public ScoobyEscalada escaladaScooby;

    [Header("Interface")]
    public bool mostrarHUD = true;
    public float duracaoAviso = 3.0f;

    private readonly HashSet<BiscoitoColetavel> coletados = new HashSet<BiscoitoColetavel>();
    private string aviso = "";
    private float ateQuandoAviso;
    private bool capturadoNoFrameAnterior;
    private bool venceuNoFrameAnterior;

    public int Quantidade => coletados.Count;
    public int Total => biscoitos != null ? biscoitos.Length : 0;
    public bool BolinhaLiberada => Total > 0 && Quantidade >= Total;
    public bool JogoEncerrado => jogo != null && jogo.JogadorBloqueado;

    private void Awake()
    {
        if (jogo == null) jogo = FindFirstObjectByType<SausageRunGameManager>();
        if (bolinha == null) bolinha = FindFirstObjectByType<BolinhaColetavel>(FindObjectsInactive.Include);
        if (movimentoScooby == null) movimentoScooby = FindFirstObjectByType<ScoobyController>();
        if (escaladaScooby == null) escaladaScooby = FindFirstObjectByType<ScoobyEscalada>();
        if (biscoitos == null) biscoitos = new BiscoitoColetavel[0];
        foreach (BiscoitoColetavel b in biscoitos)
            if (b != null) b.desafio = this;

        DefinirTrava(true);
    }

    private void Update()
    {
        // Quando a Maria captura Scooby, ambos os controladores sao desativados.
        // Ao escalar somente ScoobyController e desativado; nao confundir escalada com captura.
        bool venceu = jogo != null && jogo.Venceu;
        bool capturado = !venceu && movimentoScooby != null && escaladaScooby != null &&
                         !movimentoScooby.enabled && !escaladaScooby.enabled;
        // Recomeço manual do SausageRunGameManager quando a cena ainda nao esta no Build Profile.
        if (venceuNoFrameAnterior && !venceu)
            ReiniciarColecao();
        venceuNoFrameAnterior = venceu;
        if (capturado && !capturadoNoFrameAnterior)
            ReiniciarColecao();
        capturadoNoFrameAnterior = capturado;
    }

    public bool RegistrarColeta(BiscoitoColetavel biscoito)
    {
        if (biscoito == null || JogoEncerrado || !coletados.Add(biscoito))
            return false;

        aviso = "Biscoito encontrado: " + biscoito.nomeDesafio;
        ateQuandoAviso = Time.time + duracaoAviso;

        if (BolinhaLiberada)
        {
            DefinirTrava(false);
            aviso = "5/5 BISCOITOS! A BOLINHA FOI LIBERADA!";
            ateQuandoAviso = Time.time + 5f;
            Debug.Log("Sausage Run: todos os biscoitos encontrados! Bolinha liberada.");
        }
        else
        {
            Debug.Log("Sausage Run: " + Quantidade + "/" + Total + " biscoitos encontrados.");
        }
        return true;
    }

    private void DefinirTrava(bool bloquear)
    {
        if (portaoDaBolinha != null) portaoDaBolinha.SetActive(bloquear);
        if (bolinha != null) bolinha.enabled = !bloquear;
    }

    private void ReiniciarColecao()
    {
        coletados.Clear();
        if (bolinha != null && bolinha.Carregada)
            bolinha.RestaurarBolinha();
        DefinirTrava(true);
        foreach (BiscoitoColetavel b in biscoitos)
            if (b != null) b.Restaurar();
        aviso = "Maria pegou Scooby! Os 5 biscoitos voltaram aos esconderijos.";
        ateQuandoAviso = Time.time + 4f;
    }

    private void OnGUI()
    {
        if (!mostrarHUD || (jogo != null && jogo.Venceu)) return;
        GUIStyle estilo = new GUIStyle(GUI.skin.box);
        estilo.fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.021f, 18f, 34f));
        estilo.alignment = TextAnchor.MiddleCenter;
        estilo.wordWrap = true;
        float largura = Mathf.Min(600f, Screen.width - 16f);
        Rect painel = new Rect((Screen.width - largura) * 0.5f, 12f, largura, 94f);
        string objetivo = BolinhaLiberada
            ? "BOLINHA LIBERADA!\nPegue a bolinha e fuja pela janela."
            : "BISCOITOS: " + Quantidade + " / " + Total +
              "\nEncontre todos para liberar a bolinha.";
        GUI.Box(painel, objetivo, estilo);

        if (Time.time < ateQuandoAviso && !string.IsNullOrEmpty(aviso))
        {
            float w = Mathf.Min(Screen.width - 24f, 540f);
            GUI.Box(new Rect((Screen.width - w) * 0.5f, 112f, w, 64f), aviso, estilo);
        }
    }
}
