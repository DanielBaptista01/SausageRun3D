using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Etapa 7: cinco desafios de biscoitos em moveis reais da sala.
/// O jogo ja existente (Scooby, Maria, janela, bolinha e vitoria) e preservado.
/// Funciona tanto sobre 06B_Expansao quanto 06B_LabirintosFechados.
/// </summary>
public static class SausageRunDesafios7Setup
{
    private const string Grupo = "07_Desafio_Biscoitos";
    private const string MaterialDir = "Assets/_SausageRun3D/Materials";
    private static readonly List<Collider> Escalaveis = new List<Collider>();
    private static readonly List<BiscoitoColetavel> Biscoitos = new List<BiscoitoColetavel>();

    [MenuItem("Sausage Run/7 - Criar desafios e 5 biscoitos")]
    public static void Criar()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sausage Run - Etapa 7", "Saia do modo Play primeiro.", "OK");
            return;
        }

        Scene cena = SceneManager.GetActiveScene();
        GameObject raiz = GameObject.Find("SausageRun_Prototipo");
        GameObject scooby = GameObject.Find("Scooby");
        GameObject maria = GameObject.Find("Maria");
        GameObject managerGo = GameObject.Find("SausageRun_Jogo");
        GameObject navGo = GameObject.Find("Sala_NavMesh");
        if (string.IsNullOrEmpty(cena.path) || raiz == null || scooby == null ||
            maria == null || managerGo == null || navGo == null)
        {
            EditorUtility.DisplayDialog("Sausage Run - Etapa 7",
                "Abra a cena Sala das etapas anteriores e confira se ela esta salva. " +
                "Os objetos SausageRun_Prototipo, Scooby, Maria, SausageRun_Jogo e Sala_NavMesh sao necessarios.", "OK");
            return;
        }

        Transform moveisAntigos = raiz.transform.Find("04_Moveis_e_obstaculos");
        Transform gameplay = raiz.transform.Find("05_Scooby_Maria_Bolinha");
        Transform piso = raiz.transform.Find("01_Pisos");
        if (moveisAntigos == null || gameplay == null || piso == null ||
            raiz.transform.Find("06B_Expansao") == null && raiz.transform.Find("06B_LabirintosFechados") == null)
        {
            EditorUtility.DisplayDialog("Sausage Run - Etapa 7",
                "Execute antes a Etapa 6B que amplia a sala. A etapa 7 requer o cenario 18 x 14 m.", "OK");
            return;
        }

        Transform bolinhaT = gameplay.Find("Bolinha");
        BolinhaColetavel bolinha = bolinhaT != null ? bolinhaT.GetComponent<BolinhaColetavel>() : null;
        ScoobyEscalada escalada = scooby.GetComponent<ScoobyEscalada>();
        ScoobyController movimento = scooby.GetComponent<ScoobyController>();
        SausageRunGameManager jogo = managerGo.GetComponent<SausageRunGameManager>();
        MariaAI ia = maria.GetComponent<MariaAI>();
        NavMeshSurface nav = navGo.GetComponent<NavMeshSurface>();

        if (bolinha == null || escalada == null || movimento == null || jogo == null || ia == null || nav == null)
        {
            EditorUtility.DisplayDialog("Sausage Run - Etapa 7",
                "Verifique ScoobyEscalada, ScoobyController, BolinhaColetavel, " +
                "SausageRunGameManager, MariaAI e NavMeshSurface na cena.", "OK");
            return;
        }

        if (raiz.transform.position.sqrMagnitude > 0.0001f ||
            (raiz.transform.localScale - Vector3.one).sqrMagnitude > 0.0001f)
        {
            EditorUtility.DisplayDialog("Sausage Run - Etapa 7",
                "O objeto SausageRun_Prototipo precisa permanecer em (0,0,0), escala (1,1,1).", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Sausage Run - Etapa 7",
            "Criar uma sala com 5 desafios obrigatorios?\n\n" +
            "1. Entrar por baixo do aparador\n" +
            "2. Subir pelas cadeiras ate a mesa\n" +
            "3. Resolver o labirinto de almofadas\n" +
            "4. Escalar caixas e estantes\n" +
            "5. Descobrir a passagem atras do rack\n\n" +
            "A bolinha sera trancada ate 5/5 biscoitos.\n" +
            "Moveis antigos serao desativados, nao apagados.\n\n" +
            "Sera criado um backup da cena.", "Criar desafios", "Cancelar"))
            return;

        string backup = AssetDatabase.GenerateUniqueAssetPath(
            "Assets/_SausageRun3D/Scenes/Sala_BACKUP_AntesBiscoitos.unity");
        if (!EditorSceneManager.SaveScene(cena, backup, true))
        {
            EditorUtility.DisplayDialog("Etapa 7", "Nao foi possivel salvar o backup.", "OK");
            return;
        }

        try
        {
            Transform anterior = raiz.transform.Find(Grupo);
            if (anterior != null) Undo.DestroyObjectImmediate(anterior.gameObject);

            DesativarMoveisAnteriores(raiz.transform, moveisAntigos);
            Escalaveis.Clear();
            Biscoitos.Clear();

            Transform novo = NovoGrupo(Grupo, raiz.transform);
            Transform modulo1 = NovoGrupo("01_Sofa_e_Tunel", novo);
            Transform modulo2 = NovoGrupo("02_Mesa_de_Jantar", novo);
            Transform modulo3 = NovoGrupo("03_Forte_de_Almofadas", novo);
            Transform modulo4 = NovoGrupo("04_Estantes_e_Caixas", novo);
            Transform modulo5 = NovoGrupo("05_Rack_e_Passagem_Secreta", novo);
            Transform objetivo = NovoGrupo("06_Bolinha_Trancada", novo);
            Transform cookies = NovoGrupo("07_Biscoitos", novo);
            Transform teto = NovoGrupo("08_Tetos", novo);
            Transform iluminacao = NovoGrupo("09_Iluminacao", novo);
            Transform detalhes = NovoGrupo("10_Objetos_Extras_Sala", novo);

            Material madeira = Material("7_Madeira", new Color(0.56f, 0.35f, 0.18f));
            Material madeiraEscura = Material("7_Madeira_Escura", new Color(0.29f, 0.19f, 0.14f));
            Material bege = Material("7_Almofada_Bege", new Color(0.80f, 0.71f, 0.55f));
            Material verde = Material("7_Sofa_Verde", new Color(0.20f, 0.42f, 0.32f));
            Material azul = Material("7_Tecido_Azul", new Color(0.24f, 0.38f, 0.61f));
            Material mostarda = Material("7_Almofada_Mostarda", new Color(0.84f, 0.56f, 0.22f));
            Material marrom = Material("7_Biscoito", new Color(0.87f, 0.59f, 0.25f));
            Material metal = Material("7_Metal", new Color(0.40f, 0.43f, 0.46f));
            Material tapete = Material("7_Tapete", new Color(0.48f, 0.29f, 0.37f));
            Material vidro = Material("7_Grade", new Color(0.19f, 0.47f, 0.51f));
            Material branco = Material("7_Teto", new Color(0.84f, 0.84f, 0.80f));
            Material amarelo = Material("7_Brinquedos_Amarelos", new Color(0.83f, 0.62f, 0.30f));

            ConstruirSofa(modulo1, madeiraEscura, verde, bege, tapete);
            ConstruirMesa(modulo2, madeira, madeiraEscura, azul);
            ConstruirForte(modulo3, bege, mostarda, madeira);
            ConstruirEscalada(modulo4, madeiraEscura, amarelo, madeira);
            ConstruirRack(modulo5, madeiraEscura, metal, azul, bege);
            ConstruirDetalhes(detalhes, madeira, madeiraEscura, azul, verde, bege, tapete);
            GameObject portao = ConstruirBolinha(objetivo, bolinhaT, madeira, vidro, bege);

            CriarBiscoito(cookies, 1, "tunel sob o aparador", new Vector3(-7.55f, 0.62f, -1.90f), marrom, scooby.transform);
            CriarBiscoito(cookies, 2, "mesa de jantar", new Vector3(6.25f, 1.47f, -1.45f), marrom, scooby.transform);
            CriarBiscoito(cookies, 3, "forte de almofadas", new Vector3(-4.45f, 0.67f, 4.85f), marrom, scooby.transform);
            CriarBiscoito(cookies, 4, "topo das estantes", new Vector3(7.35f, 2.60f, 5.35f), marrom, scooby.transform);
            CriarBiscoito(cookies, 5, "passagem atras do rack", new Vector3(0f, 0.65f, 7.75f), marrom, scooby.transform);

            ConstruirTetos(teto, branco);
            ConstruirLuzes(iluminacao);
            CorrigirEscadaGaragem(raiz.transform);
            ConfigurarMissao(novo, Biscoitos, jogo, bolinha, portao, movimento, escalada);
            ConfigurarEscalaveis(escalada);
            ConfigurarIA(ia, maria.transform);
            AjustarNavegacao(nav, piso);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            bool bakeOK = false;
            try
            {
                nav.BuildNavMesh();
                EditorSceneManager.SaveScene(cena);
                bakeOK = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Etapa 7: a NavMesh precisa de Bake manual: " + e.Message);
            }

            Selection.activeGameObject = novo.gameObject;
            EditorGUIUtility.PingObject(novo.gameObject);
            EditorUtility.DisplayDialog("Sausage Run - Etapa 7 instalada",
                "Foram criados 5 desafios com objetos reais da sala.\n" +
                "A bolinha foi movida para uma cesta com grade travada.\n" +
                "Agora a coleta exige 5/5 biscoitos.\n\n" +
                "A escalada antiga da janela foi preservada.\n" +
                (bakeOK ? "NavMesh refeita.\n" : "Verifique o Bake de Sala_NavMesh.\n") +
                "Backup: " + backup + "\n\n" +
                "Teste o percurso completo antes de decorar o cenario.", "OK");
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao criar Etapa 7. Backup em: " + backup + "\n" + e);
            EditorUtility.DisplayDialog("Erro Etapa 7", "Falha ao criar a etapa.\nBackup: " + backup + "\nVerifique o Console.", "OK");
        }
    }

    private static void DesativarMoveisAnteriores(Transform raiz, Transform moveis)
    {
        foreach (Transform t in moveis)
        {
            if (t.name == "Caixa_Externa_Janela" || t.name == "Caixa_Interna_Janela")
                continue;
            SetActiveUndo(t.gameObject, false);
        }

        Transform old = raiz.Find("06B_Expansao");
        if (old != null)
        {
            Transform oldFurniture = old.Find("01_Novos_Moveis_Sala");
            if (oldFurniture != null) SetActiveUndo(oldFurniture.gameObject, false);
            // Tapetes e plantas antigas podem ficar, mas podem atrapalhar os novos tunéis.
            Transform oldDetails = old.Find("06_Detalhes");
            if (oldDetails != null) SetActiveUndo(oldDetails.gameObject, false);
        }
        Transform lab = raiz.Find("06B_LabirintosFechados");
        if (lab != null)
        {
            Transform oldFurniture = lab.Find("01_Sala_Labirintos");
            if (oldFurniture != null) SetActiveUndo(oldFurniture.gameObject, false);
            Transform oldRoof = lab.Find("04_Tetos");
            if (oldRoof != null) SetActiveUndo(oldRoof.gameObject, false);
        }
    }

    private static void ConstruirSofa(Transform pai, Material escuro, Material sofa, Material almofada, Material tapete)
    {
        // Recinto de 4,5 x 3,4 m. So existe acesso pelo tunel sob o aparador.
        Estante("Estante_Tras_Sofa", pai, -6.3f, -1.25f, 4.35f, 2.1f, 0.38f, escuro);
        EstanteVertical("Livreiro_Parede_Esquerda", pai, -8.45f, -2.7f, 3.25f, 1.95f, escuro);
        EstanteVertical("Livreiro_Parede_Direita", pai, -4.15f, -2.7f, 3.25f, 1.95f, escuro);
        // Barricada sul de sofa + estante, com abertura central sob um aparador.
        Cubo("Sofa_Assento_Frente", pai, -7.65f, 0.36f, -4.24f, 1.6f, 0.72f, 0.55f, sofa);
        Cubo("Sofa_Encosto_Alto", pai, -7.65f, 1.06f, -4.24f, 1.6f, 2.12f, 0.32f, sofa);
        Cubo("Estante_Frente_Direita", pai, -4.78f, 0.95f, -4.24f, 1.36f, 1.90f, 0.40f, escuro);
        Cubo("Aparador_Passagem_Topo", pai, -6.15f, 0.87f, -4.24f, 1.36f, 0.15f, 1.15f, escuro);
        // Os pes ficam nas extremidades; por baixo ha passagem livre de 0,795 m.
        Cubo("Aparador_Pe_Esq", pai, -6.75f, 0.38f, -4.24f, 0.12f, 0.76f, 1.14f, escuro);
        Cubo("Aparador_Pe_Dir", pai, -5.55f, 0.38f, -4.24f, 0.12f, 0.76f, 1.14f, escuro);
        Cubo("Tapete_Da_Sala", pai, -6.4f, 0.010f, -2.7f, 3.4f, 0.02f, 1.5f, tapete, false);
        Cubo("Almofada_Escondida", pai, -7.5f, 0.21f, -2.7f, 0.75f, 0.42f, 0.63f, almofada);
        Cubo("Cesto_de_Brinquedos", pai, -4.85f, 0.35f, -1.95f, 0.6f, 0.7f, 0.65f, escuro);
    }

    private static void ConstruirMesa(Transform pai, Material madeira, Material escura, Material tecido)
    {
        // O biscoito fica sobre a mesa, alto demais para alcancar do piso.
        Cubo("Mesa_Jantar_Tampo", pai, 5.90f, 1.045f, -1.45f, 2.25f, 0.14f, 1.55f, madeira, true);
        for (int i = -1; i <= 1; i += 2)
            for (int j = -1; j <= 1; j += 2)
                Cubo("Pe_Mesa_" + i + "_" + j, pai, 5.90f + 0.92f * i, 0.49f,
                    -1.45f + 0.58f * j, 0.14f, 0.98f, 0.14f, escura);
        Cubo("Cadeira_Assento_Escalavel", pai, 4.22f, 0.25f, -2.45f, 0.88f, 0.50f, 0.82f, tecido, true, true);
        Cubo("Cadeira_Encosto", pai, 3.82f, 0.64f, -2.45f, 0.12f, 0.78f, 0.82f, escura);
        Cubo("Cadeira_Dois_Assento", pai, 7.8f, 0.25f, -0.4f, 0.80f, 0.50f, 0.8f, tecido);
        Cubo("Cadeira_Dois_Encosto", pai, 8.2f, 0.62f, -0.4f, 0.12f, 0.75f, 0.8f, escura);
        Cubo("Vaso_Mesa", pai, 5.35f, 1.20f, -1.85f, 0.32f, 0.22f, 0.32f, tecido, false);
    }

    private static void ConstruirForte(Transform pai, Material bege, Material mostarda, Material madeira)
    {
        // Forte de almofadas e caixas fechado. A unica entrada fica na frente esquerda.
        EstanteVertical("Forte_Lateral_Esquerda", pai, -8.45f, 3.35f, 4.12f, 1.85f, madeira);
        EstanteVertical("Forte_Lateral_Direita", pai, -3.85f, 3.35f, 4.12f, 1.85f, madeira);
        Cubo("Sofa_Costas_Forte", pai, -6.15f, 0.95f, 5.50f, 4.62f, 1.9f, 0.36f, bege);
        Cubo("Forte_Entrada_Lado_Esq", pai, -7.8f, 0.85f, 1.20f, 1.3f, 1.7f, 0.34f, bege);
        Cubo("Forte_Entrada_Lado_Dir", pai, -4.88f, 0.85f, 1.20f, 2.08f, 1.7f, 0.34f, bege);
        // Paredes interiores alternadas: caminho em zigue-zague, sem atalhos externos.
        Cubo("Almofadas_Divisoria_1", pai, -5.45f, 0.78f, 2.35f, 2.75f, 1.56f, 0.38f, mostarda);
        Cubo("Almofadas_Divisoria_2", pai, -6.65f, 0.78f, 3.50f, 2.70f, 1.56f, 0.38f, bege);
        Cubo("Almofadas_Divisoria_3", pai, -5.45f, 0.78f, 4.48f, 2.75f, 1.56f, 0.38f, mostarda);
        // Pequenos brinquedos em cima das almofadas, visuais.
        Cubo("Bichinho_Pelucia", pai, -7.30f, 0.23f, 2.65f, 0.35f, 0.46f, 0.35f, bege, false);
    }

    private static void ConstruirEscalada(Transform pai, Material escura, Material caixa, Material madeira)
    {
        // Escadinha de caixas com alturas progressivas e plataformas de verdade.
        Cubo("Caixa_Passo_1", pai, 4.20f, 0.25f, 2.50f, 0.95f, 0.50f, 0.95f, caixa, true, true);
        Cubo("Caixa_Passo_2", pai, 5.35f, 0.60f, 3.25f, 0.95f, 1.20f, 0.95f, caixa, true, true);
        Cubo("Caixa_Passo_3", pai, 6.48f, 0.88f, 4.00f, 0.95f, 1.76f, 0.95f, caixa, true, true);
        Cubo("Prateleira_Destino", pai, 7.35f, 1.125f, 5.35f, 1.4f, 2.25f, 1.0f, madeira, true, true);
        Cubo("Armario_Fundo", pai, 8.22f, 1.25f, 5.30f, 0.46f, 2.50f, 2.1f, escura);
        Cubo("Estante_Norte", pai, 6.25f, 1.12f, 6.55f, 3.55f, 2.24f, 0.38f, escura);
        // No piso ha um banquinho que cria esconderijo debaixo do tampo.
        Cubo("Banco_Proximo_Estante", pai, 4.75f, 0.73f, 5.45f, 1.25f, 0.13f, 0.72f, madeira, true, true);
        foreach (float dx in new[] { -0.48f, 0.48f })
            foreach (float dz in new[] { -0.25f, 0.25f })
                Cubo("Pe_Banco_" + dx + "_" + dz, pai, 4.75f + dx, 0.33f, 5.45f + dz,
                    0.12f, 0.66f, 0.12f, madeira);
    }

    private static void ConstruirRack(Transform pai, Material madeira, Material metal, Material tecido, Material almofada)
    {
        // Pequeno recinto de estantes com entrada sob o rack, teto baixo e passagem secreta.
        Cubo("Armario_Lateral_Esquerda", pai, -2.0f, 1.00f, 7.05f, 0.38f, 2.0f, 2.70f, madeira);
        Cubo("Armario_Lateral_Direita", pai, 2.0f, 1.00f, 7.05f, 0.38f, 2.0f, 2.70f, madeira);
        Cubo("Painel_TV_Fundo", pai, 0f, 1.00f, 8.45f, 4.40f, 2.0f, 0.35f, madeira);
        Cubo("Rack_Fachada_Esquerda", pai, -1.38f, 0.90f, 5.72f, 1.30f, 1.80f, 0.40f, madeira);
        Cubo("Rack_Fachada_Direita", pai, 1.38f, 0.90f, 5.72f, 1.30f, 1.80f, 0.40f, madeira);
        Cubo("Rack_Topo_Tunel", pai, 0f, 0.83f, 5.72f, 1.14f, 0.13f, 1.00f, madeira);
        // Túnel com altura livre de 0.765 m: Scooby cabe, Maria nao.
        Cubo("Rack_Coluna_L", pai, -0.55f, 0.35f, 5.72f, 0.11f, 0.70f, 0.88f, madeira);
        Cubo("Rack_Coluna_R", pai, 0.55f, 0.35f, 5.72f, 0.11f, 0.70f, 0.88f, madeira);
        Cubo("TV_Tela", pai, 0f, 1.52f, 8.20f, 1.65f, 0.80f, 0.10f, metal, false);
        Cubo("Almofada_Atras", pai, 1.10f, 0.20f, 7.15f, 0.58f, 0.4f, 0.58f, almofada);
        Cubo("Caixa_de_Jogos", pai, -1.10f, 0.30f, 7.5f, 0.65f, 0.60f, 0.65f, tecido);
    }

    private static void ConstruirDetalhes(Transform pai, Material madeira,
        Material escura, Material azul, Material verde, Material almofada, Material tapete)
    {
        // Objetos de uma sala real que reforcam as rotas e escondem a Maria.
        Cubo("Poltrona_Assento", pai, -2.5f, 0.40f, -1.15f, 1.05f, 0.80f, 1.05f, azul);
        Cubo("Poltrona_Encosto", pai, -2.5f, 0.94f, -0.68f, 1.05f, 1.07f, 0.18f, azul);
        Cubo("Poltrona_Braco_A", pai, -3.07f, 0.59f, -1.15f, 0.15f, 0.95f, 1.05f, azul);
        Cubo("Poltrona_Braco_B", pai, -1.93f, 0.59f, -1.15f, 0.15f, 0.95f, 1.05f, azul);
        Cubo("Mesa_Lateral_Tampo", pai, -1.15f, 0.60f, -1.75f, 0.65f, 0.12f, 0.60f, madeira);
        Cubo("Mesa_Lateral_Base", pai, -1.15f, 0.29f, -1.75f, 0.14f, 0.58f, 0.14f, escura);
        Cubo("Tapete_Central", pai, 0.8f, 0.012f, -0.15f, 3.65f, 0.024f, 2.2f, tapete, false);
        Cubo("Cesto_de_Pelucias", pai, -2.9f, 0.30f, 7.65f, 0.72f, 0.60f, 0.62f, madeira);
        Cubo("Almofada_no_Cesto", pai, -2.9f, 0.67f, 7.65f, 0.55f, 0.21f, 0.45f, almofada, false);
        Cubo("Vaso_Planta", pai, 3.0f, 0.30f, 7.60f, 0.48f, 0.60f, 0.48f, escura);
        Cubo("Folhagem_Planta", pai, 3.0f, 1.05f, 7.60f, 0.62f, 0.90f, 0.62f, verde, false);
    }

    private static GameObject ConstruirBolinha(Transform pai, Transform bolinha,
        Material madeira, Material grade, Material forro)
    {
        // Cesto fechado: o unico acesso ao interior e pela grade frontal desbloqueavel.
        Cubo("Baú_Bola_Parede_Fundo", pai, 0f, 0.70f, 3.95f, 2.10f, 1.40f, 0.18f, madeira);
        Cubo("Bau_Bola_Lado_Esq", pai, -1.00f, 0.70f, 3.15f, 0.18f, 1.40f, 1.65f, madeira);
        Cubo("Bau_Bola_Lado_Dir", pai, 1.00f, 0.70f, 3.15f, 0.18f, 1.40f, 1.65f, madeira);
        Cubo("Bau_Bola_Piso", pai, 0f, 0.07f, 3.15f, 2.0f, 0.14f, 1.60f, forro);
        // Barra com collider unico; fechar fisicamente e impedir pegar a bolinha.
        Transform porta = NovoGrupo("Grade_Bolinha_Trancada_0_de_5", pai);
        Cubo("Bloqueio_Frontal", porta, 0f, 0.71f, 2.29f, 2.04f, 1.42f, 0.13f, grade);
        // Detalhes frontais, sem colisor extra.
        for (int i = -3; i <= 3; i++)
            Cubo("Barra_" + i, porta, i * 0.27f, 0.76f, 2.20f, 0.055f, 1.40f, 0.06f,
                madeira, false);
        Cubo("Trava_Central", porta, 0f, 1.12f, 2.16f, 0.32f, 0.22f, 0.09f, grade, false);
        Undo.RecordObject(bolinha, "Mover bolinha para a cesta trancada");
        bolinha.localPosition = new Vector3(0f, 0.37f, 3.35f);
        EditorUtility.SetDirty(bolinha);
        return porta.gameObject;
    }

    private static void CriarBiscoito(Transform pai, int indice, string nome,
        Vector3 pos, Material material, Transform scooby)
    {
        GameObject biscoito = new GameObject("Biscoito_" + indice + "_" + nome.Replace(' ', '_'));
        biscoito.transform.SetParent(pai, false);
        biscoito.transform.localPosition = pos;
        biscoito.transform.localRotation = Quaternion.identity;
        Undo.RegisterCreatedObjectUndo(biscoito, "Criar biscoito de desafio");
        SphereCollider trigger = biscoito.AddComponent<SphereCollider>();
        trigger.radius = 0.30f;
        trigger.isTrigger = true;
        Rigidbody corpo = biscoito.AddComponent<Rigidbody>();
        corpo.useGravity = false;
        corpo.isKinematic = true;
        NavMeshModifier ignorar = biscoito.AddComponent<NavMeshModifier>();
        ignorar.ignoreFromBuild = true;
        ignorar.applyToChildren = true;
        BiscoitoColetavel script = biscoito.AddComponent<BiscoitoColetavel>();
        script.nomeDesafio = nome;
        script.scooby = scooby;
        Biscoitos.Add(script);

        Transform osso = NovoGrupo("Modelo_Osso", biscoito.transform);
        osso.localRotation = Quaternion.Euler(65f, 0f, 0f);
        Cubo("Corpo_do_Biscoito", osso, 0f, 0f, 0f, 0.36f, 0.14f, 0.17f, material, false);
        foreach (float x in new[] { -0.20f, 0.20f })
            foreach (float z in new[] { -0.14f, 0.14f })
                Esfera("Ponta_do_Osso", osso, new Vector3(x, 0f, z),
                    new Vector3(0.18f, 0.14f, 0.18f), material);
    }

    private static void ConstruirTetos(Transform pai, Material cor)
    {
        // Renderizados e com collider: a sala fica fisicamente coberta.
        Cubo("Teto_Completo_Sala", pai, 0f, 3.39f, 2f, 18.2f, 0.13f, 14.2f, cor);
        Cubo("Teto_Completo_Corredor", pai, 0f, 4.49f, -7f, 18.2f, 0.13f, 4.2f, cor);
        Cubo("Teto_Completo_Garagem", pai, -11.1f, 3.05f, -7f, 5.1f, 0.13f, 3.0f, cor);
        Cubo("Teto_Banheirinho", pai, 10.4f, 3.28f, -8f, 2.95f, 0.13f, 2.15f, cor);
        Cubo("Teto_Lavanderia", pai, 11.0f, 3.69f, -6f, 4.10f, 0.13f, 2.15f, cor);
        NavMeshModifier ignorarTeto = pai.gameObject.AddComponent<NavMeshModifier>();
        ignorarTeto.ignoreFromBuild = true;
        ignorarTeto.applyToChildren = true;
    }

    private static void ConstruirLuzes(Transform pai)
    {
        foreach (float x in new[] { -5.7f, 0f, 5.7f })
            foreach (float z in new[] { -2.4f, 2.5f, 7f })
                Luz(pai, "Luz_Teto_Sala", new Vector3(x, 2.96f, z), 8f, 2.8f, new Color(1f, 0.90f, 0.75f));
        foreach (float x in new[] { -5f, 0f, 5f })
            Luz(pai, "Luz_Corredor", new Vector3(x, 4.02f, -7f), 7f, 2.5f,
                new Color(1f, 0.95f, 0.82f));
    }

    private static void CorrigirEscadaGaragem(Transform raiz)
    {
        Transform g = raiz.Find("06B_Expansao/03_Grade_e_Escada_Garagem");
        if (g == null) return;
        for (int i = 0; i < 6; i++)
        {
            Transform t = g.Find("Degrau_Garagem_" + (i + 1));
            if (t == null) continue;
            Undo.RecordObject(t, "Virar escada para cima");
            t.localPosition = new Vector3(-9.37f - 0.53f * i, 0.12f + 0.18f * i, -7f);
        }
        Transform patamar = g.Find("Patamar_Visivel_Garagem");
        if (patamar != null)
        {
            Undo.RecordObject(patamar, "Subir patamar da garagem");
            patamar.localPosition = new Vector3(-13.05f, 1.14f, -7f);
        }
    }

    private static void ConfigurarMissao(Transform grupo, List<BiscoitoColetavel> biscoitos,
        SausageRunGameManager jogo, BolinhaColetavel bolinha, GameObject porta,
        ScoobyController movimento, ScoobyEscalada escalada)
    {
        DesafioBiscoitos desafio = grupo.gameObject.AddComponent<DesafioBiscoitos>();
        desafio.biscoitos = biscoitos.ToArray();
        desafio.bolinha = bolinha;
        desafio.portaoDaBolinha = porta;
        desafio.jogo = jogo;
        desafio.movimentoScooby = movimento;
        desafio.escaladaScooby = escalada;
        foreach (BiscoitoColetavel cookie in biscoitos)
            cookie.desafio = desafio;

        Undo.RecordObject(bolinha, "Bloquear coleta ate os 5 biscoitos");
        bolinha.enabled = false;
        EditorUtility.SetDirty(bolinha);
        Undo.RecordObject(jogo, "Mostrar novo objetivo dos biscoitos");
        jogo.mostrarObjetivo = false;
        EditorUtility.SetDirty(jogo);
        EditorUtility.SetDirty(desafio);
    }

    private static void ConfigurarEscalaveis(ScoobyEscalada escalada)
    {
        // Editor serializa a lista extra sem sobrescrever referencias originais.
        SerializedObject so = new SerializedObject(escalada);
        SerializedProperty extras = so.FindProperty("objetosEscalaveisExtras");
        if (extras == null)
            throw new Exception("O ScoobyEscalada.cs antigo nao tem 'objetosEscalaveisExtras'. " +
                                "Substitua o script pela versao incluida neste pacote e deixe a Unity recompilar.");
        // Guardar extras de expansoes anteriores e somar os desafios atuais.
        List<Collider> todos = new List<Collider>();
        for (int i = 0; i < extras.arraySize; i++)
        {
            Collider c = extras.GetArrayElementAtIndex(i).objectReferenceValue as Collider;
            if (c != null && c.gameObject.activeInHierarchy && !todos.Contains(c)) todos.Add(c);
        }
        foreach (Collider c in Escalaveis)
            if (c != null && !todos.Contains(c)) todos.Add(c);
        extras.arraySize = todos.Count;
        for (int i = 0; i < todos.Count; i++)
            extras.GetArrayElementAtIndex(i).objectReferenceValue = todos[i];
        so.ApplyModifiedProperties();
    }

    private static void ConfigurarIA(MariaAI ia, Transform maria)
    {
        Undo.RecordObject(ia, "Atualizar patrulha para os biscoitos");
        ia.limiteFrontalDaSalaZ = -4.7f;
        ia.limiteLateralX = 8.6f;
        ia.limiteFundoDaSalaZ = 8.7f;
        ia.pontosPatrulha = new Vector3[]
        {
            new Vector3(0.0f, 0f, -3.30f),
            new Vector3(3.0f, 0f, -2.80f),
            new Vector3(1.8f, 0f, 0.4f),
            new Vector3(-1.8f, 0f, 1.2f),
            new Vector3(-1.9f, 0f, 4.4f),
            new Vector3(3.0f, 0f, 6.8f),
            new Vector3(0.0f, 0f, 6.0f)
        };
        EditorUtility.SetDirty(ia);
        Undo.RecordObject(maria, "Mover Maria para rota acessivel");
        maria.position = new Vector3(0.6f, 0f, 0.0f);
        EditorUtility.SetDirty(maria);
    }

    private static void AjustarNavegacao(NavMeshSurface nav, Transform pisos)
    {
        Undo.RecordObject(nav, "Atualizar NavMesh apos os desafios");
        nav.collectObjects = CollectObjects.Volume;
        nav.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        nav.center = new Vector3(0f, 1.75f, 2f);
        nav.size = new Vector3(18.4f, 4.2f, 14.4f);
        EditorUtility.SetDirty(nav);

        Transform pisoCorredor = pisos.Find("Chao_Corredor");
        if (pisoCorredor != null)
        {
            NavMeshModifier m = pisoCorredor.GetComponent<NavMeshModifier>();
            if (m == null) m = Undo.AddComponent<NavMeshModifier>(pisoCorredor.gameObject);
            m.ignoreFromBuild = true;
            m.applyToChildren = false;
            EditorUtility.SetDirty(m);
        }
    }

    // --- Primitivas ---
    private static Transform NovoGrupo(string nome, Transform pai)
    {
        GameObject go = new GameObject(nome);
        go.transform.SetParent(pai, false);
        Undo.RegisterCreatedObjectUndo(go, "Etapa 7: criar grupo");
        return go.transform;
    }

    private static GameObject Cubo(string nome, Transform pai,
        float x, float y, float z, float sx, float sy, float sz, Material mat,
        bool comCollider = true, bool escalavel = false)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nome;
        go.transform.SetParent(pai, false);
        go.transform.localPosition = new Vector3(x, y, z);
        go.transform.localScale = new Vector3(sx, sy, sz);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        BoxCollider collider = go.GetComponent<BoxCollider>();
        if (!comCollider) collider.enabled = false;
        if (escalavel && comCollider) Escalaveis.Add(collider);
        Undo.RegisterCreatedObjectUndo(go, "Etapa 7: criar objeto " + nome);
        return go;
    }

    private static void Esfera(string nome, Transform pai, Vector3 pos, Vector3 escala, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = nome;
        go.transform.SetParent(pai, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.GetComponent<Collider>().enabled = false;
        Undo.RegisterCreatedObjectUndo(go, "Etapa 7: criar modelo de biscoito");
    }

    private static void Estante(string nome, Transform pai, float x, float z,
        float largura, float altura, float profundidade, Material mat)
    {
        Cubo(nome, pai, x, altura / 2f, z, largura, altura, profundidade, mat);
        for (int i = 1; i <= 3; i++)
            Cubo(nome + "_Prateleira_" + i, pai, x, i * altura / 4f, z - profundidade / 2f - 0.025f,
                largura * 0.94f, 0.045f, 0.08f, mat, false);
    }

    private static void EstanteVertical(string nome, Transform pai, float x, float z,
        float comprimentoZ, float altura, Material mat)
    {
        Cubo(nome, pai, x, altura / 2f, z, 0.38f, altura, comprimentoZ, mat);
    }

    private static void Luz(Transform pai, string nome, Vector3 pos,
        float alcance, float intensidade, Color cor)
    {
        GameObject obj = new GameObject(nome);
        obj.transform.SetParent(pai, false);
        obj.transform.localPosition = pos;
        Light l = obj.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = alcance;
        l.intensity = intensidade;
        l.color = cor;
        l.shadows = LightShadows.None;
        Undo.RegisterCreatedObjectUndo(obj, "Etapa 7: criar luz");
    }

    private static void SetActiveUndo(GameObject go, bool active)
    {
        Undo.RecordObject(go, "Etapa 7: organizar moveis antigos");
        go.SetActive(active);
        EditorUtility.SetDirty(go);
    }

    private static Material Material(string nome, Color cor)
    {
        if (!AssetDatabase.IsValidFolder(MaterialDir))
            AssetDatabase.CreateFolder("Assets/_SausageRun3D", "Materials");
        string path = MaterialDir + "/" + nome + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", cor);
        if (m.HasProperty("_Color")) m.SetColor("_Color", cor);
        EditorUtility.SetDirty(m);
        return m;
    }
}
