using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Este arquivo deve ficar em Assets/Editor. Exige o pacote AI Navigation.
public static class SausageRunMariaSetup
{
    [MenuItem("Sausage Run/2 - Configurar Maria IA")]
    public static void ConfigurarMaria()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sausage Run", "Pare o Play antes de configurar a Maria.", "OK");
            return;
        }

        GameObject scooby = GameObject.Find("Scooby");
        GameObject marcador = GameObject.Find("Maria_Marcador");
        GameObject maria = GameObject.Find("Maria");

        if (scooby == null || (marcador == null && maria == null))
        {
            EditorUtility.DisplayDialog("Sausage Run",
                "Nao encontrei Scooby e/ou Maria_Marcador. Abra a cena Sala e confira a Hierarchy.", "OK");
            return;
        }

        if (maria == null)
        {
            Vector3 posMarcador = marcador.transform.position;
            maria = new GameObject("Maria");
            Undo.RegisterCreatedObjectUndo(maria, "Criar Maria");
            maria.transform.position = new Vector3(posMarcador.x, 0f, posMarcador.z);
        }

        if (marcador != null)
        {
            if (marcador.transform.parent != maria.transform)
                Undo.SetTransformParent(marcador.transform, maria.transform,
                    "Adicionar visual da Maria");

            Renderer visual = marcador.GetComponent<Renderer>();
            float meioTamanho = visual != null ? visual.bounds.extents.y : 0.75f;
            marcador.transform.localPosition = new Vector3(0f, meioTamanho, 0f);
            marcador.transform.localRotation = Quaternion.identity;

            // O collider da capsula visual nao deve furar a NavMesh durante o Bake.
            Collider colliderMarcador = marcador.GetComponent<Collider>();
            if (colliderMarcador != null) colliderMarcador.enabled = false;
        }

        NavMeshAgent agente = maria.GetComponent<NavMeshAgent>();
        if (agente == null) agente = Undo.AddComponent<NavMeshAgent>(maria);
        agente.radius = 0.35f;
        agente.height = 1.6f;
        agente.baseOffset = 0f;
        agente.speed = 1.5f;
        agente.acceleration = 6f;
        agente.angularSpeed = 420f;
        agente.stoppingDistance = 0.1f;
        agente.autoBraking = true;

        MariaAI ia = maria.GetComponent<MariaAI>();
        if (ia == null) ia = Undo.AddComponent<MariaAI>(maria);
        ia.scooby = scooby.transform;

        GameObject superficieGO = GameObject.Find("Sala_NavMesh");
        if (superficieGO == null)
        {
            superficieGO = new GameObject("Sala_NavMesh");
            Undo.RegisterCreatedObjectUndo(superficieGO, "Criar Sala NavMesh");
        }

        NavMeshSurface superficie = superficieGO.GetComponent<NavMeshSurface>();
        if (superficie == null) superficie = Undo.AddComponent<NavMeshSurface>(superficieGO);
        superficie.collectObjects = CollectObjects.Volume;
        superficie.center = new Vector3(0f, 1.5f, 0f);
        superficie.size = new Vector3(12f, 3.4f, 10f);
        superficie.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        superficie.agentTypeID = agente.agentTypeID;

        EditorSceneManager.MarkSceneDirty(maria.scene);
        EditorSceneManager.SaveScene(maria.scene);

        try
        {
            superficie.BuildNavMesh();
            EditorSceneManager.SaveScene(maria.scene);
        }
        catch (System.Exception erro)
        {
            Debug.LogError("Nao foi possivel gerar a NavMesh automaticamente: " + erro.Message);
            EditorUtility.DisplayDialog("Sausage Run",
                "Maria configurada, mas a NavMesh nao foi gerada. " +
                "Selecione Sala_NavMesh e clique Bake no Inspector.", "OK");
        }

        Selection.activeGameObject = maria;
        EditorGUIUtility.PingObject(maria);
        Debug.Log("Sausage Run: Maria AI e superficie de navegacao configuradas. " +
            "Pressione Play para testar.");
    }

    [MenuItem("Sausage Run/3 - Regerar NavMesh da Sala")]
    public static void RegerarNavMesh()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sausage Run", "Pare o Play antes de gerar a NavMesh.", "OK");
            return;
        }

        GameObject navGO = GameObject.Find("Sala_NavMesh");
        NavMeshSurface superficie = navGO != null ? navGO.GetComponent<NavMeshSurface>() : null;
        if (superficie == null)
        {
            EditorUtility.DisplayDialog("Sausage Run",
                "Execute primeiro Sausage Run > 2 - Configurar Maria IA.", "OK");
            return;
        }

        superficie.BuildNavMesh();
        EditorSceneManager.SaveScene(superficie.gameObject.scene);
        Debug.Log("Sausage Run: NavMesh da sala regenerada.");
    }
}
