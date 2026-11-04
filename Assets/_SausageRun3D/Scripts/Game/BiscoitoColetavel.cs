using UnityEngine;

/// <summary>
/// Biscoito flutuante e giratorio, coletado automaticamente por proximidade.
/// A coleta depende de visada livre (nao atravessa estantes ou tampos).
/// </summary>
public class BiscoitoColetavel : MonoBehaviour
{
    [Header("Identificacao")]
    public string nomeDesafio;
    public DesafioBiscoitos desafio;
    public Transform scooby;

    [Header("Movimento visual")]
    [Min(0f)] public float amplitudeFlutuacao = 0.10f;
    [Min(0.1f)] public float velocidadeFlutuacao = 2.2f;
    public float rotacaoPorSegundo = 85f;

    [Header("Coleta")]
    [Min(0.15f)] public float distanciaDeColeta = 0.58f;

    private Vector3 posicaoOriginalLocal;
    private Quaternion rotacaoOriginalLocal;
    private bool coletado;
    private float fase;

    private void Awake()
    {
        posicaoOriginalLocal = transform.localPosition;
        rotacaoOriginalLocal = transform.localRotation;
        fase = transform.GetSiblingIndex() * 0.7f;
    }

    private void Update()
    {
        transform.localPosition = posicaoOriginalLocal +
            Vector3.up * (Mathf.Sin(Time.time * velocidadeFlutuacao + fase) * amplitudeFlutuacao);
        transform.Rotate(0f, rotacaoPorSegundo * Time.deltaTime, 0f, Space.World);

        if (coletado || desafio == null || scooby == null || desafio.JogoEncerrado)
            return;

        Vector3 focinho = scooby.position + Vector3.up * 0.38f;
        Vector3 alvo = transform.position;
        if (Vector3.Distance(focinho, alvo) > distanciaDeColeta)
            return;

        // Evita pegar biscoitos escondidos atras de movel ou parede.
        if (Physics.Linecast(focinho, alvo, out RaycastHit acerto,
            ~0, QueryTriggerInteraction.Ignore))
        {
            if (acerto.transform != scooby && !acerto.transform.IsChildOf(scooby))
                return;
        }

        if (desafio.RegistrarColeta(this))
        {
            coletado = true;
            gameObject.SetActive(false);
        }
    }

    public void Restaurar()
    {
        coletado = false;
        gameObject.SetActive(true);
        transform.localPosition = posicaoOriginalLocal;
        transform.localRotation = rotacaoOriginalLocal;
    }
}
