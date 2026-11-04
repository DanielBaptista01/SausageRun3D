using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Configura o objetivo final sem alterar os scripts anteriores nem recriar
/// a sala. Menu: Sausage Run > 4 - Configurar bolinha e vitoria.
/// </summary>
public static class SausageRunEtapa5Setup
{
    [MenuItem("Sausage Run/4 - Configurar bolinha e vitoria")]
    public static void Configurar()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sausage Run", "Pare o Play antes de configurar.", "OK");
            return;
        }

        GameObject scooby = GameObject.Find("Scooby");
        GameObject bolinhaGo = GameObject.Find("Bolinha");
        MariaAI maria = Object.FindFirstObjectByType<MariaAI>();

        if (scooby == null || bolinhaGo == null || maria == null)
        {
            EditorUtility.DisplayDialog("Sausage Run",
                "Nao encontrei Scooby, Bolinha ou MariaAI.\n" +
                "Confira a cena Sala e a Hierarchy antes de continuar.", "OK");
            return;
        }

        ScoobyController controle = scooby.GetComponent<ScoobyController>();
        ScoobyEscalada escalada = scooby.GetComponent<ScoobyEscalada>();
        if (controle == null || escalada == null)
        {
            EditorUtility.DisplayDialog("Sausage Run",
                "Scooby precisa ter ScoobyController e ScoobyEscalada.", "OK");
            return;
        }

        GameObject gerenciadorGo = GameObject.Find("SausageRun_Jogo");
        if (gerenciadorGo == null)
        {
            gerenciadorGo = new GameObject("SausageRun_Jogo");
            Undo.RegisterCreatedObjectUndo(gerenciadorGo, "Criar gerenciador");
        }

        SausageRunGameManager jogo = gerenciadorGo.GetComponent<SausageRunGameManager>();
        if (jogo == null) jogo = Undo.AddComponent<SausageRunGameManager>(gerenciadorGo);

        BolinhaColetavel bolinha = bolinhaGo.GetComponent<BolinhaColetavel>();
        if (bolinha == null) bolinha = Undo.AddComponent<BolinhaColetavel>(bolinhaGo);

        Camera cameraScooby = scooby.GetComponentInChildren<Camera>();
        bolinha.scooby = scooby.transform;
        bolinha.cameraScooby = cameraScooby;
        bolinha.jogo = jogo;

        // Raycasts da IA ignoram triggers. A coleta nao precisa bloquear Scooby.
        Collider c = bolinhaGo.GetComponent<Collider>();
        if (c != null) c.isTrigger = true;

        jogo.scooby = scooby.transform;
        jogo.movimentoScooby = controle;
        jogo.escaladaScooby = escalada;
        jogo.maria = maria;
        jogo.bolinha = bolinha;

        EditorUtility.SetDirty(jogo);
        EditorUtility.SetDirty(bolinha);
        if (c != null) EditorUtility.SetDirty(c);

        Scene cena = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(cena.path))
        {
            EditorUtility.DisplayDialog("Sausage Run",
                "Salve a cena como Sala.unity antes de testar a vitoria.", "OK");
            return;
        }

        // Permite que 'Jogar novamente (R)' recarregue a cena, inclusive
        // quando o jogo for exportado. Mantem todas as outras cenas existentes.
        EditorBuildSettingsScene[] atuais = EditorBuildSettings.scenes;
        if (!atuais.Any(s => s.path == cena.path))
        {
            EditorBuildSettings.scenes = atuais.Concat(new[] {
                new EditorBuildSettingsScene(cena.path, true)
            }).ToArray();
        }

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        Selection.activeGameObject = gerenciadorGo;
        EditorGUIUtility.PingObject(gerenciadorGo);
        Debug.Log("Sausage Run: bolinha e vitoria configuradas. Pressione Play para testar.");
        EditorUtility.DisplayDialog("Sausage Run — Etapa 5",
            "Configuracao concluida!\n\n" +
            "Aproxime-se da bolinha e pressione E.\n" +
            "Retorne ao corredor pela janela para vencer.\n" +
            "Se Maria capturar Scooby, pressione R para tentar novamente.", "OK");
    }
}
