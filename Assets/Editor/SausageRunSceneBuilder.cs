using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Gera o primeiro prototipo da sala do Sausage Run 3D.
/// Menu: Sausage Run > 1 - Criar cenario inicial
/// Este script e apenas para o Editor (colocar em Assets/Editor).
/// </summary>
public static class SausageRunSceneBuilder
{
    private const string RootName = "SausageRun_Prototipo";
    private const string BasePath = "Assets/_SausageRun3D";
    private const string MaterialPath = BasePath + "/Materials";

    [MenuItem("Sausage Run/1 - Criar cenario inicial")]
    public static void BuildScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sausage Run 3D", "Saia do modo Play antes de construir o cenario.", "OK");
            return;
        }

        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            bool recreate = EditorUtility.DisplayDialog(
                "Recriar cenario?",
                "Ja existe um cenario gerado. Recriar apaga somente os objetos dentro de '" + RootName + "'.",
                "Recriar", "Cancelar");
            if (!recreate) return;
            Object.DestroyImmediate(existing);
        }

        EnsureFolder("Assets", "_SausageRun3D");
        EnsureFolder(BasePath, "Scenes");
        EnsureFolder(BasePath, "Scripts");
        EnsureFolder(BasePath, "Prefabs");
        EnsureFolder(BasePath, "Models");
        EnsureFolder(BasePath, "Materials");
        EnsureFolder(BasePath, "Animations");
        EnsureFolder(BasePath, "Audio");
        EnsureFolder(BasePath, "UI");

        Material wall = MakeMaterial("Parede_CinzaClaro", new Color(0.84f, 0.86f, 0.88f));
        Material floor = MakeMaterial("Piso_Sala", new Color(0.68f, 0.59f, 0.47f));
        Material corridor = MakeMaterial("Piso_Corredor", new Color(0.55f, 0.64f, 0.66f));
        Material door = MakeMaterial("Portas_Fechadas", new Color(0.43f, 0.29f, 0.19f));
        Material boxes = MakeMaterial("Caixas", new Color(0.70f, 0.47f, 0.23f));
        Material sofa = MakeMaterial("Sofa_Verde", new Color(0.17f, 0.39f, 0.34f));
        Material table = MakeMaterial("Mesa_Madeira", new Color(0.42f, 0.26f, 0.16f));
        Material shelf = MakeMaterial("Estante", new Color(0.33f, 0.26f, 0.22f));
        Material rug = MakeMaterial("Tapete", new Color(0.46f, 0.31f, 0.50f));
        Material scooby = MakeMaterial("Scooby_Marcador", new Color(0.25f, 0.60f, 0.94f));
        Material maria = MakeMaterial("Maria_Marcador", new Color(0.93f, 0.30f, 0.40f));
        Material ball = MakeMaterial("Bolinha_Amarela", new Color(1.00f, 0.79f, 0.12f));

        GameObject root = new GameObject(RootName);
        Transform floorGroup = NewGroup("01_Pisos", root.transform);
        Transform walls = NewGroup("02_Paredes", root.transform);
        Transform doors = NewGroup("03_Portas_fechadas", root.transform);
        Transform furniture = NewGroup("04_Moveis_e_obstaculos", root.transform);
        Transform items = NewGroup("05_Scooby_Maria_Bolinha", root.transform);

        // Coordenadas: sala X [-6, 6], Z [-5, 5]; corredor Z [-8, -5].
        Cube("Chao_Sala", floorGroup, new Vector3(0f, -0.1f, 0f), new Vector3(12f, 0.2f, 10f), floor);
        Cube("Chao_Corredor", floorGroup, new Vector3(0f, -0.1f, -6.5f), new Vector3(12f, 0.2f, 3f), corridor);

        // Parede do fundo (Z = +5).
        Cube("Fundo", walls, new Vector3(0f, 1.5f, 5f), new Vector3(12f, 3f, 0.2f), wall);

        // Frente (Z = -5): porta a esquerda, janela a direita.
        // Porta: X [-4.6, -3.2], abertura Y [0, 2.2].
        // Janela: X [1.7, 3.5], abertura Y [0.85, 2.1].
        Cube("Frente_Esquerda", walls, new Vector3(-5.3f, 1.5f, -5f), new Vector3(1.4f, 3f, 0.2f), wall);
        Cube("Frente_Entre_Porta_Janela", walls, new Vector3(-0.75f, 1.5f, -5f), new Vector3(4.9f, 3f, 0.2f), wall);
        Cube("Frente_Direita", walls, new Vector3(4.75f, 1.5f, -5f), new Vector3(2.5f, 3f, 0.2f), wall);
        Cube("Frente_Acima_Porta", walls, new Vector3(-3.9f, 2.6f, -5f), new Vector3(1.4f, 0.8f, 0.2f), wall);
        Cube("Frente_Abaixo_Janela", walls, new Vector3(2.6f, 0.425f, -5f), new Vector3(1.8f, 0.85f, 0.2f), wall);
        Cube("Frente_Acima_Janela", walls, new Vector3(2.6f, 2.55f, -5f), new Vector3(1.8f, 0.9f, 0.2f), wall);

        // Portas laterais nas pontas superiores: Z [3.2, 4.4].
        Cube("Esquerda_Principal", walls, new Vector3(-6f, 1.5f, -0.9f), new Vector3(0.2f, 3f, 8.2f), wall);
        Cube("Esquerda_Fundo", walls, new Vector3(-6f, 1.5f, 4.7f), new Vector3(0.2f, 3f, 0.6f), wall);
        Cube("Esquerda_Acima_Porta", walls, new Vector3(-6f, 2.6f, 3.8f), new Vector3(0.2f, 0.8f, 1.2f), wall);
        Cube("Direita_Principal", walls, new Vector3(6f, 1.5f, -0.9f), new Vector3(0.2f, 3f, 8.2f), wall);
        Cube("Direita_Fundo", walls, new Vector3(6f, 1.5f, 4.7f), new Vector3(0.2f, 3f, 0.6f), wall);
        Cube("Direita_Acima_Porta", walls, new Vector3(6f, 2.6f, 3.8f), new Vector3(0.2f, 0.8f, 1.2f), wall);

        // As tres portas ficam fechadas: so a janela permite acesso ao corredor.
        Cube("Porta_Frontal_Fechada", doors, new Vector3(-3.9f, 1.1f, -5f), new Vector3(1.38f, 2.2f, 0.1f), door);
        Cube("Porta_Esquerda_Fechada", doors, new Vector3(-6f, 1.1f, 3.8f), new Vector3(0.1f, 2.2f, 1.18f), door);
        Cube("Porta_Direita_Fechada", doors, new Vector3(6f, 1.1f, 3.8f), new Vector3(0.1f, 2.2f, 1.18f), door);

        // Parapeitos simples para definir o corredor exterior.
        Cube("Corredor_Borda_Fundo", walls, new Vector3(0f, 0.3f, -8f), new Vector3(12f, 0.6f, 0.2f), corridor);
        Cube("Corredor_Borda_Esquerda", walls, new Vector3(-6f, 0.3f, -6.5f), new Vector3(0.2f, 0.6f, 3f), corridor);
        Cube("Corredor_Borda_Direita", walls, new Vector3(6f, 0.3f, -6.5f), new Vector3(0.2f, 0.6f, 3f), corridor);

        // Plataformas de apoio para o salto pela janela (X = 2.6).
        Cube("Caixa_Externa_Janela", furniture, new Vector3(2.6f, 0.25f, -5.85f), new Vector3(0.9f, 0.5f, 0.9f), boxes);
        Cube("Caixa_Interna_Janela", furniture, new Vector3(2.6f, 0.25f, -4.1f), new Vector3(0.9f, 0.5f, 0.9f), boxes);

        // Sofa: bloqueia a visao e cria rota de desvio.
        Cube("Sofa_Base", furniture, new Vector3(-3.7f, 0.32f, 1.5f), new Vector3(2.8f, 0.64f, 1.15f), sofa);
        Cube("Sofa_Encosto", furniture, new Vector3(-3.7f, 0.86f, 2.0f), new Vector3(2.8f, 1.08f, 0.22f), sofa);
        Cube("Sofa_Braco_Esquerdo", furniture, new Vector3(-5.0f, 0.57f, 1.5f), new Vector3(0.2f, 1.14f, 1.15f), sofa);
        Cube("Sofa_Braco_Direito", furniture, new Vector3(-2.4f, 0.57f, 1.5f), new Vector3(0.2f, 1.14f, 1.15f), sofa);

        // Mesa: espaco livre sob o tampo para Scooby se esconder.
        float tableX = -0.35f, tableZ = -0.3f;
        Cube("Mesa_Tampo", furniture, new Vector3(tableX, 0.75f, tableZ), new Vector3(2.1f, 0.15f, 1.4f), table);
        float[] legX = { -1.25f, 0.55f };
        float[] legZ = { -0.85f, 0.25f };
        for (int x = 0; x < legX.Length; x++)
            for (int z = 0; z < legZ.Length; z++)
                Cube("Mesa_Pe_" + x + "_" + z, furniture,
                    new Vector3(legX[x], 0.3375f, legZ[z]),
                    new Vector3(0.13f, 0.675f, 0.13f), table);

        Cube("Estante", furniture, new Vector3(4.75f, 1.25f, 1.1f), new Vector3(1.2f, 2.5f, 2.4f), shelf);
        Cube("Caixa_Labirinto_01", furniture, new Vector3(1.35f, 0.45f, -1.35f), new Vector3(1.0f, 0.9f, 1.0f), boxes);
        Cube("Caixa_Labirinto_02", furniture, new Vector3(2.6f, 0.35f, -0.45f), new Vector3(0.9f, 0.7f, 0.9f), boxes);
        Cube("Caixa_Labirinto_03", furniture, new Vector3(2.0f, 0.5f, 1.1f), new Vector3(1.2f, 1f, 0.9f), boxes);
        Cube("Caixa_Labirinto_04", furniture, new Vector3(-2.1f, 0.4f, -2.6f), new Vector3(0.8f, 0.8f, 1.1f), boxes);

        GameObject carpet = Cube("Tapete_Decorativo", furniture,
            new Vector3(-0.35f, 0.012f, -0.3f), new Vector3(3.4f, 0.024f, 2.3f), rug);
        carpet.GetComponent<BoxCollider>().enabled = false;

        // Marcadores temporarios: ainda nao possuem scripts de movimentacao/IA.
        Primitive("Scooby_Marcador_Inicio", PrimitiveType.Capsule, items,
            new Vector3(2.6f, 0.3f, -6.7f), new Vector3(0.5f, 0.3f, 0.65f), scooby);
        Primitive("Maria_Marcador", PrimitiveType.Capsule, items,
            new Vector3(-0.6f, 0.9f, 3.0f), new Vector3(0.75f, 0.9f, 0.75f), maria);
        Primitive("Bolinha", PrimitiveType.Sphere, items,
            new Vector3(3.35f, 0.2f, 3.5f), new Vector3(0.4f, 0.4f, 0.4f), ball);

        // Camera provisoria para visualizar toda a planta. Mais tarde sera FPS.
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cam = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(10f, 15f, -17f);
        cam.transform.LookAt(new Vector3(0f, 0.4f, -0.5f));
        cam.orthographic = false;
        cam.fieldOfView = 58f;
        cam.farClipPlane = 100f;

        Light sun = Object.FindFirstObjectByType<Light>();
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            sun.intensity = 1.5f;
        }

        Selection.activeGameObject = root;
        AssetDatabase.SaveAssets();
        Scene scene = SceneManager.GetActiveScene();
        string path = BasePath + "/Scenes/Sala.unity";
        bool saved = EditorSceneManager.SaveScene(scene, path, false);
        if (saved)
        {
            Debug.Log("Sausage Run 3D: cenario criado e cena salva em " + path);
            EditorUtility.DisplayDialog("Sausage Run 3D",
                "Cenario inicial criado!\n\nAbra a aba Scene ou Game para ver a sala.\nA cena foi salva em " + path +
                "\n\nScooby, Maria e bolinha ainda sao marcadores. A jogabilidade vira na proxima etapa.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Sausage Run 3D",
                "Cenario criado, mas nao foi possivel salvar automaticamente. Use File > Save As para salvar a cena como Sala.", "OK");
        }
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static Transform NewGroup(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        return Primitive(name, PrimitiveType.Cube, parent, position, scale, material);
    }

    private static GameObject Primitive(string name, PrimitiveType kind, Transform parent,
        Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(kind);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;
        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = material;
        return go;
    }

    private static Material MakeMaterial(string name, Color color)
    {
        string path = MaterialPath + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            material.name = name;
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        EditorUtility.SetDirty(material);
        return material;
    }
}
