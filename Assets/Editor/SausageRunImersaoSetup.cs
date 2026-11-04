using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Instala apenas recursos visuais opcionais na camera existente.
/// Nao recria a sala, nao substitui scripts e nao toca na NavMesh.
/// </summary>
public static class SausageRunImersaoSetup
{
    private const string PastaMateriais = "Assets/_SausageRun3D/Materials";

    [MenuItem("Sausage Run/5 - Aplicar imersao do Scooby")]
    public static void AplicarImersao()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sausage Run 3D",
                "Saia do modo Play antes de configurar a camera.", "OK");
            return;
        }

        GameObject scooby = GameObject.Find("Scooby");
        Camera cameraScooby = scooby != null ? scooby.GetComponentInChildren<Camera>(true) : null;
        if (cameraScooby == null)
        {
            EditorUtility.DisplayDialog("Sausage Run 3D",
                "Nao encontrei a CameraScooby dentro do objeto Scooby.", "OK");
            return;
        }

        if (cameraScooby.GetComponent<ScoobyCameraImersao>() == null)
            Undo.AddComponent<ScoobyCameraImersao>(cameraScooby.gameObject);

        // Nao duplique o focinho caso o usuario execute o menu novamente.
        Transform focinhoExistente = cameraScooby.transform.Find("FocinhoScooby_Provisorio");
        if (focinhoExistente == null)
        {
            GarantirPasta("Assets", "_SausageRun3D");
            GarantirPasta("Assets/_SausageRun3D", "Materials");
            Material pelo = CriarMaterial("Focinho_Pelo", new Color(0.56f, 0.31f, 0.17f));
            Material focinho = CriarMaterial("Focinho_Claro", new Color(0.69f, 0.44f, 0.27f));
            Material nariz = CriarMaterial("Focinho_Nariz", new Color(0.075f, 0.058f, 0.052f));

            GameObject raiz = new GameObject("FocinhoScooby_Provisorio");
            Undo.RegisterCreatedObjectUndo(raiz, "Criar focinho provisório");
            raiz.transform.SetParent(cameraScooby.transform, false);
            raiz.transform.localPosition = Vector3.zero;
            raiz.transform.localRotation = Quaternion.identity;

            Esfera("Bochecha_Esquerda", raiz.transform,
                new Vector3(-0.115f, -0.235f, 0.49f), new Vector3(0.24f, 0.15f, 0.29f), focinho);
            Esfera("Bochecha_Direita", raiz.transform,
                new Vector3(0.115f, -0.235f, 0.49f), new Vector3(0.24f, 0.15f, 0.29f), focinho);
            Esfera("Nariz", raiz.transform,
                new Vector3(0f, -0.183f, 0.67f), new Vector3(0.13f, 0.09f, 0.12f), nariz);
            Esfera("Orelha_Esquerda", raiz.transform,
                new Vector3(-0.385f, -0.095f, 0.44f), new Vector3(0.15f, 0.27f, 0.16f), pelo);
            Esfera("Orelha_Direita", raiz.transform,
                new Vector3(0.385f, -0.095f, 0.44f), new Vector3(0.15f, 0.27f, 0.16f), pelo);
        }

        EditorUtility.SetDirty(cameraScooby.gameObject);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(cameraScooby.gameObject.scene);
        EditorUtility.DisplayDialog("Sausage Run 3D",
            "Imersao instalada na CameraScooby!\n\n" +
            "Play: movimente-se com WASD e Shift.\n" +
            "O focinho e provisório e nao possui colisores.\n" +
            "Para ocultar: desative FocinhoScooby_Provisorio.", "OK");
    }

    private static void Esfera(string nome, Transform pai, Vector3 posicao, Vector3 escala, Material material)
    {
        GameObject objeto = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        objeto.name = nome;
        Undo.RegisterCreatedObjectUndo(objeto, "Criar " + nome);
        objeto.transform.SetParent(pai, false);
        objeto.transform.localPosition = posicao;
        objeto.transform.localRotation = Quaternion.identity;
        objeto.transform.localScale = escala;
        Collider collider = objeto.GetComponent<Collider>();
        if (collider != null) Undo.DestroyObjectImmediate(collider);
        Renderer renderizador = objeto.GetComponent<Renderer>();
        if (renderizador != null) renderizador.sharedMaterial = material;
    }

    private static void GarantirPasta(string pai, string nome)
    {
        if (!AssetDatabase.IsValidFolder(pai + "/" + nome))
            AssetDatabase.CreateFolder(pai, nome);
    }

    private static Material CriarMaterial(string nome, Color cor)
    {
        string caminho = PastaMateriais + "/" + nome + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            material.name = nome;
            AssetDatabase.CreateAsset(material, caminho);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", cor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", cor);
        EditorUtility.SetDirty(material);
        return material;
    }
}
