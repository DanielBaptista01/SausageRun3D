using UnityEngine;

/// <summary>
/// Sausage Run 3D - Etapa 6A
/// Efeitos visuais de caminhada para a camera em PRIMEIRA pessoa.
/// Instale no CameraFX_Pivot (objeto PAI da CameraScooby).
/// Nao altera ScoobyController, escalada, IA ou GameManager.
/// </summary>
[DisallowMultipleComponent]
public class ScoobyCameraEffects : MonoBehaviour
{
    [Header("Referencias (preenchidas automaticamente)")]
    [SerializeField] private Camera cameraScooby;
    [SerializeField] private CharacterController controlador;
    [SerializeField] private ScoobyController movimentoScooby;

    [Header("Balanco de caminhada (metros)")]
    [SerializeField, Range(0f, 0.07f)] private float bobAndando = 0.025f;
    [SerializeField, Range(0f, 0.09f)] private float bobCorrendo = 0.045f;
    [SerializeField, Range(0f, 1f)] private float proporcaoBalancoLateral = 0.40f;
    [SerializeField, Range(0.5f, 5f)] private float passosPorSegundoAndando = 1.9f;
    [SerializeField, Range(0.5f, 6f)] private float passosPorSegundoCorrendo = 2.8f;
    [SerializeField, Min(1f)] private float suavizacaoPosicao = 12f;

    [Header("Inclinacao natural nas curvas")]
    [SerializeField, Range(0f, 4f)] private float inclinacaoMaximaGraus = 1.5f;
    [SerializeField, Range(0f, 0.05f)] private float respostaCurvas = 0.016f;
    [SerializeField, Range(0f, 1.5f)] private float inclinacaoDeslocamentoLateral = 0.45f;
    [SerializeField, Min(1f)] private float suavizacaoInclinacao = 9f;

    [Header("Campo de visao (FOV)")]
    [SerializeField, Range(50f, 100f)] private float fovParado = 75f;
    [SerializeField, Range(50f, 110f)] private float fovCorrendo = 81f;
    [SerializeField, Min(1f)] private float suavizacaoFov = 6f;

    [Header("Pulo e aterrissagem")]
    [SerializeField, Range(0f, 0.09f)] private float deslocamentoAoPousar = 0.04f;
    [SerializeField, Min(1f)] private float recuperacaoAoPousar = 9f;

    [Header("Respiracao quando parado")]
    [SerializeField, Range(0f, 0.015f)] private float amplitudeRespiracao = 0.002f;
    [SerializeField, Range(0.1f, 3f)] private float frequenciaRespiracao = 0.35f;

    private Vector3 posicaoLocalInicial;
    private Quaternion rotacaoLocalInicial;
    private float fasePassos;
    private float intensidadeMovimento;
    private float inclinacaoAtual;
    private float deslocamentoPousoAtual;
    private float ultimoYaw;
    private bool estavaNoChao = true;
    private float tempoNoAr;
    private float maiorVelocidadeQueda;
    private bool inicializado;

    private void Awake()
    {
        LocalizarReferencias();
        posicaoLocalInicial = transform.localPosition;
        rotacaoLocalInicial = transform.localRotation;
        inicializado = true;
        ultimoYaw = movimentoScooby != null ? movimentoScooby.transform.eulerAngles.y : 0f;
    }

    private void OnEnable()
    {
        LocalizarReferencias();
        if (!inicializado)
        {
            posicaoLocalInicial = transform.localPosition;
            rotacaoLocalInicial = transform.localRotation;
            inicializado = true;
        }
        ultimoYaw = movimentoScooby != null ? movimentoScooby.transform.eulerAngles.y : 0f;
        estavaNoChao = true;
        tempoNoAr = 0f;
        maiorVelocidadeQueda = 0f;
    }

    private void LocalizarReferencias()
    {
        if (cameraScooby == null) cameraScooby = GetComponentInChildren<Camera>(true);
        if (controlador == null) controlador = GetComponentInParent<CharacterController>();
        if (movimentoScooby == null) movimentoScooby = GetComponentInParent<ScoobyController>();
    }

    private void LateUpdate()
    {
        if (controlador == null || cameraScooby == null || movimentoScooby == null)
            return;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        bool movimentoAtivo = movimentoScooby.isActiveAndEnabled &&
                              controlador.enabled && controlador.gameObject.activeInHierarchy;
        bool noChao = movimentoAtivo && controlador.isGrounded;

        Vector3 velocidade = movimentoAtivo ? controlador.velocity : Vector3.zero;
        float velocidadeHorizontal = new Vector2(velocidade.x, velocidade.z).magnitude;
        bool andando = noChao && velocidadeHorizontal > 0.18f;
        float proporcaoCorrida = Mathf.InverseLerp(2.7f, 3.8f, velocidadeHorizontal);

        // Corrige a intensidade gradualmente para nao "dar trancos" ao comecar/parar.
        float intensidadeAlvo = andando ? Mathf.Clamp01(velocidadeHorizontal / 1.1f) : 0f;
        intensidadeMovimento = Mathf.Lerp(
            intensidadeMovimento, intensidadeAlvo, Interpolacao(10f, dt));

        if (andando)
        {
            float cadencia = Mathf.Lerp(passosPorSegundoAndando, passosPorSegundoCorrendo,
                proporcaoCorrida);
            float escalaVelocidade = Mathf.Clamp(velocidadeHorizontal / 2.5f, 0.45f, 1.35f);
            fasePassos += dt * 2f * Mathf.PI * cadencia * escalaVelocidade;
        }

        float amplitude = Mathf.Lerp(bobAndando, bobCorrendo, proporcaoCorrida);
        float bobY = Mathf.Sin(fasePassos) * amplitude * intensidadeMovimento;
        float bobX = Mathf.Cos(fasePassos * 0.5f) * amplitude *
                     proporcaoBalancoLateral * intensidadeMovimento;

        // Detecta aterrissagem somente apos pulo/queda real.
        // Escalada desliga temporariamente o ScoobyController e o CharacterController,
        // entao nao produz um impacto falso ao terminar a animacao de escalada.
        if (!movimentoAtivo)
        {
            estavaNoChao = true;
            tempoNoAr = 0f;
            maiorVelocidadeQueda = 0f;
        }
        else if (!noChao)
        {
            tempoNoAr += dt;
            maiorVelocidadeQueda = Mathf.Min(maiorVelocidadeQueda, velocidade.y);
        }
        else
        {
            if (!estavaNoChao && tempoNoAr > 0.12f && maiorVelocidadeQueda < -2f)
            {
                float forca = Mathf.InverseLerp(2f, 6f, -maiorVelocidadeQueda);
                deslocamentoPousoAtual = -deslocamentoAoPousar * forca;
            }
            tempoNoAr = 0f;
            maiorVelocidadeQueda = 0f;
        }
        estavaNoChao = noChao;
        deslocamentoPousoAtual = Mathf.Lerp(
            deslocamentoPousoAtual, 0f, Interpolacao(recuperacaoAoPousar, dt));

        float respiracao = Mathf.Sin(Time.time * 2f * Mathf.PI * frequenciaRespiracao) *
                           amplitudeRespiracao * (1f - intensidadeMovimento);
        Vector3 posicaoAlvo = posicaoLocalInicial +
                              new Vector3(bobX, bobY + deslocamentoPousoAtual + respiracao, 0f);
        transform.localPosition = Vector3.Lerp(
            transform.localPosition, posicaoAlvo, Interpolacao(suavizacaoPosicao, dt));

        // O ScoobyController aplica o pitch do mouse na CameraScooby em Update.
        // Aqui inclinamos APENAS o pivot pai para nao sobrescrever o pitch.
        float yaw = movimentoScooby.transform.eulerAngles.y;
        float grausPorSegundo = Mathf.DeltaAngle(ultimoYaw, yaw) / dt;
        ultimoYaw = yaw;

        Vector3 velocidadeLocal = movimentoScooby.transform.InverseTransformDirection(velocidade);
        float deslocamentoLateral = Mathf.Clamp(velocidadeLocal.x / 2.5f, -1f, 1f);
        float inclinacaoAlvo = movimentoAtivo
            ? Mathf.Clamp(-grausPorSegundo * respostaCurvas -
                          deslocamentoLateral * inclinacaoDeslocamentoLateral,
                          -inclinacaoMaximaGraus, inclinacaoMaximaGraus)
            : 0f;
        inclinacaoAtual = Mathf.Lerp(
            inclinacaoAtual, inclinacaoAlvo, Interpolacao(suavizacaoInclinacao, dt));
        transform.localRotation = rotacaoLocalInicial * Quaternion.Euler(0f, 0f, inclinacaoAtual);

        float fovAlvo = Mathf.Lerp(fovParado, fovCorrendo,
            andando ? proporcaoCorrida : 0f);
        cameraScooby.fieldOfView = Mathf.Lerp(
            cameraScooby.fieldOfView, fovAlvo, Interpolacao(suavizacaoFov, dt));
    }

    private static float Interpolacao(float velocidade, float dt)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, velocidade) * dt);
    }

    private void OnDisable()
    {
        if (inicializado)
        {
            transform.localPosition = posicaoLocalInicial;
            transform.localRotation = rotacaoLocalInicial;
        }
        if (cameraScooby != null && Application.isPlaying)
            cameraScooby.fieldOfView = fovParado;
    }

    /// <summary>Usado pelo menu de configuracao para atualizar cenas ja instaladas.</summary>
    public void AplicarPresetNatural()
    {
        bobAndando = 0.025f;
        bobCorrendo = 0.045f;
        proporcaoBalancoLateral = 0.40f;
        passosPorSegundoAndando = 1.9f;
        passosPorSegundoCorrendo = 2.8f;
        suavizacaoPosicao = 12f;
        inclinacaoMaximaGraus = 1.5f;
        respostaCurvas = 0.016f;
        inclinacaoDeslocamentoLateral = 0.45f;
        suavizacaoInclinacao = 9f;
        fovParado = 75f;
        fovCorrendo = 81f;
        suavizacaoFov = 6f;
        deslocamentoAoPousar = 0.04f;
        recuperacaoAoPousar = 9f;
        amplitudeRespiracao = 0.002f;
        frequenciaRespiracao = 0.35f;
    }
}
