﻿using ResetCore.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResetCore.NAsset
{
    public class AssetLoader
    {
        /// <summary>
        /// 已经加载的Bundle
        /// </summary>
        private static Dictionary<string, AssetBundle> loadedBundle = new Dictionary<string, AssetBundle>();
        /// <summary>
        /// 未加载的Bundle
        /// </summary>
        private static Dictionary<string, BundleResources> bundleResources = new Dictionary<string, BundleResources>();

        public static void LoadAndCallback(string bundleName,
            ThreadPriority priority = ThreadPriority.High,
            Action<float> progressAct = null, Action callBack = null)
        {
            CoroutineTaskManager.Instance.AddTask(LoadBundleAsyc(bundleName), (bo) =>
            {
                if (bo)
                {
                    if (callBack != null)
                        callBack();
                }else
                {
                    Debug.LogError("加载失败");
                }
                
            });
        }

        /// <summary>
        /// 异步加载Bundle
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="priority"></param>
        /// <param name="progressAct"></param>
        /// <returns></returns>
        public static IEnumerator LoadBundleAsyc(string bundleName, 
            ThreadPriority priority = ThreadPriority.High, 
            Action<float> progressAct = null)
        {
            if (HasLoaded(bundleName))
            {
                yield break;
            }

            yield return DownloadAsyc(bundleName, priority, progressAct, (bundle)=> {
                loadedBundle[bundleName] = bundle;
                loadedBundle[bundleName].name = bundleName;
                bundleResources[bundleName] = new BundleResources(loadedBundle[bundleName]);
            });

        }

        private static IEnumerator DownloadAsyc(string bundleName, ThreadPriority priority, Action<float> progressAct, Action<AssetBundle> callback)
        {
            string path = PathEx.Combine(NAssetPaths.defBundleFolderName, bundleName);
            string downloadPath = "";

            bool isPersistentFileExist = FileManager.IsPersistentFileExist(path);
            bool isStreamFileExist = FileManager.IsStreamFileExist(path);
            bool isResourcesFileExist = FileManager.IsResourceFileExist(path);
            if(isPersistentFileExist)
            {
                downloadPath = FileManager.PersistentFileWWWPath(path);

                yield return PersistentDownloadAsync(downloadPath, priority, progressAct, callback);
            }
            else if (isResourcesFileExist)
            {
                yield return ResourcesDownloadAsync(path, priority, progressAct, callback);
            }
            else if (isStreamFileExist)
            {
                downloadPath = FileManager.StreamFileWWWPath(path);
                yield return StreamingDownloadAsync(downloadPath, priority, progressAct, callback);
            }
            else
            {
                Debug.LogError("未找到文件");
                yield break;
            }
        }
        /// <summary>
        /// 使用沙盒下载
        /// </summary>
        /// <param name="downloadPath"></param>
        /// <param name="priority"></param>
        /// <param name="progressAct"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static IEnumerator PersistentDownloadAsync(string downloadPath, 
            ThreadPriority priority, 
            Action<float> progressAct, 
            Action<AssetBundle> callback)
        {
            WWW www = new WWW(downloadPath);
            www.threadPriority = priority;

            while (!www.isDone)
            {
                if (progressAct != null)
                    progressAct(www.progress);

                yield return null;
            }

            if (www.error != null)
            {
                Debug.LogError(www.error);
                yield break;
            }

            if (callback != null)
                callback(www.assetBundle);

            www.Dispose();
        }

        /// <summary>
        /// 使用Resources下载
        /// </summary>
        /// <param name="downloadPath"></param>
        /// <param name="priority"></param>
        /// <param name="progressAct"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static IEnumerator ResourcesDownloadAsync(string downloadPath,
            ThreadPriority priority,
            Action<float> progressAct,
            Action<AssetBundle> callback)
        {
            var request = Resources.LoadAsync<TextAsset>(downloadPath);
            request.priority = (int)priority;

            while (!request.isDone)
            {
                if (progressAct != null)
                    progressAct(request.progress);

                yield return null;
            }

            var asset = request.asset as TextAsset;
            if (asset == null)
            {
                Debug.LogError("加载失败");
                yield break;
            }

            var adRequest = AssetBundle.LoadFromMemoryAsync(asset.bytes);
            adRequest.priority = (int)priority;

            while (!adRequest.isDone)
            {
                if (progressAct != null)
                    progressAct(request.progress);

                yield return null;
            }

            if (adRequest.assetBundle == null)
            {
                Debug.LogError("加载失败");
                yield break;
            }

            if (callback != null)
                callback(adRequest.assetBundle);

            Resources.UnloadAsset(asset);
        }

        /// <summary>
        /// 使用流媒体下载
        /// </summary>
        /// <param name="downloadPath"></param>
        /// <param name="priority"></param>
        /// <param name="progressAct"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static IEnumerator StreamingDownloadAsync(string downloadPath,
            ThreadPriority priority,
            Action<float> progressAct,
            Action<AssetBundle> callback)
        {
            WWW www = new WWW(downloadPath);
            www.threadPriority = priority;

            while (!www.isDone)
            {
                if (progressAct != null)
                    progressAct(www.progress);

                yield return null;
            }

            if (www.error != null)
            {
                Debug.LogError(www.error);
                yield break;
            }

            if (callback != null)
                callback(www.assetBundle);

            www.Dispose();
        }

        /// <summary>
        /// 是否已经被加载
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>    
        public static bool HasLoaded(string name)
        {
            if (loadedBundle.ContainsKey(name))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取Bundle
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AssetBundle GetBundle(string name)
        {
            if(HasLoaded(name))
                return loadedBundle[name];
            return null;
        }


        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="name"></param>
        /// <param name="flag"></param>
        public static void UnloadBundle(string name, bool flag = true)
        {
            AssetBundle bundle = GetBundle(name);

            loadedBundle.Remove(name);

            if(bundle == null)
            {
                Debug.LogWarning("未找到Bundle");
                return;
            }

            var bundleResource = bundleResources[name];
            if(bundleResource != null)
            {
                bundleResource.Reset();
                bundleResources.Remove(name);
            }

            bundle.Unload(flag);
        }

        /// <summary>
        /// 获取文本
        /// </summary>
        /// <param name="bundleName">资源包路径</param>
        /// <param name="resName">文本名</param>
        /// <returns>文本对象</returns>
        public static TextAsset GetText(string bundleName, string resName)
        {
            BundleResources resource = bundleResources[bundleName] as BundleResources;
            return resource != null ? resource.GetText(resName) : null;
        }
        public static TextAsset GetTextByR(string R)
        {
            var Rl = R.Split(new string[] { "###" }, StringSplitOptions.RemoveEmptyEntries);
            return GetText(Rl[0], Rl[1]);
        }

        /// <summary>
        /// 获取Texture
        /// </summary>
        /// <param name="bundleName">资源包路径</param>
        /// <param name="resName">文本名</param>
        /// <returns>Texture</returns>
        public static Texture GetTexture(string bundleName, string resName)
        {
            BundleResources resource = bundleResources[bundleName] as BundleResources;
            return resource != null ? resource.GetTexture(resName) : null;
        }
        public static Texture GetTextureByR(string R)
        {
            var Rl = R.Split(new string[] { "###" }, StringSplitOptions.RemoveEmptyEntries);
            return GetTexture(Rl[0], Rl[1]);
        }

        /// <summary>
        /// 获取音频
        /// </summary>
        /// <param name="bundleName">资源包路径</param>
        /// <param name="resName">音频名</param>
        /// <returns>音频对象</returns>
        public static AudioClip GetAudio(string bundleName, string resName)
        {
            BundleResources resource = bundleResources[bundleName] as BundleResources;
            return resource != null ? resource.GetAudio(resName) : null;
        }
        public static AudioClip GetAudioByR(string R)
        {
            var Rl = R.Split(new string[] { "###" }, StringSplitOptions.RemoveEmptyEntries);
            return GetAudio(Rl[0], Rl[1]);
        }

        /// <summary>
        /// 获取材质
        /// </summary>
        /// <param name="bundleName">资源包路径</param>
        /// <param name="resName">材质名</param>
        /// <returns>材质对象</returns>
        public static Material GetMaterial(string bundleName, string resName)
        {
            BundleResources resource = bundleResources[bundleName] as BundleResources;
            return resource != null ? resource.GetMaterial(resName) : null;
        }
        public static Material GetMaterialByR(string R)
        {
            var Rl = R.Split(new string[] { "###" }, StringSplitOptions.RemoveEmptyEntries);
            return GetMaterial(Rl[0], Rl[1]);
        }

        /// <summary>
        /// 获取Shader
        /// </summary>
        /// <param name="bundleName">资源包路径</param>
        /// <param name="resName">Shader名</param>
        /// <returns>Shader对象</returns>
        public static Shader GetShader(string bundleName, string resName)
        {
            BundleResources resource = bundleResources[bundleName] as BundleResources;
            return resource != null ? resource.GetShader(resName) : null;
        }

        public static Shader GetShaderByR(string R)
        {
            var Rl = R.Split(new string[] { "###" }, StringSplitOptions.RemoveEmptyEntries);
            return GetShader(Rl[0], Rl[1]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="resName"></param>
        /// <returns></returns>
        public static GameObject GetGameObject(string bundleName, string resName)
        {
            BundleResources resource = bundleResources[bundleName] as BundleResources;
            return resource != null ? resource.GetGameObject(resName) : null;
        }
        /// <summary>
        /// 通过R来获取GameObject
        /// </summary>
        /// <param name="R"></param>
        /// <returns></returns>
        public static GameObject GetGameObjectByR(string R)
        {
            var Rl = R.Split(new string[] { "###" }, StringSplitOptions.RemoveEmptyEntries);
            return GetGameObject(Rl[0], Rl[1]);
        }
    }

}