namespace Tilia.Utilities
{
    using SimpleJSON;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Unity.EditorCoroutines.Editor;
    using UnityEditor;
    using UnityEditor.PackageManager;
    using UnityEditor.PackageManager.Requests;
    using UnityEngine;
    using UnityEngine.Networking;

    [InitializeOnLoad]
    public class PackageImporter : EditorWindow
    {
        private static EditorWindow promptWindow;
        private const string WindowPath = "Window/Tilia/";
        private const string WindowName = "Package Importer";
        private const string DataUri = "https://www.vrtk.io/tilia.json";
        private static bool windowAlreadyOpen;
        private static List<string> installedPackages = new List<string>();
        private static AddRequest addRequest;
        private static ListRequest installedPackagesRequest;

        private bool registryFound;
        private string manifestFile;
        private JSONNode rootManifest;
        private EditorCoroutine getWebDataRoutine;
        private string availableScopedRegistry;
        private List<string> availablePackages = new List<string>();
        private Dictionary<string, string> packageDescriptions = new Dictionary<string, string>();
        private Dictionary<string, string> packageUrls = new Dictionary<string, string>();
        private Vector2 scrollPosition;
        private string searchString = "";

        public void OnGUI()
        {
            if (!windowAlreadyOpen)
            {
                DownloadPackageList();
                windowAlreadyOpen = true;
            }

            DrawHorizontalLine(Color.black);
            GUILayout.Space(1);
            GUILayout.Label(" Available Tilia Packages To Import", new GUIStyle { fontSize = 15, fontStyle = FontStyle.Bold });
            DrawHorizontalLine(Color.black);

            if (!registryFound)
            {
                EditorGUILayout.HelpBox("The required scoped registry has not been found in your project manifest.json.\n\n" +
                    "Click the button below to attempt to automatically add the required scoped registry to your project manifest.json file.", MessageType.Warning);
                GUILayout.Space(4);
                if (GUILayout.Button("Add Scoped Registry"))
                {
                    AddRegistry();
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Filter", GUILayout.Width(40));
                    searchString = EditorGUILayout.TextField(searchString);
                }

                DrawHorizontalLine();

                using (GUILayout.ScrollViewScope scrollViewScope = new GUILayout.ScrollViewScope(scrollPosition))
                {
                    scrollPosition = scrollViewScope.scrollPosition;

                    foreach (string availablePackage in availablePackages.Except(installedPackages).ToList())
                    {
                        if (!string.IsNullOrEmpty(searchString.Trim()) && !availablePackage.Contains(searchString))
                        {
                            continue;
                        }

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            packageDescriptions.TryGetValue(availablePackage, out string packageDescription);
                            packageUrls.TryGetValue(availablePackage, out string packageUrl);
                            GUILayout.Label(new GUIContent(availablePackage, packageDescription));
                            GUILayout.FlexibleSpace();
                            if (addRequest == null)
                            {
                                if (GUILayout.Button("Add"))
                                {
                                    addRequest = Client.Add(availablePackage);
                                    EditorApplication.update += HandlePackageAddRequest;
                                }

                                if (GUILayout.Button(new GUIContent("View", "View on GitHub")))
                                {
                                    Application.OpenURL(packageUrl);
                                }
                                GUILayout.Label(" ", new GUIStyle { fontSize = 10 });
                            }
                        }
                        DrawHorizontalLine();
                    }
                }

                DrawHorizontalLine(Color.black);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (addRequest == null)
                    {
                        if (GUILayout.Button("Refresh Package List"))
                        {
                            DownloadPackageList();
                        }
                    }
                }
            }

            GUILayout.Space(8);
        }

        private void DownloadPackageList()
        {
            GetInstalledPackages();
            GetRawData();
        }

        private void GetRawData()
        {
            if (getWebDataRoutine != null)
            {
                return;
            }

            getWebDataRoutine = EditorCoroutineUtility.StartCoroutine(GetWebRequest(DataUri), this);
        }

        private IEnumerator GetWebRequest(string uri)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.ConnectionError:
                    case UnityWebRequest.Result.DataProcessingError:
                        Debug.LogError("Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogError("HTTP Error: " + webRequest.error);
                        break;
                    case UnityWebRequest.Result.Success:
                        ParseRawData(webRequest.downloadHandler.text);
                        break;
                }
            }

            getWebDataRoutine = null;
        }

        private void ParseRawData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            JSONNode jsonData = JSONNode.Parse(data);

            availablePackages.Clear();
            packageDescriptions.Clear();
            packageUrls.Clear();
            if (!string.IsNullOrEmpty(jsonData["scopedRegistry"]))
            {
                availableScopedRegistry = "{ " + jsonData["scopedRegistry"] + " }";
            }

            foreach (JSONNode package in jsonData["packages"])
            {
                availablePackages.Add(package["name"]);
                packageDescriptions.Add(package["name"], package["description"] + ".\n\nLatest version: " + package["version"]);
                packageUrls.Add(package["name"], package["url"]);
            }

            DoesRegistryExist();
        }

        private bool DoesRegistryExist()
        {
            manifestFile = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            string manifest = File.ReadAllText(manifestFile);
            rootManifest = JSONNode.Parse(manifest);

            registryFound = false;

            foreach (JSONNode registry in rootManifest["scopedRegistries"])
            {
                if (Array.IndexOf(registry["scopes"], "io.extendreality") > -1)
                {
                    registryFound = true;
                }
            }

            return registryFound;
        }

        private void AddRegistry()
        {
            if (!registryFound && !string.IsNullOrEmpty(availableScopedRegistry))
            {
                JSONNode newNode = JSONNode.Parse(availableScopedRegistry);
                rootManifest["scopedRegistries"].Add(newNode);
                File.WriteAllText(manifestFile, rootManifest.ToString());
                DownloadPackageList();
            }
        }

        private static void GetInstalledPackages()
        {
            installedPackagesRequest = Client.List(false, true);
            EditorApplication.update += HandleInstalledPackagesRequest;
        }

        private static void HandleInstalledPackagesRequest()
        {
            if (installedPackagesRequest.IsCompleted)
            {
                if (installedPackagesRequest.Status == StatusCode.Success)
                {
                    installedPackages.Clear();
                    foreach (var packageInfo in installedPackagesRequest.Result)
                    {
                        installedPackages.Add(packageInfo.name);
                    }
                }
                else
                {
                    Debug.LogError("Failure to receive installed packages: " + installedPackagesRequest.Error.message);
                }

                EditorApplication.update -= HandleInstalledPackagesRequest;
            }
        }

        private static void HandlePackageAddRequest()
        {
            if (addRequest.IsCompleted)
            {
                if (addRequest.Status == StatusCode.Success)
                {
                    GetInstalledPackages();
                }
                else
                {
                    Debug.LogError("Failure to add package: " + addRequest.Error.message);
                }

                EditorApplication.update -= HandleInstalledPackagesRequest;
                addRequest = null;
            }
        }

        private static void DrawHorizontalLine(Color color, float height, Vector2 margin)
        {
            GUILayout.Space(margin.x);
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, height), color);
            GUILayout.Space(margin.y);
        }

        private static void DrawHorizontalLine(Color color)
        {
            DrawHorizontalLine(color, 1f, Vector2.one * 5f);
        }

        private static void DrawHorizontalLine()
        {
            DrawHorizontalLine(new Color(0f, 0f, 0f, 0.3f));
        }

        [MenuItem(WindowPath + WindowName)]
        private static void ShowWindow()
        {
            windowAlreadyOpen = false;
            promptWindow = GetWindow(typeof(PackageImporter));
            promptWindow.titleContent = new GUIContent(WindowName);
        }
    }
}