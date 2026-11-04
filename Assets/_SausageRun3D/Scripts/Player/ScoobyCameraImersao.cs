using UnityEngine;

/// <summary>
/// Movimento sutil de cabeca para reforcar o ponto de vista de um cachorro.
/// Adicione na CameraScooby. NAO altera rotacao da camera: o ScoobyController
/// continua controlando o mouse normalmente.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class ScoobyCameraImersao : MonoBehaviour
{
    [Header("Movimento da cabeca")]
    [SerializeField, Range(0f, 0.05f)] private float amplitudeAndando = 0.012f;
    [SerializeField, Range(0f, 0.06f)] private float amplitudeCorrendo = 0.025f;
    [SerializeField, Min(0.1f)] private float frequenciaAndando = 9f;
    [SerializeField, Min(0.1f)] private float frequenciaCorrendo = 13f;
    [SerializeField, Min(0.1f)] private float suavizacao = 11f;

    [Header("Respiracao parado")]
    [SerializeField, Range(0f, 0.02f)] private float amplitudeRespiracao = 0.002f;
    [SerializeField, Min(0.1f)] private float frequenciaRespiracao = 1.8f;

    private CharacterController controlador;
    private ScoobyController movimento;
    private Vector3 posicaoLocalBase;
    private float fasePassos;

    private void Awake()
    {
        controlador = GetComponentInParent<CharacterController>();
        movimento = GetComponentInParent<ScoobyController>();
        posicaoLocalBase = transform.localPosition;
    }

    private void LateUpdate()
    {
        // Quando Scooby e capturado ou esta escalando, nao balancamos a cabeca.
        bool podeAndar = movimento != null && movimento.isActiveAndEnabled &&
                         controlador != null && controlador.enabled && controlador.isGrounded;
        Vector3 velocidade = controlador != null ? controlador.velocity : Vector3.zero;
        float velocidadeHorizontal = new Vector2(velocidade.x, velocidade.z).magnitude;
        bool caminhando = podeAndar && velocidadeHorizontal > 0.18f;
        bool correndo = caminhando && velocidadeHorizontal > 3.15f;

        float amplitude = correndo ? amplitudeCorrendo : amplitudeAndando;
        float frequencia = correndo ? frequenciaCorrendo : frequenciaAndando;
        Vector3 deslocamento = Vector3.zero;

        if (caminhando)
        {
            fasePassos += Time.deltaTime * frequencia;
            deslocamento.x = Mathf.Cos(fasePassos * 0.5f) * amplitude * 0.45f;
            deslocamento.y = Mathf.Sin(fasePassos) * amplitude;
        }
        else
        {
            deslocamento.y = Mathf.Sin(Time.time * frequenciaRespiracao) * amplitudeRespiracao;
        }

        Vector3 alvo = posicaoLocalBase + deslocamento;
        float interpolacao = 1f - Mathf.Exp(-suavizacao * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, alvo, interpolacao);
    }
}
