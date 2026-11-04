using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Ajusta a hierarquia da camera de modo nao destrutivo. Pode executar 2+ vezes.
/// A camera real permanece como filha do Scooby, agora por meio de um pivot.
/// </summary>
public static class SausageRunCamera6ASetup
{
    [MenuItem("Sausage Run/6A - Melhorar caminhada do Scooby")]
    public static void Instalar()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sausage Run 3D",
                "Pare o modo Play antes de executar esta configuracao.", "OK");
            return;
        }

        GameObject scooby = GameObject.Find("Scooby");
        if (scooby == null)
        {
            EditorUtility.DisplayDialog("Sausage Run 3D",
                "Objeto 'Scooby' nao encontrado na cena aberta.", "OK");
            return;
        }

        Camera camera = scooby.GetComponentInChildren<Camera>(true);
        if (camera == null)
        {
            EditorUtility.DisplayDialog("Sausage Run 3D",
                "CameraScooby nao encontrada dentro do objeto Scooby.", "OK");
            return;
        }

        if (scooby.GetComponent<CharacterController>() == null ||
            scooby.GetComponent<ScoobyController>() == null)
        {
            EditorUtility.DisplayDialog("Sausage Run 3D",
                "Scooby precisa dos componentes Character Controller e Scooby Controller.", "OK");
            return;
        }

        Transform pivot = scooby.transform.Find("CameraFX_Pivot");
        if (pivot == null)
        {
            if (camera.transform.parent != scooby.transform)
            {
                EditorUtility.DisplayDialog("Sausage Run 3D",
                    "CameraScooby deve ser filha direta de Scooby antes da primeira instalacao. " +
                    "A configuracao foi cancelada para nao perder sua posicao.", "OK");
                return;
            }

            GameObject novoPivot = new GameObject("CameraFX_Pivot");
            Undo.RegisterCreatedObjectUndo(novoPivot, "Criar pivot da camera");
            Undo.SetTransformParent(novoPivot.transform, scooby.transform, "Organizar camera");
            novoPivot.transform.localPosition = camera.transform.localPosition;
            novoPivot.transform.localRotation = Quaternion.identity;
            novoPivot.transform.localScale = Vector3.one;
            pivot = novoPivot.transform;
        }

        if (camera.transform.parent != pivot)
        {
            if (camera.transform.parent != scooby.transform)
            {
                EditorUtility.DisplayDialog("Sausage Run 3D",
                    "A CameraScooby tem um pai inesperado. Nenhum componente foi alterado.", "OK");
                return;
            }

            Undo.SetTransformParent(camera.transform, pivot, "Mover camera para pivot");
            Undo.RecordObject(camera.transform, "Centralizar camera no pivot");
            camera.transform.localPosition = Vector3.zero;
        }

        // A Etapa 6 anterior altera a posicao local da camera.
        // Desativamos somente aquele efeito antigo, preservando o focinho.
        foreach (MonoBehaviour componente in camera.GetComponents<MonoBehaviour>())
        {
            if (componente != null && componente.GetType().Name == "ScoobyCameraImersao" &&
                componente.enabled)
            {
                Undo.RecordObject(componente, "Desativar balanco antigo");
                componente.enabled = false;
                EditorUtility.SetDirty(componente);
            }
        }

        ScoobyCameraEffects efeito = pivot.GetComponent<ScoobyCameraEffects>();
        if (efeito == null)
            efeito = Undo.AddComponent<ScoobyCameraEffects>(pivot.gameObject);

        Undo.RecordObject(efeito, "Aplicar configuracao natural de camera");
        efeito.AplicarPresetNatural();
        efeito.enabled = true;
        EditorUtility.SetDirty(efeito);

        Undo.RecordObject(camera, "Ajustar FOV inicial");
        camera.fieldOfView = 75f;
        EditorUtility.SetDirty(camera);
        EditorSceneManager.MarkSceneDirty(scooby.scene);

        Selection.activeGameObject = pivot.gameObject;
        EditorUtility.DisplayDialog("Sausage Run 3D - Etapa 6A",
            "Caminhada melhorada!\n\n" +
            "CameraScooby agora fica sob CameraFX_Pivot.\n" +
            "O balanco antigo foi desativado (nao foi excluido).\n" +
            "O focinho, o ScoobyController e a escalada foram preservados.\n\n" +
            "Salve a cena (Ctrl+S), pressione Play e teste WASD / Shift / Espaco.",
            "OK");
    }
}
