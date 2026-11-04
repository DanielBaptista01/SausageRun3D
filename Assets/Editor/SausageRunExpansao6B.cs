using System;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Etapa 6B: amplia o graybox existente SEM destruir as caixas, a janela,
/// os personagens, a bolinha ou os componentes existentes.
/// Unity 6 + AI Navigation. Executar somente no Editor, fora do Play.
/// </summary>
public static class SausageRunExpansao6B
{
    private const string RootName = "SausageRun_Prototipo";
    private const string GroupName = "06B_Expansao";
    private const string MaterialsPath = "Assets/_SausageRun3D/Materials";

    [MenuItem("Sausage Run/6B - Expandir casa e corredor")]
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
        if (pisos == null || paredes == null || portas == null || moveis == null ||
            !VerificarBase(pisos, paredes, portas, moveis))
        {
            EditorUtility.DisplayDialog("Sausage Run 6B",
                "Faltam objetos do cenario original. A expansao foi cancelada para proteger o projeto.", "OK");
            return;
        }

        if (root.transform.position.sqrMagnitude > 0.0001f ||
            (root.transform.localScale - Vector3.one).sqrMagnitude > 0.0001f)
        {
            EditorUtility.DisplayDialog("Sausage Run 6B",
                "SausageRun_Prototipo precisa estar em (0,0,0), escala (1,1,1). " +
                "Ajuste o objeto pai antes de executar.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Sausage Run — Etapa 6B",
            "Ampliar a sala para 18 x 14 m e o corredor para 18 x 4 m?\n\n" +
            "As caixas de escalada, Scooby, Maria, bolinha e janela serao mantidos. " +
            "Os limites de patrulha, vitoria e a NavMesh serao atualizados.\n\n" +
            "Uma copia da cena sera salva antes da mudanca.", "Aplicar expansao", "Cancelar"))
            return;

        // Backup separado, sem mudar a cena ativa. Mantem cada backup com um nome unico.
        string pastaBackup = "Assets/_SausageRun3D/Scenes";
        if (!AssetDatabase.IsValidFolder(pastaBackup))
        {
            EditorUtility.DisplayDialog("Sausage Run 6B", "Pasta Scenes nao encontrada; salve a cena antes.", "OK");
            return;
        }
        string caminhoBackup = AssetDatabase.GenerateUniqueAssetPath(
            pastaBackup + "/Sala_BACKUP_Antes6B.unity");
        if (!EditorSceneManager.SaveScene(cena, caminhoBackup, true))
        {
            EditorUtility.DisplayDialog("Sausage Run 6B",
                "Nao consegui salvar a copia de seguranca. Nenhuma alteracao foi aplicada.", "OK");
            return;
        }

        try
        {
            // Mantemos os MESMOS objetos originais: os campos da ScoobyEscalada
            // continuam apontando para os colliders validos, sem precisar reconfigurar.
            AmpliarEstrutura(pisos, paredes, portas);
            CriarExpansao(root.transform);
            AtualizarJogo(gerenciador.GetComponent<SausageRunGameManager>(),
                          maria.GetComponent<MariaAI>());

            NavMeshSurface superficie = nav.GetComponent<NavMeshSurface>();
            Undo.RecordObject(superficie, "Ajustar area de navegacao 6B");
            superficie.collectObjects = CollectObjects.Volume;
            superficie.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            superficie.center = new Vector3(0f, 1.65f, 2f);
            superficie.size = new Vector3(18f, 3.8f, 14f);
            EditorUtility.SetDirty(superficie);

            // Impede que o piso exterior faca parte da navegacao da Maria.
            GameObject pisoExterior = pisos.Find("Chao_Corredor").gameObject;
            NavMeshModifier ignorarCorredor = pisoExterior.GetComponent<NavMeshModifier>();
            if (ignorarCorredor == null)
                ignorarCorredor = Undo.AddComponent<NavMeshModifier>(pisoExterior);
            Undo.RecordObject(ignorarCorredor, "Excluir corredor da NavMesh");
            ignorarCorredor.ignoreFromBuild = true;
            ignorarCorredor.applyToChildren = false;
            EditorUtility.SetDirty(ignorarCorredor);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);

            bool bakeOK = false;
            try
            {
                superficie.BuildNavMesh();
                EditorSceneManager.SaveScene(cena);
                bakeOK = true;
            }
            catch (Exception bakeErro)
            {
                Debug.LogWarning("Sausage Run 6B: foi preciso gerar NavMesh manualmente: " + bakeErro.Message);
            }

            GameObject expansao = root.transform.Find(GroupName).gameObject;
            Selection.activeGameObject = expansao;
            EditorGUIUtility.PingObject(expansao);
            Debug.Log("Sausage Run 6B aplicado. Backup: " + caminhoBackup +
                ". NavMesh automatica: " + (bakeOK ? "OK" : "verificar Bake"));
            EditorUtility.DisplayDialog("Sausage Run 6B — Pronto!",
                "Sala 18 x 14, corredor 18 x 4 e muros de 4,4 m.\n" +
                "Garagem/grade a esquerda; banheirinho e lavanderia a direita.\n" +
                "Mais moveis e esconderijos adicionados.\n\n" +
                (bakeOK ? "NavMesh reconstruida.\n" :
                  "IMPORTANTE: selecione Sala_NavMesh e clique Bake.\n") +
                "Backup: " + caminhoBackup + "\n\n" +
                "Teste o trajeto de ida e volta pela janela, coleta e perseguicao.", "OK");
        }
        catch (Exception erro)
        {
            Debug.LogError("Sausage Run 6B: expansao incompleta. Recupere o backup: " +
                           caminhoBackup + "\n" + erro);
            EditorUtility.DisplayDialog("Sausage Run 6B — Erro",
                "Houve uma falha durante a expansao.\n\n" +
                "Voce pode reabrir a copia salva em:\n" + caminhoBackup +
                "\n\nConfira a aba Console.", "OK");
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
        string[] reqPortas = {
            "Porta_Frontal_Fechada", "Porta_Esquerda_Fechada", "Porta_Direita_Fechada" };
        string[] reqMoveis = { "Caixa_Externa_Janela", "Caixa_Interna_Janela" };
        return Contem(pisos, reqPisos) && Contem(paredes, reqParedes) &&
               Contem(portas, reqPortas) && Contem(moveis, reqMoveis);
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

        // Sala: Z [-5, 9], X [-9, 9]. A parede frontal CONTINUA em Z=-5.
        Alterar(paredes, "Fundo", new Vector3(0f, 1.65f, 9f), new Vector3(18f, 3.3f, 0.2f));
        Alterar(paredes, "Frente_Esquerda", new Vector3(-6.8f, 1.65f, -5f), new Vector3(4.4f, 3.3f, 0.2f));
        Alterar(paredes, "Frente_Entre_Porta_Janela", new Vector3(-0.75f, 1.65f, -5f), new Vector3(4.9f, 3.3f, 0.2f));
        Alterar(paredes, "Frente_Direita", new Vector3(6.25f, 1.65f, -5f), new Vector3(5.5f, 3.3f, 0.2f));
        Alterar(paredes, "Frente_Acima_Porta", new Vector3(-3.9f, 2.75f, -5f), new Vector3(1.4f, 1.1f, 0.2f));
        // Peitoril de janela PRESERVADO (mesmo Transform / Collider / altura)
        Alterar(paredes, "Frente_Acima_Janela", new Vector3(2.6f, 2.7f, -5f), new Vector3(1.8f, 1.2f, 0.2f));

        // Duas portas laterais continuam perto dos cantos superiores do mapa.
        Alterar(paredes, "Esquerda_Principal", new Vector3(-9f, 1.65f, 1.05f), new Vector3(0.2f, 3.3f, 12.1f));
        Alterar(paredes, "Esquerda_Fundo", new Vector3(-9f, 1.65f, 8.65f), new Vector3(0.2f, 3.3f, 0.7f));
        Alterar(paredes, "Esquerda_Acima_Porta", new Vector3(-9f, 2.75f, 7.7f), new Vector3(0.2f, 1.1f, 1.2f));
        Alterar(paredes, "Direita_Principal", new Vector3(9f, 1.65f, 1.05f), new Vector3(0.2f, 3.3f, 12.1f));
        Alterar(paredes, "Direita_Fundo", new Vector3(9f, 1.65f, 8.65f), new Vector3(0.2f, 3.3f, 0.7f));
        Alterar(paredes, "Direita_Acima_Porta", new Vector3(9f, 2.75f, 7.7f), new Vector3(0.2f, 1.1f, 1.2f));
        Alterar(portas, "Porta_Esquerda_Fechada", new Vector3(-9f, 1.1f, 7.7f), new Vector3(0.1f, 2.2f, 1.18f));
        Alterar(portas, "Porta_Direita_Fechada", new Vector3(9f, 1.1f, 7.7f), new Vector3(0.1f, 2.2f, 1.18f));
        // Porta frontal (x=-3.9), janela (x=2.6) e suas caixas: intactas.

        // Bordinhas antigas sao substituidas por muros altos no grupo 06B.
        DesligarRendererEColisor(paredes, "Corredor_Borda_Fundo");
        DesligarRendererEColisor(paredes, "Corredor_Borda_Esquerda");
        DesligarRendererEColisor(paredes, "Corredor_Borda_Direita");
    }

    private static void Alterar(Transform grupo, string nome, Vector3 pos, Vector3 escala)
    {
        Transform t = grupo.Find(nome);
        Undo.RecordObject(t, "Ampliar Sausage Run 6B");
        t.localPosition = pos;
        t.localScale = escala;
        EditorUtility.SetDirty(t);
    }

    private static void DesligarRendererEColisor(Transform grupo, string nome)
    {
        Transform item = grupo.Find(nome);
        if (item == null) return;
        // Deixar o GameObject no projeto permite reaplicar e reverter com o backup.
        Undo.RecordObject(item.gameObject, "Substituir borda do corredor");
        item.gameObject.SetActive(false);
        EditorUtility.SetDirty(item.gameObject);
    }

    private static void CriarExpansao(Transform root)
    {
        Transform anterior = root.Find(GroupName);
        if (anterior != null) Undo.DestroyObjectImmediate(anterior.gameObject);

        Transform g = NovoGrupo(GroupName, root);
        Transform sala = NovoGrupo("01_Novos_Moveis_Sala", g);
        Transform muro = NovoGrupo("02_Muros_Altos_Corredor", g);
        Transform garagem = NovoGrupo("03_Grade_e_Escada_Garagem", g);
        Transform banheiro = NovoGrupo("04_Banheirinho", g);
        Transform lavanderia = NovoGrupo("05_Acesso_Lavanderia", g);
        Transform detalhes = NovoGrupo("06_Detalhes", g);

        Material muroTinta = Mat("6B_Muro_Externo", new Color(0.67f, 0.73f, 0.73f));
        Material chaoPiso = Mat("6B_Piso_Externo", new Color(0.48f, 0.55f, 0.58f));
        Material madeira = Mat("6B_Madeira_Clara", new Color(0.56f, 0.35f, 0.20f));
        Material madeiraEscura = Mat("6B_Madeira_Escura", new Color(0.27f, 0.21f, 0.17f));
        Material tecido = Mat("6B_Tecido_Terracota", new Color(0.68f, 0.36f, 0.27f));
        Material azul = Mat("6B_Tecido_Azul", new Color(0.24f, 0.38f, 0.52f));
        Material creme = Mat("6B_Creme", new Color(0.85f, 0.81f, 0.71f));
        Material preto = Mat("6B_Grade_Preto", new Color(0.15f, 0.18f, 0.20f));
        Material metal = Mat("6B_Metal", new Color(0.52f, 0.56f, 0.58f));
        Material verde = Mat("6B_Verdes", new Color(0.23f, 0.43f, 0.32f));
        Material tapete = Mat("6B_Tapete", new Color(0.45f, 0.29f, 0.36f));
        Material amarelado = Mat("6B_Caixas", new Color(0.70f, 0.49f, 0.25f));

        // Contraparede do corredor: 4,4 m. O acesso externo existe nas pontas X.
        Cubo("Muro_Externo_Oposto_A_Sala", muro, 0f, 2.2f, -9f, 18f, 4.4f, 0.22f, muroTinta);

        // Extremidade esquerda: abertura Z [-7.9,-6.1], fechada por uma grade.
        Cubo("Muro_Esquerda_Frente", muro, -9f, 2.2f, -8.45f, 0.22f, 4.4f, 1.1f, muroTinta);
        Cubo("Muro_Esquerda_Fundos", muro, -9f, 2.2f, -5.55f, 0.22f, 4.4f, 1.1f, muroTinta);
        Cubo("Muro_Esquerda_Acima_Grade", muro, -9f, 3.45f, -7f, 0.22f, 1.9f, 1.8f, muroTinta);

        // Extremidade direita: porta pequena do banheirinho e passagem da lavanderia.
        Cubo("Muro_Direita_Perto_Fundo", muro, 9f, 2.2f, -8.7f, 0.22f, 4.4f, 0.6f, muroTinta);
        Cubo("Muro_Direita_Entre_Acessos", muro, 9f, 2.2f, -7.15f, 0.22f, 4.4f, 0.3f, muroTinta);
        Cubo("Muro_Direita_Perto_Sala", muro, 9f, 2.2f, -5.15f, 0.22f, 4.4f, 0.3f, muroTinta);
        Cubo("Muro_Acima_Porta_Banheiro", muro, 9f, 3.35f, -7.9f, 0.22f, 2.1f, 1.0f, muroTinta);
        Cubo("Muro_Acima_Passagem_Lavanderia", muro, 9f, 3.5f, -6.1f, 0.22f, 1.8f, 1.6f, muroTinta);

        // Porta de grade para escada que desce a garagem (lado X negativo).
        Cubo("Grade_Barra_Lado_A", garagem, -9f, 1.25f, -7.87f, .20f, 2.5f, .08f, preto);
        Cubo("Grade_Barra_Lado_B", garagem, -9f, 1.25f, -6.13f, .20f, 2.5f, .08f, preto);
        Cubo("Grade_Trilho_Cima", garagem, -9f, 2.43f, -7f, .20f, .14f, 1.8f, preto);
        Cubo("Grade_Trilho_Baixo", garagem, -9f, .14f, -7f, .20f, .12f, 1.8f, preto);
        for (int i = 0; i <= 7; i++)
            Cubo("Grade_Barra_Vertical_" + i, garagem,
                -9f, 1.25f, -7.75f + i * (1.5f / 7f), .085f, 2.4f, .07f, preto);
        // Bloqueio transparente: evita que Scooby atravesse os espacos entre barras.
        Cubo("Grade_Colisao_Invisivel", garagem, -9f, 1.25f, -7f, .18f, 2.5f, 1.8f,
             preto, false);
        for (int i = 0; i < 6; i++)
        {
            float yTopo = -0.13f - i * .16f;
            Cubo("Degrau_Garagem_" + (i + 1), garagem,
                -9.37f - .53f * i, yTopo - .11f, -7f, .54f, .22f, 1.95f, chaoPiso);
        }
        Cubo("Patamar_Visivel_Garagem", garagem, -13.05f, -1.12f, -7f, 1.8f, .2f, 2.4f, chaoPiso);
        Cubo("Corrimao_Garagem_Lateral_01", garagem, -11f, .35f, -8.02f, 4.4f, .14f, .12f, metal);
        Cubo("Corrimao_Garagem_Lateral_02", garagem, -11f, .35f, -5.98f, 4.4f, .14f, .12f, metal);

        // Banheirinho externo cenografico, porta FECHADA no corredor.
        Cubo("Piso_Banheirinho", banheiro, 10.4f, -.1f, -8f, 2.8f, .2f, 2f, creme);
        Cubo("Banheiro_Parede_Fora", banheiro, 11.8f, 1.6f, -8f, .2f, 3.2f, 2f, muroTinta);
        Cubo("Banheiro_Parede_Fundo", banheiro, 10.4f, 1.6f, -9f, 2.8f, 3.2f, .2f, muroTinta);
        Cubo("Banheiro_Parede_Da_Lavanderia", banheiro, 10.4f, 1.6f, -7f, 2.8f, 3.2f, .2f, muroTinta);
        Cubo("Porta_Banheirinho_Fechada", banheiro, 9f, 1.13f, -7.9f, .16f, 2.26f, .98f, madeiraEscura);
        Cubo("Vaso_Sanitario", banheiro, 10.8f, .30f, -8.3f, .55f, .60f, .70f, creme);
        Cubo("Caixa_Acoplada", banheiro, 11.12f, .55f, -8.3f, .22f, .67f, .60f, creme);
        Cubo("Pia_Banheirinho", banheiro, 10.15f, .62f, -8.65f, .65f, .18f, .40f, creme);

        // Lavanderia visual do outro lado, com portao fechado por seguranca.
        Cubo("Piso_Lavanderia", lavanderia, 11f, -.1f, -6f, 4f, .2f, 2f, chaoPiso);
        Cubo("Lavanderia_Parede_Fora", lavanderia, 13f, 1.8f, -6f, .2f, 3.6f, 2f, muroTinta);
        Cubo("Lavanderia_Parede_Tras", lavanderia, 11f, 1.8f, -5f, 4f, 3.6f, .2f, muroTinta);
        Cubo("Lavanderia_Parede_Entre_Banheiro", lavanderia, 11f, 1.8f, -7f, 4f, 3.6f, .2f, muroTinta);
        Cubo("Portao_Lavanderia_Cima", lavanderia, 9f, 2.36f, -6.1f, .18f, .12f, 1.6f, preto);
        Cubo("Portao_Lavanderia_Baixo", lavanderia, 9f, .1f, -6.1f, .18f, .12f, 1.6f, preto);
        for (int i = 0; i <= 6; i++)
            Cubo("Portao_Lavanderia_Grade_" + i, lavanderia,
                9f, 1.2f, -6.83f + i * (.24f), .07f, 2.35f, .07f, preto);
        Cubo("Lavanderia_Bloqueio_Invisivel", lavanderia, 9f, 1.2f, -6.1f,
             .17f, 2.4f, 1.6f, preto, false);
        Cubo("Tanque_Lavanderia", lavanderia, 11.2f, .7f, -5.5f, 1.1f, .3f, .65f, creme);
        Cubo("Lavadora_Base", lavanderia, 12.25f, .48f, -6.5f, .85f, .95f, .85f, creme);
        Cubo("Lavadora_Painel", lavanderia, 12.25f, .98f, -6.23f, .85f, .12f, .28f, metal);

        // Novo sofa em L proximo ao fundo da sala.
        Cubo("Sofa_L_Assento_A", sala, -6.75f, .35f, 5.4f, 2.4f, .7f, 1.1f, tecido);
        Cubo("Sofa_L_Encosto_A", sala, -6.75f, .87f, 5.92f, 2.4f, 1.04f, .23f, tecido);
        Cubo("Sofa_L_Assento_B", sala, -7.60f, .35f, 6.30f, .85f, .7f, 2.0f, tecido);
        Cubo("Sofa_L_Encosto_B", sala, -8.08f, .84f, 6.32f, .22f, 1.0f, 2.0f, tecido);
        Cubo("Tapete_Sofa_L", sala, -6.2f, .012f, 5.0f, 3.5f, .024f, 2.4f, tapete, true, false);

        // Rack + TV (na parede do fundo, atras da zona central).
        Cubo("Rack_TV_Corpo", sala, .25f, .40f, 8.20f, 4.2f, .8f, .6f, madeiraEscura);
        Cubo("TV_Tela", sala, .25f, 1.45f, 8.57f, 2.7f, 1.36f, .12f, preto);
        Cubo("TV_Base", sala, .25f, .88f, 8.34f, .55f, .12f, .35f, metal, true, false);

        // Mesa de jantar, cadeiras e bancos: rota pela ala esquerda.
        Cubo("Jantar_Tampo", sala, -6.7f, .79f, -2.1f, 2.3f, .13f, 1.65f, madeira);
        Cubo("Jantar_Pe_A", sala, -7.55f, .36f, -2.70f, .16f, .72f, .16f, madeira);
        Cubo("Jantar_Pe_B", sala, -5.85f, .36f, -2.70f, .16f, .72f, .16f, madeira);
        Cubo("Jantar_Pe_C", sala, -7.55f, .36f, -1.50f, .16f, .72f, .16f, madeira);
        Cubo("Jantar_Pe_D", sala, -5.85f, .36f, -1.50f, .16f, .72f, .16f, madeira);
        CriarCadeira(sala, "Cadeira_01", -6.72f, -3.52f, madeira, azul);
        CriarCadeira(sala, "Cadeira_02", -6.72f, -.65f, madeira, azul);

        // Poltrona, aparador, mesa lateral e estante do lado direito.
        Cubo("Poltrona_Assento", sala, 6.8f, .38f, 5.2f, 1.3f, .76f, 1.2f, azul);
        Cubo("Poltrona_Encosto", sala, 6.8f, .95f, 5.72f, 1.3f, 1.1f, .22f, azul);
        Cubo("Poltrona_Braco_01", sala, 6.14f, .62f, 5.20f, .17f, .9f, 1.18f, azul);
        Cubo("Poltrona_Braco_02", sala, 7.46f, .62f, 5.20f, .17f, .9f, 1.18f, azul);
        Cubo("Aparador_Lateral", sala, 7.60f, .64f, -1.15f, 1.0f, 1.28f, 2.25f, madeiraEscura);
        Cubo("Mesa_Lateral_Tampo", sala, 5.8f, .62f, 6.55f, .85f, .15f, .85f, madeira);
        Cubo("Mesa_Lateral_Pe", sala, 5.8f, .29f, 6.55f, .15f, .58f, .15f, madeira);
        Cubo("Estante_Livros_Alta", sala, 7.75f, 1.35f, 1.60f, .85f, 2.7f, 1.8f, madeiraEscura);

        // Bancos criam esconderijos baixos. A parte de baixo fica livre.
        CriarBanco(sala, "Banco_Esconderijo_A", -3.5f, 6.2f, madeira);
        CriarBanco(sala, "Banco_Esconderijo_B", 3.15f, 6.25f, madeira);

        // Caixas escalonadas para desviar, se esconder e escalar mais tarde.
        Cubo("Caixa_Labirinto_Nova_01", sala, 5.9f, .43f, -3.35f, 1.0f, .86f, 1.1f, amarelado);
        Cubo("Caixa_Labirinto_Nova_02", sala, 7.25f, .31f, -2.25f, .9f, .62f, .85f, amarelado);
        Cubo("Caixa_Labirinto_Nova_03", sala, 5.6f, .50f, 2.65f, .9f, 1.0f, 1.1f, amarelado);
        Cubo("Caixa_Labirinto_Nova_04", sala, -6.25f, .48f, 2.9f, 1.05f, .96f, 1.1f, amarelado);
        Cubo("Caixa_Labirinto_Nova_05", sala, -7.6f, .31f, 1.45f, .95f, .62f, .9f, amarelado);
        Cubo("Caixa_Labirinto_Nova_06", sala, 1.85f, .30f, 7.0f, .9f, .6f, .9f, amarelado);

        // Plantas, vasos e detalhes leves (sem collider: nao prejudicam percepcao/IA).
        Cubo("Vaso_Grande_01", detalhes, -8.0f, .35f, -3.9f, .48f, .7f, .48f, creme, true, false);
        Cubo("Planta_Grande_01", detalhes, -8.0f, 1.07f, -3.9f, .70f, .85f, .70f, verde, true, false);
        Cubo("Vaso_Grande_02", detalhes, 8.0f, .35f, 7.55f, .48f, .7f, .48f, creme, true, false);
        Cubo("Planta_Grande_02", detalhes, 8.0f, 1.07f, 7.55f, .70f, .85f, .70f, verde, true, false);
        Cubo("Tapete_Central_Novo", detalhes, 0f, .012f, 5.0f, 3.4f, .024f, 2.1f, tapete, true, false);
        Cubo("Lixeira_Externa", detalhes, -6.6f, .42f, -8.3f, .58f, .84f, .58f, metal);
        Cubo("Balde_Externo", detalhes, 7.4f, .26f, -8.25f, .55f, .52f, .55f, azul);
        Cubo("Sapateira_Corredor", detalhes, -5.8f, .40f, -5.72f, 1.4f, .8f, .45f, madeiraEscura);
    }

    private static void CriarCadeira(Transform parent, string n, float x, float z, Material madeira, Material estofado)
    {
        Cubo(n + "_Assento", parent, x, .43f, z, .65f, .13f, .65f, estofado);
        Cubo(n + "_Encosto", parent, x, .77f, z + .28f, .65f, .7f, .12f, madeira);
        foreach (float dx in new[] { -.24f, .24f })
            foreach (float dz in new[] { -.24f, .24f })
                Cubo(n + "_Pe_" + dx + "_" + dz, parent, x + dx, .20f, z + dz,
                     .09f, .4f, .09f, madeira);
    }

    private static void CriarBanco(Transform parent, string n, float x, float z, Material madeira)
    {
        Cubo(n + "_Tampo", parent, x, .70f, z, 1.65f, .14f, .85f, madeira);
        foreach (float dx in new[] { -.68f, .68f })
            foreach (float dz in new[] { -.28f, .28f })
                Cubo(n + "_Pe_" + dx + "_" + dz, parent, x + dx, .32f, z + dz,
                     .13f, .64f, .13f, madeira);
    }

    private static void AtualizarJogo(SausageRunGameManager jogo, MariaAI ia)
    {
        Undo.RecordObject(jogo, "Expandir limites de vitoria");
        jogo.zMaximoParaVitoria = -5.4f; // preservar seguranca proxima da janela
        jogo.zMinimoDoCorredor = -9.0f;
        jogo.xMaximoCorredor = 8.8f;
        EditorUtility.SetDirty(jogo);

        Undo.RecordObject(ia, "Expandir patrulha da Maria");
        ia.limiteLateralX = 8.6f;
        ia.limiteFundoDaSalaZ = 8.7f;
        ia.limiteFrontalDaSalaZ = -4.7f;
        ia.margemParaDestino = .45f;
        ia.pontosPatrulha = new Vector3[]
        {
            new Vector3(-4.8f, 0f, -3.4f),
            new Vector3(-3.6f, 0f, 3.55f),
            new Vector3(-4.55f, 0f, 7.65f),
            new Vector3(3.95f, 0f, 7.65f),
            new Vector3(4.35f, 0f, 4.55f),
            new Vector3(6.15f, 0f, -1.7f),
            new Vector3(3.7f, 0f, -3.4f)
        };
        EditorUtility.SetDirty(ia);
    }

    private static Transform NovoGrupo(string nome, Transform parent)
    {
        GameObject g = new GameObject(nome);
        g.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(g, "Criar grupo de expansao 6B");
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
        if (r != null) { r.sharedMaterial = material; r.enabled = visivel; }
        BoxCollider c = g.GetComponent<BoxCollider>();
        if (c != null) c.enabled = colidir;
        // Registrar a criacao de cada elemento permite desfazer alteracoes pelo Editor.
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
