//using System.Collections.Generic;
//using System.Linq;
//using UnityEditor;
//using UnityEngine;
//using UnityEngine.ProBuilder;
//using UnityEngine.ProBuilder.MeshOperations;

//public class ProBuilderCombiner : Editor
//{
//    // 정점 병합 거리 임계값 (예: 0.01 = 1cm 이내의 정점을 하나로 합침)
//    private const float WELD_THRESHOLD = 0.001f;

//    [MenuItem("Tools/ProBuilder/Selected Meshes Combine & Weld")]
//    private static void CombineAndWeldSelectedProBuilderObjects()
//    {
//        // 1. 선택된 GameObjects 가져오기
//        GameObject[] selectedObjects = Selection.gameObjects;

//        if (selectedObjects.Length < 2)
//        {
//            EditorUtility.DisplayDialog("경고", "병합할 ProBuilder 오브젝트를 2개 이상 선택해주세요.", "확인");
//            return;
//        }

//        // 2. ProBuilderMesh 컴포넌트가 있는 대상 수집
//        List<ProBuilderMesh> pbMeshes = new List<ProBuilderMesh>();
//        foreach (GameObject go in selectedObjects)
//        {
//            ProBuilderMesh pb = go.GetComponent<ProBuilderMesh>();
//            if (pb != null)
//            {
//                pbMeshes.Add(pb);
//            }
//        }

//        if (pbMeshes.Count < 2)
//        {
//            EditorUtility.DisplayDialog("경고", "선택한 오브젝트 중 ProBuilder 메시가 2개 이상 필요합니다.", "확인");
//            return;
//        }

//        // 3. Undo(되돌리기) 등록
//        Undo.RegisterCompleteObjectUndo(selectedObjects, "Combine and Weld ProBuilder Meshes");

//        // 4. ProBuilder 메시 병합 실행
//        ProBuilderMesh combinedMesh = CombineMeshes.Combine(pbMeshes);

//        if (combinedMesh != null)
//        {
//            // 5. 정점(Vertex) 자동 Weld(합치기) 처리
//            // 근접한 위치의 정점들을 찾아 그룹화
//            int[] allIndexes = Enumerable.Range(0, combinedMesh.vertexCount).ToArray();
//            List<int> weldedIndices = WeldCoincidentVertices(combinedMesh, allIndexes, WELD_THRESHOLD);

//            // 6. 메쉬 최적화 및 갱신
//            combinedMesh.Optimize();
//            combinedMesh.Refresh();
//            combinedMesh.ToMesh();

//            // 7. 결과 오브젝트 선택
//            Selection.activeGameObject = combinedMesh.gameObject;

//            // 8. 원본 오브젝트 삭제
//            foreach (ProBuilderMesh pb in pbMeshes)
//            {
//                if (pb != combinedMesh)
//                {
//                    Undo.DestroyObjectImmediate(pb.gameObject);
//                }
//            }

//            Debug.Log($"[ProBuilder] {pbMeshes.Count}개 메시 병합 및 근접 정점 Weld 완료! (거리 오차: {WELD_THRESHOLD})");
//        }
//        else
//        {
//            Debug.LogError("[ProBuilder] 메시 병합에 실패했습니다.");
//        }
//    }

//    /// <summary>
//    /// 지정된 거리에 위치한 중복 정점들을 하나로 합칩니다.
//    /// </summary>
//    private static List<int> WeldCoincidentVertices(ProBuilderMesh pb, IEnumerable<int> indices, float neighborDistance)
//    {
//        List<int> result = new List<int>();

//        // 근접 정점 고리(Coincident Vertices) 검색 후 병합 API 호출
//        pb.GetCoincidentVertices(indices, result);
//        List<int> welded = VertexEditing.WeldVertices(pb, result, neighborDistance);

//        return welded;
//    }

//    [MenuItem("Tools/ProBuilder/Selected Meshes Combine & Weld", true)]
//    private static bool ValidateCombineSelectedProBuilderObjects()
//    {
//        return Selection.gameObjects.Length >= 2;
//    }
//}