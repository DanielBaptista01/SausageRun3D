using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Etapa 6B revisada: amplia a casa, fecha o corredor com teto,
/// adiciona labirintos fechados, muitos objetos escalaveis e uma
/// escada de acesso a garagem SUBINDO.
/// Deve ser usada depois das Etapas 1 a 6A.
/// </summary>
public static class SausageRunExpansao6B_Labirintos
{
    private const string RootName = "SausageRun_Prototipo";
    private const string GroupName = "06B_LabirintosFechados";
    private const string OldGroupName = "06B_Expansao";
    private const string MaterialsPath = "Assets/_SausageRun3D/Materials";

    [MenuItem("Sausage Run/6B - Expandir casa com labirintos fechados")]
    public static void Expandir()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sausage Run 6B", "Pare o modo Play antes de alterar a cena.", "OK");
            return;
        }

        Scene cena = SceneManager.GetActiveScene();
        GameObject root = GameObject.Find(RootName);
        GameObject scooby = GameObject.Find("Scooby");
        GameObject maria = GameObject.Find("Maria");
        GameObject gerenciador = GameObject.Find("SausageRun_Jogo");
        GameObject nav = GameObject.Find("Sala_NavMesh");

        if (string.IsNullOrEmpty(cena.path) || root == null || scooby == null || maria == null ||
            gerenciador == null || nav == null || maria.GetComponent<MariaAI>() == null ||
            gerenciador.GetComponent<SausageRunGameManager>() == null ||
            nav.GetComponent<NavMeshSurface>() == null)
        {
            EditorUtility.DisplayDialog("Sausage Run 6B",
                "Abra a cena Sala, salve-a, e confira se as Etapas 1 a 5 foram aplicadas: " +
                "SausageRun_Prototipo, Scooby, Maria, SausageRun_Jogo e Sala_NavMesh.", "OK");
            return;
        }

        Transform pisos = root.transform.Find("01_Pisos");
        Transform paredes = root.transform.Find("02_Paredes");
        Transform portas = root.transform.Find("03_Portas_fechadas");
        Transform moveis = root.transform.Find("04_Moveis_e_obstaculos");
        Transform gameplay = root.transform.Find("05_Scooby_Maria_Bolinha");
        if (pisos == null || paredes == null || portas == null || moveis == null || gameplay == null ||
            !VerificarBase(pisos, paredes, portas, moveis))
        {
            EditorUtility.DisplayDialog("Sausage Run 6B",
                "Faltam objetos do cenario original. A expansao foi cancelada para proteger o projeto.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Sausage Run — Etapa 6B revisada",
            "Aplicar a nova versao da casa com:\n\n" +
            "• sala maior (18 x 14 m)\n" +
            "• corredor alto e coberto\n" +
            "• porta de grade com escada SUBINDO para a garagem\n" +
            "• banheirinho e acesso a lavanderia\n" +
            "• labirintos fechados\n" +
            "• varios objetos escalaveis\n" +
            "• teto cobrindo toda a area\n\n" +
            "Uma copia da cena sera salva antes da mudanca.", "Aplicar", "Cancelar"))
            return;

        string pastaBackup = "Assets/_SausageRun3D/Scenes";
        string caminhoBackup = AssetDatabase.GenerateUniqueAssetPath(
            pastaBackup + "/Sala_BACKUP_Antes6B_Labirintos.unity");
        if (!EditorSceneManager.SaveScene(cena, caminhoBackup, true))
        {
            EditorUtility.DisplayDialog("Sausage Run 6B",
                "Nao consegui salvar a copia de seguranca. Nenhuma alteracao foi aplicada.", "OK");
            return;
        }

        try
        {
            Transform velho = root.transform.Find(OldGroupName);
            if (velho != null) Undo.DestroyObjectImmediate(velho.gameObject);
            Transform atual = root.transform.Find(GroupName);
            if (atual != null) Undo.DestroyObjectImmediate(atual.gameObject);

            AmpliarEstrutura(pisos, paredes, portas);
            Transform grupoNovo = CriarNovaArquitetura(root.transform);
            ConfigurarEscaladaExtra(scooby, grupoNovo, moveis, paredes);
            ReposicionarBolinha(gameplay, grupoNovo);
            AtualizarJogo(gerenciador.GetComponent<SausageRunGameManager>(), maria.GetComponent<MariaAI>(), maria.transform);
            AtualizarNavMesh(nav.GetComponent<NavMeshSurface>(), pisos);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            bool bakeOK = false;
            try
            {
                nav.GetComponent<NavMeshSurface>().BuildNavMesh();
                EditorSceneManager.SaveScene(cena);
                bakeOK = true;
            }
            catch (Exception bakeErro)
            {
                Debug.LogWarning("Sausage Run 6B: gere a NavMesh manualmente se necessario. " + bakeErro.Message);
            }

            Selection.activeGameObject = grupoNovo.gameObject;
            EditorGUIUtility.PingObject(grupoNovo.gameObject);
            EditorUtility.DisplayDialog("Sausage Run 6B — Pronto!",
                "Labirintos fechados, teto e multiplos objetos escalaveis foram aplicados.\n\n" +
                "Teste especialmente:\n" +
                "• entrada e saida pela janela\n" +
                "• subida em caixas, bancos e pilhas\n" +
                "• bolinha sobre a plataforma elevada\n" +
                "• perseguicao da Maria entre os novos labirintos\n\n" +
                (bakeOK ? "NavMesh reconstruida automaticamente.\n" : "Selecione Sala_NavMesh e clique Bake.\n") +
                "Backup salvo em:\n" + caminhoBackup,
                "OK");
        }
        catch (Exception erro)
        {
            Debug.LogError("Sausage Run 6B revisada falhou. Use o backup: " + caminhoBackup + "\n" + erro);
            EditorUtility.DisplayDialog("Sausage Run 6B — Erro",
                "Houve uma falha durante a expansao.\n\nConfira a aba Console.\nBackup:\n" + caminhoBackup,
                "OK");
        }
    }

    private static bool VerificarBase(Transform pisos, Transform paredes, Transform portas, Transform moveis)
    {
        string[] reqPisos = { "Chao_Sala", "Chao_Corredor" };
        string[] reqParedes = {
            "Fundo", "Frente_Esquerda", "Frente_Entre_Porta_Janela", "Frente_Direita",
            "Frente_Acima_Porta", "Frente_Abaixo_Janela", "Frente_Acima_Janela",
            "Esquerda_Principal", "Esquerda_Fundo", "Esquerda_Acima_Porta",
            "Direita_Principal", "Direita_Fundo", "Direita_Acima_Porta",
            "Corredor_Borda_Fundo", "Corredor_Borda_Esquerda", "Corredor_Borda_Direita" };
        string[] reqPortas = { "Porta_Frontal_Fechada", "Porta_Esquerda_Fechada", "Porta_Direita_Fechada" };
        string[] reqMoveis = { "Caixa_Externa_Janela", "Caixa_Interna_Janela" };
        return Contem(pisos, reqPisos) && Contem(paredes, reqParedes) && Contem(portas, reqPortas) && Contem(moveis, reqMoveis);
    }

    private static bool Contem(Transform grupo, string[] nomes)
    {
        foreach (string nome in nomes)
            if (grupo.Find(nome) == null) return false;
        return true;
    }

    private static void AmpliarEstrutura(Transform pisos, Transform paredes, Transform portas)
    {
        Alterar(pisos, "Chao_Sala", new Vector3(0f, -0.1f, 2f), new Vector3(18f, 0.2f, 14f));
        Alterar(pisos, "Chao_Corredor", new Vector3(0f, -0.1f, -7f), new Vector3(18f, 0.2f, 4f));

        Alterar(paredes, "Fundo", new Vector3(0f, 1.65f, 9f), new Vector3(18f, 3.3f, 0.2f));
        Alterar(paredes, "Frente_Esquerda", new Vector3(-6.8f, 1.65f, -5f), new Vector3(4.4f, 3.3f, 0.2f));
        Alterar(paredes, "Frente_Entre_Porta_Janela", new Vector3(-0.75f, 1.65f, -5f), new Vector3(4.9f, 3.3f, 0.2f));
        Alterar(paredes, "Frente_Direita", new Vector3(6.25f, 1.65f, -5f), new Vector3(5.5f, 3.3f, 0.2f));
        Alterar(paredes, "Frente_Acima_Porta", new Vector3(-3.9f, 2.75f, -5f), new Vector3(1.4f, 1.1f, 0.2f));
        Alterar(paredes, "Frente_Acima_Janela", new Vector3(2.6f, 2.7f, -5f), new Vector3(1.8f, 1.2f, 0.2f));

        Alterar(paredes, "Esquerda_Principal", new Vector3(-9f, 1.65f, 1.05f), new Vector3(0.2f, 3.3f, 12.1f));
        Alterar(paredes, "Esquerda_Fundo", new Vector3(-9f, 1.65f, 8.65f), new Vector3(0.2f, 3.3f, 0.7f));
        Alterar(paredes, "Esquerda_Acima_Porta", new Vector3(-9f, 2.75f, 7.7f), new Vector3(0.2f, 1.1f, 1.2f));
        Alterar(paredes, "Direita_Principal", new Vector3(9f, 1.65f, 1.05f), new Vector3(0.2f, 3.3f, 12.1f));
        Alterar(paredes, "Direita_Fundo", new Vector3(9f, 1.65f, 8.65f), new Vector3(0.2f, 3.3f, 0.7f));
        Alterar(paredes, "Direita_Acima_Porta", new Vector3(9f, 2.75f, 7.7f), new Vector3(0.2f, 1.1f, 1.2f));
        Alterar(portas, "Porta_Esquerda_Fechada", new Vector3(-9f, 1.1f, 7.7f), new Vector3(0.1f, 2.2f, 1.18f));
        Alterar(portas, "Porta_Direita_Fechada", new Vector3(9f, 1.1f, 7.7f), new Vector3(0.1f, 2.2f, 1.18f));

        Desativar(paredes, "Corredor_Borda_Fundo");
        Desativar(paredes, "Corredor_Borda_Esquerda");
        Desativar(paredes, "Corredor_Borda_Direita");
    }

    private static Transform CriarNovaArquitetura(Transform root)
    {
        Transform g = NovoGrupo(GroupName, root);
        Transform sala = NovoGrupo("01_Sala_Labirintos", g);
        Transform corredor = NovoGrupo("02_Corredor_Externo", g);
        Transform servicos = NovoGrupo("03_Servicos_Externos", g);
        Transform teto = NovoGrupo("04_Tetos", g);
        Transform detalhes = NovoGrupo("05_Detalhes_Visuais", g);

        Material paredeBranca = Mat("6B2_ParedeBranca", new Color(0.86f, 0.86f, 0.84f));
        Material muro = Mat("6B2_MuroExterno", new Color(0.60f, 0.68f, 0.70f));
        Material cimento = Mat("6B2_Cimento", new Color(0.50f, 0.56f, 0.60f));
        Material madeira = Mat("6B2_Madeira", new Color(0.53f, 0.35f, 0.20f));
        Material madeiraEscura = Mat("6B2_MadeiraEscura", new Color(0.27f, 0.20f, 0.15f));
        Material tecidoVerde = Mat("6B2_TecidoVerde", new Color(0.22f, 0.45f, 0.36f));
        Material tecidoAzul = Mat("6B2_TecidoAzul", new Color(0.25f, 0.37f, 0.50f));
        Material tecidoVermelho = Mat("6B2_TecidoVermelho", new Color(0.55f, 0.26f, 0.22f));
        Material creme = Mat("6B2_Creme", new Color(0.82f, 0.79f, 0.70f));
        Material tapete = Mat("6B2_Tapete", new Color(0.40f, 0.24f, 0.30f));
        Material preto = Mat("6B2_Preto", new Color(0.12f, 0.13f, 0.15f));
        Material metal = Mat("6B2_Metal", new Color(0.56f, 0.61f, 0.64f));
        Material amareloCaixa = Mat("6B2_Caixa", new Color(0.73f, 0.53f, 0.26f));
        Material verdePlanta = Mat("6B2_Planta", new Color(0.22f, 0.43f, 0.27f));

        // Corredor alto e fechado.
        Cubo("Muro_Corredor_Fundo", corredor, 0f, 2.2f, -9f, 18f, 4.4f, 0.22f, muro);
        Cubo("Muro_Corredor_Esquerda_A", corredor, -9f, 2.2f, -8.45f, 0.22f, 4.4f, 1.1f, muro);
        Cubo("Muro_Corredor_Esquerda_B", corredor, -9f, 2.2f, -5.55f, 0.22f, 4.4f, 1.1f, muro);
        Cubo("Muro_Corredor_Esquerda_Cima_Grade", corredor, -9f, 3.45f, -7f, 0.22f, 1.9f, 1.8f, muro);
        Cubo("Muro_Corredor_Direita_A", corredor, 9f, 2.2f, -8.7f, 0.22f, 4.4f, 0.6f, muro);
        Cubo("Muro_Corredor_Direita_B", corredor, 9f, 2.2f, -7.15f, 0.22f, 4.4f, 0.3f, muro);
        Cubo("Muro_Corredor_Direita_C", corredor, 9f, 2.2f, -5.15f, 0.22f, 4.4f, 0.3f, muro);
        Cubo("Muro_Corredor_Acima_Banheiro", corredor, 9f, 3.35f, -7.9f, 0.22f, 2.1f, 1.0f, muro);
        Cubo("Muro_Corredor_Acima_Lavanderia", corredor, 9f, 3.5f, -6.1f, 0.22f, 1.8f, 1.6f, muro);

        // Garagem: grade + escada SUBINDO.
        Cubo("Grade_Lateral_A", servicos, -9f, 1.25f, -7.87f, 0.20f, 2.5f, 0.08f, preto);
        Cubo("Grade_Lateral_B", servicos, -9f, 1.25f, -6.13f, 0.20f, 2.5f, 0.08f, preto);
        Cubo("Grade_Trilho_Cima", servicos, -9f, 2.43f, -7f, 0.20f, 0.14f, 1.8f, preto);
        Cubo("Grade_Trilho_Baixo", servicos, -9f, 0.14f, -7f, 0.20f, 0.12f, 1.8f, preto);
        for (int i = 0; i <= 7; i++)
            Cubo("Grade_Barra_" + i, servicos, -9f, 1.25f, -7.75f + i * (1.5f / 7f), 0.085f, 2.4f, 0.07f, preto);
        Cubo("Grade_Bloqueio", servicos, -9f, 1.25f, -7f, 0.18f, 2.5f, 1.8f, preto, false);
        for (int i = 0; i < 7; i++)
        {
            float altura = 0.09f + i * 0.18f;
            float x = -9.40f - i * 0.58f;
            Cubo("Degrau_Garagem_Sobe_" + (i + 1), servicos, x, altura, -7f, 0.58f, 0.18f, 1.9f, cimento);
        }
        Cubo("Patamar_Garagem_Alto", servicos, -13.3f, 1.36f, -7f, 1.9f, 0.20f, 2.3f, cimento);
        Cubo("Corrimao_Garagem_A", servicos, -11.15f, 1.05f, -8.0f, 4.8f, 0.12f, 0.10f, metal);
        Cubo("Corrimao_Garagem_B", servicos, -11.15f, 1.05f, -6.0f, 4.8f, 0.12f, 0.10f, metal);

        // Banheirinho e lavanderia.
        Cubo("Piso_Banheirinho", servicos, 10.4f, -0.1f, -8f, 2.8f, 0.2f, 2f, creme);
        Cubo("Banheiro_Parede_Fora", servicos, 11.8f, 1.6f, -8f, 0.2f, 3.2f, 2f, muro);
        Cubo("Banheiro_Parede_Fundo", servicos, 10.4f, 1.6f, -9f, 2.8f, 3.2f, 0.2f, muro);
        Cubo("Banheiro_Parede_Interna", servicos, 10.4f, 1.6f, -7f, 2.8f, 3.2f, 0.2f, muro);
        Cubo("Porta_Banheirinho_Fechada", servicos, 9f, 1.13f, -7.9f, 0.16f, 2.26f, 0.98f, madeiraEscura);
        Cubo("Vaso_Sanitario", servicos, 10.8f, 0.30f, -8.3f, 0.55f, 0.60f, 0.70f, creme);
        Cubo("Caixa_Acoplada", servicos, 11.12f, 0.55f, -8.3f, 0.22f, 0.67f, 0.60f, creme);
        Cubo("Pia_Banheiro", servicos, 10.15f, 0.62f, -8.65f, 0.65f, 0.18f, 0.40f, creme);

        Cubo("Piso_Lavanderia", servicos, 11f, -0.1f, -6f, 4f, 0.2f, 2f, cimento);
        Cubo("Lav_Parede_Fora", servicos, 13f, 1.8f, -6f, 0.2f, 3.6f, 2f, muro);
        Cubo("Lav_Parede_Tras", servicos, 11f, 1.8f, -5f, 4f, 3.6f, 0.2f, muro);
        Cubo("Lav_Parede_Interna", servicos, 11f, 1.8f, -7f, 4f, 3.6f, 0.2f, muro);
        Cubo("Portao_Lav_Cima", servicos, 9f, 2.36f, -6.1f, 0.18f, 0.12f, 1.6f, preto);
        Cubo("Portao_Lav_Baixo", servicos, 9f, 0.10f, -6.1f, 0.18f, 0.12f, 1.6f, preto);
        for (int i = 0; i <= 6; i++)
            Cubo("Portao_Lav_Grade_" + i, servicos, 9f, 1.2f, -6.83f + i * 0.24f, 0.07f, 2.35f, 0.07f, preto);
        Cubo("Portao_Lav_Bloqueio", servicos, 9f, 1.2f, -6.1f, 0.17f, 2.4f, 1.6f, preto, false);
        Cubo("Tanque_Lavanderia", servicos, 11.2f, 0.7f, -5.5f, 1.1f, 0.3f, 0.65f, creme);
        Cubo("Lavadora_Base", servicos, 12.25f, 0.48f, -6.5f, 0.85f, 0.95f, 0.85f, creme);
        Cubo("Lavadora_Painel", servicos, 12.25f, 0.98f, -6.23f, 0.85f, 0.12f, 0.28f, metal);

        // --- Sala: mover para layout mais labirintico e fechado ---
        // Zona de sofa e TV.
        Cubo("SofaL_Assento_A", sala, -7.0f, 0.35f, 5.8f, 2.4f, 0.7f, 1.1f, tecidoVerde);
        Cubo("SofaL_Encosto_A", sala, -7.0f, 0.87f, 6.32f, 2.4f, 1.04f, 0.23f, tecidoVerde);
        Cubo("SofaL_Assento_B", sala, -7.8f, 0.35f, 6.8f, 0.85f, 0.7f, 2.1f, tecidoVerde);
        Cubo("SofaL_Encosto_B", sala, -8.28f, 0.84f, 6.82f, 0.22f, 1.0f, 2.1f, tecidoVerde);
        Cubo("Tapete_Sofa", sala, -6.4f, 0.012f, 5.15f, 3.6f, 0.024f, 2.5f, tapete, true, false);
        Cubo("RackTV", sala, 0.4f, 0.40f, 8.25f, 4.2f, 0.8f, 0.6f, madeiraEscura);
        Cubo("TV", sala, 0.4f, 1.45f, 8.57f, 2.8f, 1.38f, 0.10f, preto);

        // Labirinto fechado 1: caixas e paredes baixas no lado esquerdo da sala.
        CriarLabirintoEsquerdo(sala, amareloCaixa, madeira);
        // Labirinto fechado 2: estantes e bancos no centro/fundo.
        CriarLabirintoCentral(sala, madeiraEscura, madeira, tecidoAzul);
        // Labirinto fechado 3: rota de escalada proximo a janela e lado direito.
        CriarLabirintoDireito(sala, amareloCaixa, tecidoVermelho, madeiraEscura, madeira);

        // Detalhes e cobertura baixa.
        Cubo("Planta_Vaso_1", detalhes, -8.0f, 0.35f, -3.9f, 0.48f, 0.7f, 0.48f, creme, true, false);
        Cubo("Planta_1", detalhes, -8.0f, 1.07f, -3.9f, 0.70f, 0.85f, 0.70f, verdePlanta, true, false);
        Cubo("Planta_Vaso_2", detalhes, 8.0f, 0.35f, 7.55f, 0.48f, 0.7f, 0.48f, creme, true, false);
        Cubo("Planta_2", detalhes, 8.0f, 1.07f, 7.55f, 0.70f, 0.85f, 0.70f, verdePlanta, true, false);
        Cubo("Sapateira_Corredor", detalhes, -5.8f, 0.40f, -5.72f, 1.4f, 0.8f, 0.45f, madeiraEscura);
        Cubo("Lixeira_Externa", detalhes, -6.6f, 0.42f, -8.25f, 0.58f, 0.84f, 0.58f, metal);
        Cubo("Balde_Externo", detalhes, 7.5f, 0.26f, -8.2f, 0.55f, 0.52f, 0.55f, tecidoAzul);

        // Tetos: visuais, sem collider.
        Cubo("Teto_Sala", teto, 0f, 3.34f, 2f, 18.2f, 0.14f, 14.2f, paredeBranca, true, false);
        Cubo("Teto_Corredor", teto, 0f, 4.42f, -7f, 18.2f, 0.14f, 4.1f, paredeBranca, true, false);
        Cubo("Teto_Garagem", teto, -11.2f, 2.70f, -7f, 4.6f, 0.14f, 2.8f, paredeBranca, true, false);
        Cubo("Teto_Banheirinho", teto, 10.4f, 3.22f, -8f, 2.9f, 0.14f, 2.1f, paredeBranca, true, false);
        Cubo("Teto_Lavanderia", teto, 11f, 3.62f, -6f, 4.1f, 0.14f, 2.1f, paredeBranca, true, false);

        return g;
    }

    private static void CriarLabirintoEsquerdo(Transform parent, Material caixa, Material madeira)
    {
        Transform g = NovoGrupo("Labirinto_Esquerdo", parent);
        Cubo("LE_Oeste", g, -8.0f, 0.55f, -1.1f, 0.35f, 1.1f, 5.5f, caixa);
        Cubo("LE_Norte", g, -5.9f, 0.55f, 1.5f, 4.2f, 1.1f, 0.35f, caixa);
        Cubo("LE_Leste", g, -3.8f, 0.55f, -0.25f, 0.35f, 1.1f, 3.9f, caixa);
        Cubo("LE_Sul_A", g, -7.2f, 0.55f, -3.85f, 1.8f, 1.1f, 0.35f, caixa);
        Cubo("LE_Sul_B", g, -4.6f, 0.55f, -3.85f, 1.3f, 1.1f, 0.35f, caixa);
        Cubo("LE_Divisoria_1", g, -6.15f, 0.46f, -1.4f, 0.30f, 0.92f, 3.1f, caixa);
        Cubo("LE_Divisoria_2", g, -5.05f, 0.46f, 0.25f, 0.30f, 0.92f, 2.0f, caixa);
        Cubo("LE_Pilha_1", g, -7.1f, 0.24f, -2.4f, 0.9f, 0.48f, 0.9f, caixa);
        Cubo("LE_Pilha_2", g, -6.2f, 0.38f, -2.4f, 0.9f, 0.76f, 0.9f, caixa);
        Cubo("LE_Banco", g, -4.65f, 0.36f, -1.35f, 1.5f, 0.72f, 0.8f, madeira);
    }

    private static void CriarLabirintoCentral(Transform parent, Material escura, Material madeira, Material tecido)
    {
        Transform g = NovoGrupo("Labirinto_Central", parent);
        Cubo("LC_Oeste", g, -2.1f, 0.65f, 5.0f, 0.42f, 1.3f, 5.2f, escura);
        Cubo("LC_Norte", g, 0.7f, 0.65f, 7.6f, 5.6f, 1.3f, 0.42f, escura);
        Cubo("LC_Leste_A", g, 3.5f, 0.65f, 6.2f, 0.42f, 1.3f, 2.6f, escura);
        Cubo("LC_Leste_B", g, 3.5f, 0.65f, 3.4f, 0.42f, 1.3f, 1.2f, escura);
        Cubo("LC_Sul_A", g, -0.8f, 0.65f, 2.4f, 2.2f, 1.3f, 0.42f, escura);
        Cubo("LC_Sul_B", g, 2.7f, 0.65f, 2.4f, 1.0f, 1.3f, 0.42f, escura);
        Cubo("LC_Divisoria_Centro", g, 0.7f, 0.65f, 5.05f, 0.38f, 1.3f, 3.6f, escura);
        Cubo("LC_Divisoria_Curta", g, 1.95f, 0.65f, 4.0f, 2.1f, 1.3f, 0.38f, escura);

        Cubo("LC_Banco_A_Topo", g, -0.55f, 0.36f, 6.35f, 1.6f, 0.72f, 0.85f, madeira);
        Cubo("LC_Banco_B_Topo", g, 2.45f, 0.36f, 6.1f, 1.5f, 0.72f, 0.80f, madeira);
        Cubo("LC_Banco_C_Topo", g, -0.45f, 0.36f, 3.35f, 1.35f, 0.72f, 0.80f, madeira);
        Cubo("LC_Plataforma_Bolinha", g, 1.0f, 0.47f, 5.05f, 1.05f, 0.94f, 1.05f, tecido);
        Cubo("LC_Pilha_Subida_1", g, 2.75f, 0.24f, 4.2f, 0.85f, 0.48f, 0.85f, tecido);
        Cubo("LC_Pilha_Subida_2", g, 2.2f, 0.38f, 5.2f, 0.85f, 0.76f, 0.85f, tecido);
    }

    private static void CriarLabirintoDireito(Transform parent, Material caixa, Material tecido, Material escura, Material madeira)
    {
        Transform g = NovoGrupo("Labirinto_Direito", parent);
        Cubo("LD_Estante_Oeste", g, 4.2f, 0.7f, -1.0f, 0.42f, 1.4f, 4.5f, escura);
        Cubo("LD_Estante_Norte", g, 6.35f, 0.7f, 1.0f, 4.3f, 1.4f, 0.42f, escura);
        Cubo("LD_Estante_Leste", g, 8.0f, 0.7f, -1.1f, 0.42f, 1.4f, 4.7f, escura);
        Cubo("LD_Estante_Sul_A", g, 6.0f, 0.7f, -3.45f, 2.8f, 1.4f, 0.42f, escura);
        Cubo("LD_Estante_Sul_B", g, 7.75f, 0.7f, -2.0f, 0.42f, 1.4f, 2.3f, escura);
        Cubo("LD_Divisoria_1", g, 6.0f, 0.62f, -1.1f, 2.8f, 1.24f, 0.35f, escura);
        Cubo("LD_Divisoria_2", g, 5.1f, 0.62f, 0.0f, 0.35f, 1.24f, 2.0f, escura);

        Cubo("LD_Pilha_Janela_A", g, 3.8f, 0.23f, -3.2f, 0.9f, 0.46f, 0.9f, caixa);
        Cubo("LD_Pilha_Janela_B", g, 4.7f, 0.38f, -2.55f, 0.9f, 0.76f, 0.9f, caixa);
        Cubo("LD_Pilha_Janela_C", g, 5.5f, 0.50f, -1.85f, 0.9f, 1.0f, 0.9f, caixa);
        Cubo("LD_Banco_Caminho", g, 6.65f, 0.36f, -0.15f, 1.5f, 0.72f, 0.80f, madeira);
        Cubo("LD_Poltrona_Assento", g, 6.8f, 0.38f, 3.9f, 1.3f, 0.76f, 1.2f, tecido);
        Cubo("LD_Poltrona_Encosto", g, 6.8f, 0.95f, 4.42f, 1.3f, 1.1f, 0.22f, tecido);
        Cubo("LD_Poltrona_Braco_1", g, 6.14f, 0.62f, 3.9f, 0.17f, 0.9f, 1.18f, tecido);
        Cubo("LD_Poltrona_Braco_2", g, 7.46f, 0.62f, 3.9f, 0.17f, 0.9f, 1.18f, tecido);
    }

    private static void ConfigurarEscaladaExtra(GameObject scooby, Transform grupoNovo, Transform moveisOriginais, Transform paredesOriginais)
    {
        ScoobyEscalada escalada = scooby.GetComponent<ScoobyEscalada>();
        if (escalada == null)
        {
            Debug.LogWarning("Sausage Run 6B: ScoobyEscalada nao encontrado no Scooby.");
            return;
        }

        List<Collider> extras = new List<Collider>();
        // Mantem as caixas originais e o peitoril via campos antigos do script.
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Esquerdo/LE_Pilha_1");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Esquerdo/LE_Pilha_2");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Esquerdo/LE_Banco");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Central/LC_Banco_A_Topo");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Central/LC_Banco_B_Topo");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Central/LC_Banco_C_Topo");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Central/LC_Plataforma_Bolinha");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Central/LC_Pilha_Subida_1");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Central/LC_Pilha_Subida_2");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Direito/LD_Pilha_Janela_A");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Direito/LD_Pilha_Janela_B");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Direito/LD_Banco_Caminho");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Direito/LD_Poltrona_Assento");
        AdicionarCollider(extras, grupoNovo, "01_Sala_Labirintos/Labirinto_Direito/LD_Pilha_Janela_C");
        escalada.ObjetosEscalaveisExtras = extras.ToArray();
        EditorUtility.SetDirty(escalada);
    }

    private static void AdicionarCollider(List<Collider> lista, Transform raiz, string caminho)
    {
        Transform t = raiz.Find(caminho);
        if (t == null) return;
        Collider c = t.GetComponent<Collider>();
        if (c != null && !lista.Contains(c)) lista.Add(c);
    }

    private static void ReposicionarBolinha(Transform gameplay, Transform grupoNovo)
    {
        Transform bolinha = gameplay.Find("Bolinha");
        if (bolinha == null) return;
        Transform plataforma = grupoNovo.Find("01_Sala_Labirintos/Labirinto_Central/LC_Plataforma_Bolinha");
        if (plataforma == null) return;

        Undo.RecordObject(bolinha, "Reposicionar bolinha na etapa 6B");
        bolinha.position = plataforma.position + Vector3.up * 0.72f;
        EditorUtility.SetDirty(bolinha);

        BolinhaColetavel bc = bolinha.GetComponent<BolinhaColetavel>();
        if (bc != null)
        {
            SerializedObject so = new SerializedObject(bc);
            SerializedProperty prop = so.FindProperty("posicaoInicial");
            if (prop != null)
            {
                prop.vector3Value = bolinha.position;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(bc);
        }
    }

    private static void AtualizarJogo(SausageRunGameManager jogo, MariaAI ia, Transform mariaTransform)
    {
        Undo.RecordObject(jogo, "Atualizar limites de vitoria");
        jogo.zMaximoParaVitoria = -5.4f;
        jogo.zMinimoDoCorredor = -9.0f;
        jogo.xMaximoCorredor = 8.8f;
        EditorUtility.SetDirty(jogo);

        Undo.RecordObject(ia, "Atualizar IA da Maria");
        ia.limiteLateralX = 8.6f;
        ia.limiteFundoDaSalaZ = 8.7f;
        ia.limiteFrontalDaSalaZ = -4.7f;
        ia.margemParaDestino = 0.45f;
        ia.pontosPatrulha = new Vector3[]
        {
            new Vector3(-5.7f, 0f, -2.6f),
            new Vector3(-6.2f, 0f, 2.0f),
            new Vector3(-4.8f, 0f, 6.4f),
            new Vector3(0.1f, 0f, 7.2f),
            new Vector3(4.7f, 0f, 6.2f),
            new Vector3(6.5f, 0f, 2.6f),
            new Vector3(6.0f, 0f, -2.4f),
            new Vector3(1.2f, 0f, -2.2f)
        };
        EditorUtility.SetDirty(ia);

        if (mariaTransform != null)
        {
            Undo.RecordObject(mariaTransform, "Reposicionar Maria");
            mariaTransform.position = new Vector3(1.8f, 0f, 0.6f);
            EditorUtility.SetDirty(mariaTransform);
        }
    }

    private static void AtualizarNavMesh(NavMeshSurface superficie, Transform pisos)
    {
        Undo.RecordObject(superficie, "Atualizar NavMesh 6B");
        superficie.collectObjects = CollectObjects.Volume;
        superficie.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        superficie.center = new Vector3(0f, 1.8f, 2f);
        superficie.size = new Vector3(18.5f, 4.2f, 14.5f);
        EditorUtility.SetDirty(superficie);

        GameObject pisoExterior = pisos.Find("Chao_Corredor").gameObject;
        NavMeshModifier ignorarCorredor = pisoExterior.GetComponent<NavMeshModifier>();
        if (ignorarCorredor == null)
            ignorarCorredor = Undo.AddComponent<NavMeshModifier>(pisoExterior);
        Undo.RecordObject(ignorarCorredor, "Excluir corredor da NavMesh");
        ignorarCorredor.ignoreFromBuild = true;
        ignorarCorredor.applyToChildren = false;
        EditorUtility.SetDirty(ignorarCorredor);
    }

    private static void Alterar(Transform grupo, string nome, Vector3 pos, Vector3 escala)
    {
        Transform t = grupo.Find(nome);
        Undo.RecordObject(t, "Alterar estrutura 6B");
        t.localPosition = pos;
        t.localScale = escala;
        EditorUtility.SetDirty(t);
    }

    private static void Desativar(Transform grupo, string nome)
    {
        Transform t = grupo.Find(nome);
        if (t == null) return;
        Undo.RecordObject(t.gameObject, "Desativar borda antiga do corredor");
        t.gameObject.SetActive(false);
        EditorUtility.SetDirty(t.gameObject);
    }

    private static Transform NovoGrupo(string nome, Transform parent)
    {
        GameObject g = new GameObject(nome);
        g.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(g, "Criar grupo 6B");
        return g.transform;
    }

    private static GameObject Cubo(string nome, Transform parent,
        float x, float y, float z, float sx, float sy, float sz,
        Material material, bool visivel = true, bool colidir = true)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nome;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = new Vector3(x, y, z);
        g.transform.localRotation = Quaternion.identity;
        g.transform.localScale = new Vector3(sx, sy, sz);
        Renderer r = g.GetComponent<Renderer>();
        if (r != null)
        {
            r.sharedMaterial = material;
            r.enabled = visivel;
        }
        BoxCollider c = g.GetComponent<BoxCollider>();
        if (c != null) c.enabled = colidir;
        Undo.RegisterCreatedObjectUndo(g, "Adicionar objeto 6B: " + nome);
        return g;
    }

    private static Material Mat(string nome, Color cor)
    {
        if (!AssetDatabase.IsValidFolder(MaterialsPath))
            AssetDatabase.CreateFolder("Assets/_SausageRun3D", "Materials");

        string path = MaterialsPath + "/" + nome + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            m.name = nome;
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", cor);
        if (m.HasProperty("_Color")) m.SetColor("_Color", cor);
        EditorUtility.SetDirty(m);
        return m;
    }
}
