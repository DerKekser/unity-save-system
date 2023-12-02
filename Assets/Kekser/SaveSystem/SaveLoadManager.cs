using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Game.Scripts.SaveSystem.Attributes;
using Game.Scripts.SaveSystem.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.SaveSystem
{
    public class SaveLoadManager : MonoBehaviour
    {
        public static async Task<bool> Save(string file)
        {
            try
            {
                DataObject dataObject = new DataObject();
                dataObject.Add("Scene", new DataElement(SceneManager.GetActiveScene().name));
                SaveAttributeManager.Save(dataObject);
                
                SaveBuffer saveData = new SaveBuffer();
                dataObject.DataSerialize(saveData);
                File.WriteAllBytes(file, Compress(saveData.Data));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        public static async Task<bool> Load(string file)
        {
            if (!File.Exists(file))
                return false;
            try
            {
                byte[] data = Decompress(File.ReadAllBytes(file));
                SaveBuffer saveData = new SaveBuffer(data);
                
                DataObject dataObject = new DataObject();
                dataObject.DataDeserialize(saveData);
                string sceneName = dataObject.Get<DataElement>("Scene").ToObject<string>();
                if (sceneName != SceneManager.GetActiveScene().name)
                {
                    AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
                    while (!operation.isDone)
                        await Task.Yield();
                }
                int currentFrame = Time.frameCount;
                while (Time.frameCount == currentFrame)
                    await Task.Yield();
                SaveAttributeManager.Load(dataObject);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }
        
        public static bool Delete(string file)
        {
            if (!File.Exists(file))
                return false;
            try
            {
                Debug.LogWarning("Deleting save file");
                File.Delete(file);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        public static byte[] Compress(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(ms, CompressionMode.Compress))
                {
                    gz.Write(bytes, 0, bytes.Length);
                }
                return ms.ToArray();
            }
        }
        
        public static byte[] Decompress(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gz = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
                {
                    gz.CopyTo(ms);
                }
                return ms.ToArray();
            }
        }
    }
}