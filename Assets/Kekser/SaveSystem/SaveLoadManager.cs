using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Kekser.SaveSystem.Attributes;
using Kekser.SaveSystem.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kekser.SaveSystem
{
    public class SaveLoadManager : MonoBehaviour
    {
        /// <summary>True while a save or load runs.</summary>
        public static bool IsBusy { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsBusy = false;

        private static void EnsureFolderExists(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
                return;
            if (Directory.Exists(directory))
                return;
            Directory.CreateDirectory(directory);
        }

        public static Task<bool> Save(string file) => Save(file, null);

        /// <summary>
        /// Saves the active scene. <paramref name="meta"/> is stored next to the scene data and can be read back
        /// without loading the save (<see cref="LoadMeta"/>), e.g. a version or what a save slot shows.
        /// </summary>
        public static Task<bool> Save(string file, DataObject meta)
        {
            if (IsBusy)
            {
                Debug.LogError("Save/Load already in progress.");
                return Task.FromResult(false);
            }
            IsBusy = true;
            try
            {
                DataObject dataObject = new DataObject();
                dataObject.Add("Scene", new DataElement(SceneManager.GetActiveScene().name));
                if (meta != null)
                    dataObject.Add("Meta", meta);
                SaveAttributeManager.Save(dataObject);

                LookUpSaveBuffer saveData = new LookUpSaveBuffer();
                dataObject.DataSerialize(saveData);

                EnsureFolderExists(file);
                // Write next to the file first, so a crash mid-write never leaves a broken save behind.
                string temp = file + ".tmp";
                File.WriteAllBytes(temp, Compress(saveData.Data));
                if (File.Exists(file))
                    File.Delete(file);
                File.Move(temp, file);
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return Task.FromResult(false);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public static Task<bool> Load(string file) => Load(file, false);

        /// <summary>
        /// Loads a save. With <paramref name="reloadScene"/> the scene is loaded fresh even if it is already active,
        /// so no runtime state from before the load is left over.
        /// </summary>
        public static async Task<bool> Load(string file, bool reloadScene)
        {
            if (!File.Exists(file))
                return false;
            if (IsBusy)
            {
                Debug.LogError("Save/Load already in progress.");
                return false;
            }
            IsBusy = true;
            try
            {
                DataObject dataObject = Read(file);
                string sceneName = dataObject.Get<DataElement>("Scene").ToObject<string>();
                if (reloadScene || sceneName != SceneManager.GetActiveScene().name)
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
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Reads only the meta data a save was written with (null if there is none or it can't be read).</summary>
        public static DataObject LoadMeta(string file)
        {
            if (!File.Exists(file) || IsBusy)
                return null;
            IsBusy = true;
            try
            {
                return Read(file).Get<DataObject>("Meta");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static DataObject Read(string file)
        {
            byte[] data = Decompress(File.ReadAllBytes(file));
            LookUpSaveBuffer saveData = new LookUpSaveBuffer(data);

            DataObject dataObject = new DataObject();
            dataObject.DataDeserialize(saveData);
            return dataObject;
        }

        public static bool Delete(string file)
        {
            if (!File.Exists(file))
                return false;
            try
            {
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
