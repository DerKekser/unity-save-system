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
            if (Pressed(KeyCode.F5))
                SaveLoadManager.Save(_savePath);
            if (Pressed(KeyCode.F9))
                SaveLoadManager.Load(_savePath);
#if UNITY_EDITOR
            if (Pressed(KeyCode.F12))
                SaveLoadManager.Delete(_savePath);
#endif
        }

        // Works with the Input System package as well (the old Input class throws when it is the only backend).
        private static bool Pressed(KeyCode key)
        {
#if KEKSER_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null)
                return false;
            switch (key)
            {
                case KeyCode.F5: return keyboard.f5Key.wasPressedThisFrame;
                case KeyCode.F9: return keyboard.f9Key.wasPressedThisFrame;
                case KeyCode.F12: return keyboard.f12Key.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetKeyDown(key);
#endif
        }
    }
}