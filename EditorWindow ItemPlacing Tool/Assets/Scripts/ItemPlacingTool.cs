using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ItemPlacingTool : EditorWindow
{
    // Creo dos listas donde guardo las referencias a los prefabs y los nombres de los prefabs (para mostrarlo en el Popup)
    private List<GameObject> prefabList = new List<GameObject>();
    private string[] prefabNames;

    // Indice del prefab seleccionado
    private int selectedIndex = 0;

    public GameObject selectedPrefab;

    private bool placingMode = false;

    private Vector3 placingHeight = new Vector3(0, 5, 0), extraHeight = new Vector3(0, 0.5f, 0);

    private bool isSpinning = false;
    private GameObject currentSpinningObject = null;

    private bool isDragging = false;
    private GameObject currentObject = null;

    [MenuItem("Tools/ItemPlacing Tool")]
    public static void OpenTool()
    {
        ItemPlacingTool ipt = GetWindow<ItemPlacingTool>();
        ipt.titleContent = new GUIContent("ItemPlacingTool");
        ipt.LoadPrefabs();
    }

    private void OnGUI()
    {
        if (prefabList.Count > 0)
        {
            // El popup muestra todos los items de una lista
            // Esto permite cambiar el index segun el prefab que elija. Pillando la referencia del mismo con actualPrefab.
            selectedIndex = EditorGUILayout.Popup("Prefab", selectedIndex, prefabNames);
            selectedPrefab = prefabList[selectedIndex];
        }
        else
        {
            EditorGUILayout.LabelField("No prefabs found in Assets/Prefabs");
        }

        if (GUILayout.Button("Test"))
        {
            Debug.Log($"Seleccionado: {selectedPrefab?.name ?? "Ninguno"}");
        }

        placingMode = EditorGUILayout.Toggle("Modo Colocar Prefab", placingMode);
        if (placingMode)
        {
            SceneView.duringSceneGui -= OnSceneGUI; // Para asegurar que no se duplique.
            SceneView.duringSceneGui += OnSceneGUI;
        }
        else
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        GUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField(
            "Click izquierdo para colocar prefab.\n" +
            "Arrastra click izquierdo para mover prefab.\n" +
            "Una vez soltado el click...\n" +
            "Click derecho para rotar.\n" +
            "Click izquierdo para terminar la rotación.",
            EditorStyles.wordWrappedLabel
        );
        GUILayout.EndVertical();

    }

    /// <summary>
    /// Carga los prefabs que se encuentren en Assets/Prefabs
    /// </summary>
    private void LoadPrefabs()
    {
        // Con esto aseguro que los objetos encontrados con el FindAssets sean de tipo Prefab.
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                prefabList.Add(prefab);
            }
        }
        // Lo mismo que lo de arriba pero mas simple ya que se cuantos nombres hay (prefabList.Count).
        prefabNames = new string[prefabList.Count];
        for (int i = 0; i < prefabList.Count; i++)
        {
            prefabNames[i] = prefabList[i].name;
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!placingMode) return;

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (!isDragging && !isSpinning)
        {
            TryBeginDrag(e, ray);
        }
        else if (isDragging)
        {
            ContinueDragOrEnd(e, ray);
        }
        else if (isSpinning)
        {
            HandleSpinning(e);
        }
    }

    private void TryBeginDrag(Event e, Ray ray)
    {
        if (e.type == EventType.MouseDown && e.button == 0 && selectedPrefab != null)
        {
            if (GetGroundHit(ray, out RaycastHit hitInfo))
            {
                currentObject = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                Undo.RegisterCreatedObjectUndo(currentObject, "Place Prefab");
                currentObject.transform.position = hitInfo.point + extraHeight;
                isDragging = true;
                e.Use();
            }
        }
    }
    private void ContinueDragOrEnd(Event e, Ray ray)
    {
        // Mientras esta en Drag se mueve el objeto.
        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            if (GetGroundHit(ray, out RaycastHit hitInfo))
            {
                currentObject.transform.position = hitInfo.point + extraHeight;
                e.Use();
            }
        }
        // Levanta el raton para empezar el spinning.
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            isDragging = false;
            isSpinning = true;
            e.Use();
        }
    }
    private void HandleSpinning(Event e)
    {
        // Rotar con el click derecho.
        if (e.type == EventType.MouseDrag && e.button == 1)
        {
            float speed = 1f;
            currentObject.transform.Rotate(Vector3.up, e.delta.x * speed, Space.World);
            e.Use();
        }
        // Para terminar la rotación pulsar click izquierdo.
        else if (e.type == EventType.MouseDown && e.button == 0)
        {
            isSpinning = false;
            currentObject = null;
            e.Use();
        }
    }
    private bool GetGroundHit(Ray ray, out RaycastHit outHit)
    {
        var hits = Physics.RaycastAll(ray);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // Hace que el ray ignore el prefab.
            if (currentObject != null &&
                hit.collider.transform.IsChildOf(currentObject.transform))
                continue;

            outHit = hit;
            return true;
        }

        outHit = default;
        return false;
    }

}
