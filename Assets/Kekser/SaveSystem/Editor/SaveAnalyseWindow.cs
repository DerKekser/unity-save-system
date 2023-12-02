using System.Collections.Generic;
using System.IO;
using Kekser.SaveSystem.Data;
using UnityEditor;
using UnityEngine;

namespace Kekser.SaveSystem
{
    public class SaveAnalyseWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private DataObject _dataObject;
        
        Dictionary<IData, bool> _foldouts = new Dictionary<IData, bool>();

        [MenuItem("Tools/Save System/Analyse")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveAnalyseWindow>();
            window.titleContent = new GUIContent("Save Analyse");
            window.Show();
        }
        
        private void OnGUI()
        {
            if (GUILayout.Button("Open Save File"))
            {
                string path = EditorUtility.OpenFilePanel("Open Save File", "", "sav");
                if (path.Length != 0)
                {
                    byte[] data = SaveLoadManager.Decompress(File.ReadAllBytes(Application.persistentDataPath + "/save.sav"));
                    SaveBuffer saveData = new SaveBuffer(data);
                
                    _dataObject = new DataObject();
                    _dataObject.DataDeserialize(saveData);
                }
            }

            if (_dataObject == null)
            {
                EditorGUILayout.HelpBox("No save file loaded!", MessageType.Warning);
                return;
            }
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            RenderIData(_dataObject);
            EditorGUILayout.EndScrollView();
        }
        
        private void RenderIData(IData data)
        {
            EditorGUI.indentLevel++;
            if (data is DataObject dataObject)
                RenderDataObject(dataObject);
            else if (data is DataArray dataArray)
                RenderDataArray(dataArray);
            else if (data is DataElement dataElement)
                RenderDataElement(dataElement);
            EditorGUI.indentLevel--;
        }
        
        private void RenderDataObject(DataObject dataObject)
        {
            _foldouts[dataObject] = EditorGUILayout.Foldout(_foldouts.ContainsKey(dataObject) ? _foldouts[dataObject] : false, "Data Object");
            
            if (!_foldouts[dataObject])
                return;
            
            foreach (KeyValuePair<string, IData> dataPair in dataObject.GetEnumerable())
            {
                EditorGUILayout.LabelField(dataPair.Key + ":");
                RenderIData(dataPair.Value);
            }
        }
        
        private void RenderDataArray(DataArray dataArray)
        {
            _foldouts[dataArray] = EditorGUILayout.Foldout(_foldouts.ContainsKey(dataArray) ? _foldouts[dataArray] : false, "Data Array");
            
            if (!_foldouts[dataArray])
                return;
            
            for (int i = 0; i < dataArray.Count(); i++)
            {
                EditorGUILayout.LabelField(i + ":");
                RenderIData(dataArray[i]);
            }
        }
        
        private void RenderDataElement(DataElement dataElement)
        {
            _foldouts[dataElement] = EditorGUILayout.Foldout(_foldouts.ContainsKey(dataElement) ? _foldouts[dataElement] : false, "Data Element");
            
            if (!_foldouts[dataElement])
                return;
            
            SaveBuffer buffer = new SaveBuffer();
            dataElement.DataSerialize(buffer);
            
            EditorGUILayout.LabelField(System.Text.Encoding.UTF8.GetString(buffer.Data));
        }
    }
}