using System;
using UnityEngine;

namespace Kekser.SaveSystem
{
    public class SaveLoadKeyInput : MonoBehaviour
    {
        [SerializeField] 
        private string _savePath;

        private void Reset()
        {
            _savePath = Application.persistentDataPath + "/save.sav";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
                SaveLoadManager.Save(_savePath);
            if (Input.GetKeyDown(KeyCode.F9))
                SaveLoadManager.Load(_savePath);
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F12))
                SaveLoadManager.Delete(_savePath);
#endif
        }
    }
}