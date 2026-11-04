using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Escalada ampliada para o prototipo do Sausage Run 3D.
/// Mantem compatibilidade com as etapas anteriores e adiciona suporte
/// a varios objetos escalaveis extras, ideais para percursos complexos.
/// Use E perto de uma caixa, banco, pilha ou peitoril para subir.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ScoobyEscalada : MonoBehaviour
{
    [Header("Objetos escalaveis originais")]
    [SerializeField] private Collider caixaExterna;
    [SerializeField] private Collider caixaInterna;
    [SerializeField] private Collider peitorilJanela;

    [Header("Objetos escalaveis extras")]
    [SerializeField] private Collider[] objetosEscalaveisExtras = new Collider[0];

    [Header("Ajustes")]
    [Min(0.1f)] [SerializeField] private float distanciaMaxima = 1.05f;
    [Min(0.1f)] [SerializeField] private float alturaMaxima = 1.15f;
    [Min(0.01f)] [SerializeField] private float alturaMinima = 0.12f;
    [Min(0.01f)] [SerializeField] private float alturaMinimaParaJanela = 0.30f;
    [Min(0.05f)] [SerializeField] private float duracaoEscalada = 0.38f;
    [Min(0f)] [SerializeField] private float arcoEscalada = 0.28f;
    [Min(0.01f)] [SerializeField] private float folgaSobrePlataforma = 0.04f;

    private CharacterController controlador;
    private ScoobyController movimento;
    private bool escalando;

    public Collider[] ObjetosEscalaveisExtras
    {
        get => objetosEscalaveisExtras;
        set => objetosEscalaveisExtras = value;
    }

    private void Awake()
    {
        controlador = GetComponent<CharacterController>();
        movimento = GetComponent<ScoobyController>();
    }

    private void Update()
    {
        if (escalando || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (movimento == null)
        {
            Debug.LogError("ScoobyEscalada: adicione ScoobyController ao mesmo objeto.", this);
            return;
        }

        Collider destino = EscolherSuperficie();
        if (destino == null)
        {
            Debug.Log("Scooby: aproxime-se de um objeto escalavel e pressione E.");
            return;
        }

        Vector3 chegada = PontoDeChegada(destino);
        if (!DestinoLivre(chegada, destino))
        {
            Debug.Log("Scooby: nao ha espaco para ficar em cima deste objeto.");
            return;
        }

        StartCoroutine(Escalar(chegada));
    }

    private Collider EscolherSuperficie()
    {
        Collider melhor = null;
        float menorDistancia = float.MaxValue;

        List<Collider> opcoes = new List<Collider>(3 + (objetosEscalaveisExtras != null ? objetosEscalaveisExtras.Length : 0));
        if (caixaExterna != null) opcoes.Add(caixaExterna);
        if (caixaInterna != null) opcoes.Add(caixaInterna);
        if (peitorilJanela != null) opcoes.Add(peitorilJanela);
        if (objetosEscalaveisExtras != null)
        {
            foreach (Collider c in objetosEscalaveisExtras)
                if (c != null) opcoes.Add(c);
        }

        HashSet<Collider> visitados = new HashSet<Collider>();
        foreach (Collider opcao in opcoes)
        {
            if (opcao == null || visitados.Contains(opcao))
                continue;
            visitados.Add(opcao);

            if (!opcao.enabled || !opcao.gameObject.activeInHierarchy)
                continue;

            if (opcao == peitorilJanela && transform.position.y < alturaMinimaParaJanela)
                continue;

            Bounds b = opcao.bounds;
            float subida = b.max.y + folgaSobrePlataforma - transform.position.y;
            if (subida < alturaMinima || subida > alturaMaxima)
                continue;

            Vector3 pontoNaBorda = b.ClosestPoint(new Vector3(
                transform.position.x, b.center.y, transform.position.z));
            Vector2 a = new Vector2(transform.position.x, transform.position.z);
            Vector2 c = new Vector2(pontoNaBorda.x, pontoNaBorda.z);
            float distancia = Vector2.Distance(a, c);

            if (distancia <= distanciaMaxima && distancia < menorDistancia)
            {
                melhor = opcao;
                menorDistancia = distancia;
            }
        }
        return melhor;
    }

    private Vector3 PontoDeChegada(Collider superficie)
    {
        Bounds b = superficie.bounds;
        return new Vector3(b.center.x, b.max.y + folgaSobrePlataforma, b.center.z);
    }

    private bool DestinoLivre(Vector3 chegada, Collider superficie)
    {
        float raio = Mathf.Max(0.05f, controlador.radius - 0.015f);
        Vector3 centro = chegada + controlador.center;
        float metadeReta = Mathf.Max(0f, controlador.height * 0.5f - raio);
        Vector3 baixo = centro - Vector3.up * metadeReta;
        Vector3 alto = centro + Vector3.up * metadeReta;

        Collider[] encontrados = Physics.OverlapCapsule(
            baixo, alto, raio, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider outro in encontrados)
        {
            if (outro == superficie || outro == controlador || outro.transform.IsChildOf(transform))
                continue;
            return false;
        }
        return true;
    }

    private IEnumerator Escalar(Vector3 chegada)
    {
        escalando = true;
        Vector3 partida = transform.position;
        movimento.enabled = false;
        controlador.enabled = false;

        float decorrido = 0f;
        while (decorrido < duracaoEscalada)
        {
            decorrido += Time.deltaTime;
            float t = Mathf.Clamp01(decorrido / duracaoEscalada);
            float suave = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(partida, chegada, suave);
            pos.y += Mathf.Sin(t * Mathf.PI) * arcoEscalada;
            transform.position = pos;
            yield return null;
        }

        transform.position = chegada;
        controlador.enabled = true;
        movimento.enabled = true;
        escalando = false;
    }
}
