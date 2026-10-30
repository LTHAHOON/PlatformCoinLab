using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace CoinGame.Utilities
{
    
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();
            SerializedProperty sceneAssetProperty =  property.FindPropertyRelative("_sceneAsset");
            SerializedProperty scenePathProperty =  property.FindPropertyRelative("_scenePath");

            SceneAsset sceneAsset = (SceneAsset)sceneAssetProperty.objectReferenceValue;
            scenePathProperty.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
            property.serializedObject.ApplyModifiedProperties();
            root.Add(new PropertyField(sceneAssetProperty));
            root.Add(new PropertyField(scenePathProperty));
            root.RegisterCallback<SerializedPropertyChangeEvent>(evt => {
                if(evt.changedProperty.propertyPath != sceneAssetProperty.propertyPath)
                {
                    return;
                }
                sceneAsset = (SceneAsset)sceneAssetProperty.objectReferenceValue;
                scenePathProperty.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
                property.serializedObject.ApplyModifiedProperties();
            });
            return root;
        }

    }
    #endif

    [Serializable]
    public class SceneReference
    {
        [SerializeField]
        private SceneAsset _sceneAsset;
        [SerializeField]
        private string _scenePath;

        public string ScenePath => _scenePath;
    }
}
