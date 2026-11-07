# if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using PlayFab.ClientModels;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.UI;

public class WorkShopJsonHelper : MonoBehaviour
{
    // public static List<CatalogItem> items = new List<CatalogItem>();
    private const string UrlPattern = "https://docs.google.com/spreadsheets/d/{0}/export?format=csv&gid={1}";
    private string TableId = "1c6Qg968njd25Pcq1IR3w4eiIghsPfQjrbRwtjw0riuI";
    // Start is called before the first frame update
    void Start()
    {
        //LoadCSV();
    }

    // [MenuItem("Tools/Download Item CSV", false, 100)]
    // static void DownloadItemCSV()
    // {
    //     
    // }

    public void DownloadCSV()
    {
        StartCoroutine(SyncCoroutine());
    }

    public IEnumerator SyncCoroutine()
    {
        var folder = string.Format("{0}/Resources/", Application.dataPath);

        Debug.Log("<color=yellow>Sync started, please wait for confirmation message...</color>");

        var dict = new Dictionary<string, UnityWebRequest>();


        var url_0 = string.Format(UrlPattern, TableId, 0);

        Debug.Log($"Downloading: {url_0}...");

        dict.Add(url_0, UnityWebRequest.Get(url_0));


        foreach (var entry in dict)
        {
            var url = entry.Key;
            var request = entry.Value;

            if (!request.isDone)
            {
                yield return request.SendWebRequest();
            }

            if (request.error == null)
            {
                //var sheet = Sheets.Single(i => url == string.Format(UrlPattern, TableId, i.Id));
                var path = System.IO.Path.Combine(folder, "WorkShopItems.csv");

                System.IO.File.WriteAllBytes(path, request.downloadHandler.data);
                Debug.LogFormat("Sheet {0} downloaded to {1}", 0, path);
            }
            else
            {
                throw new Exception(request.error);
            }
        }

        UnityEditor.AssetDatabase.Refresh();

        Debug.Log("<color=green>Localization successfully synced!</color>");
    }

    [MenuItem("Tools/Create Workshop Json")]
    static void WriteString()
    {
        string path = "Assets/Resources/result.txt";
        //TextAsset assetOld = Resources.Load<TextAsset>("result");
        // if (assetOld != null)
        // {
        AssetDatabase.DeleteAsset(path);
        //}
        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter(path, true);
        string s = LoadCSV();
        writer.Write(s);
        writer.Close();
        //Re-import the file to update the reference in the editor
        AssetDatabase.ImportAsset(path);
        TextAsset asset = Resources.Load<TextAsset>("result");
        //Print the text from the file
        Debug.Log(asset.text);
    }

    private static string ReplaceMarkers(string text)
    {
        return text.Replace("[Newline]", "\n");
    }
    public static string LoadCSV()
    {

        //items.Clear();
        var textAssets = Resources.LoadAll<TextAsset>("WorkShopItems");
        TextAsset baseWorkshop = Resources.Load<TextAsset>("workShopJsonBase");

        CatalogJson catalog = JsonConvert.DeserializeObject<CatalogJson>(baseWorkshop.text);


        string res = "";

        foreach (var textAsset in textAssets)
        {
            var text = ReplaceMarkers(textAsset.text);
            var matches = Regex.Matches(text, "\"[\\s\\S]+?\"");

            foreach (Match match in matches)
            {
                text = text.Replace(match.Value, match.Value.Replace("\"", null).Replace(",", "[comma]").Replace("\n", "[newline]"));
            }

            var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            //var lines = text.Split(',');

            for (var i = 0; i < lines.Length; i++)
            {
                var columns = lines[i].Split(',').Select(j => j.Trim()).Select(j => j.Replace("[comma]", ",").Replace("[newline]", "\n")).ToList();
                // var key = columns[0];
                // if (string.IsNullOrEmpty(key)) continue;
                // for (var j = 1; j < languages.Count; j++)
                // {
                //     Dictionary[languages[j]].Add(key, columns[j]);
                // }
                if (catalog.Catalog.FindIndex(e => e.ItemId.Equals(columns[0])) >= 0) continue;

                string[] idSplit = columns[0].Split('_');
                string displayName = "";
                for (int m = 1; m < idSplit.Length; m++)
                {
                    displayName += char.ToUpper(idSplit[m][0]) + idSplit[m].Substring(1);
                    if (m < idSplit.Length - 1)
                        displayName += " ";
                }



                CatalogItem catalogItem = new CatalogItem()
                {
                    CatalogVersion = "WorkShop",
                    ItemId = columns[0],
                    ItemClass = columns[1],
                    DisplayName = displayName,
                    Tags = new List<string>() { columns[2] },
                    VirtualCurrencyPrices = uint.Parse(columns[3]) > 0 ? new Dictionary<string, uint>() { { "GK", uint.Parse(columns[3]) } } : null,
                    CustomData = columns.Count > 4 ? columns[4] : null
                };

                catalog.Catalog.Add(catalogItem);
            }
        }

        //Debug.Log(JsonConvert.SerializeObject(items));
        // WriteString(JsonConvert.SerializeObject(items));
        res = JsonConvert.SerializeObject(catalog);
        return res;
    }

    public static void WriteString(string text)
    {
        string path = Application.persistentDataPath + "/result.txt";
        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter(path, true);
        writer.Write(text);
        writer.Close();
        StreamReader reader = new StreamReader(path);
        //Print the text from the file
        Debug.Log(reader.ReadToEnd());
        reader.Close();
    }





    [MenuItem("Tools/Generate Workshop Items", false, 100)]
    static void GenerateWorkshopItem()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.Log("Cần chọn 1 object!");
            return;
        }

        string LODConflict = "";

        foreach (var v0 in Selection.gameObjects)
        {
            var gameObject = Instantiate(v0);
            gameObject.name = v0.name;
            var LODGroup = gameObject.GetComponent<LODGroup>();
            if (LODGroup == null) LODGroup = gameObject.AddComponent<LODGroup>();
            var alignBone = gameObject.GetComponent<AlignBones>();
            if (alignBone == null) alignBone = gameObject.AddComponent<AlignBones>();
            var equipmentItem = gameObject.GetComponent<EquipmentItem>();
            if (equipmentItem == null) equipmentItem = gameObject.AddComponent<EquipmentItem>();

            List<LOD> lods = new List<LOD>();
            List<List<Renderer>> listRenders = new List<List<Renderer>>()
                { new List<Renderer>(), new List<Renderer>(), new List<Renderer>() };

            int lodCount = 0;
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                Transform child = gameObject.transform.GetChild(i);
                if (child.childCount > 0)
                    alignBone.m_bone = child;
                else
                {
                    if (child.name.Contains("lod1"))
                    {
                        lodCount = 2;
                        listRenders[1].Add(child.GetComponent<SkinnedMeshRenderer>());
                    }
                    else if (child.name.Contains("lod2"))
                    {
                        lodCount = 3;
                        listRenders[2].Add(child.GetComponent<SkinnedMeshRenderer>());
                    }
                    else if (!child.name.Contains("lod"))
                    {
                        listRenders[0].Add(child.GetComponent<SkinnedMeshRenderer>());
                        lodCount = 1;
                    }
                    else
                    {
                        lodCount = 1;
                        LODConflict += gameObject.name + "\n";
                    }
                }
            }

            List<float> lodSize = new List<float>();
            switch (lodCount)
            {
                case 1:
                    lodSize.Add(3.2f);
                    break;
                case 2:
                    lodSize.Add(40f);
                    lodSize.Add(3.2f);
                    break;
                case 3:
                    lodSize.Add(40f);
                    lodSize.Add(12f);
                    lodSize.Add(3.2f);
                    break;

            }

            //var lodSize = LODGroup.GetLODs(); //(float)Math.Round(1d / lodCount, 1);
            //.screenRelativeTransitionHeight
            for (int i = 0; i < listRenders.Count; i++)
            {
                if (listRenders[i].Count > 0)
                {
                    float crrLodSize = i < lodSize.Count ? lodSize[i] : 3.2f;
                    LOD lod = new LOD(crrLodSize / 100f, listRenders[i].ToArray());
                    lods.Add(lod);
                }
            }

            LODGroup.SetLODs(lods.ToArray());
            LODGroup.size = 1.75f;

            var meshes = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var mesh in meshes)
            {
                MeshDynamicColor meshDynamicColor = mesh.GetComponent<MeshDynamicColor>();
                if (meshDynamicColor == null)
                {
                    meshDynamicColor = mesh.AddComponent<MeshDynamicColor>();
                }
            }


            PrefabUtility.SaveAsPrefabAsset(gameObject, $"Assets/Resources/WorkShop/Draft/{gameObject.name}.prefab");
            DestroyImmediate(gameObject);
        }

        CreateTextFile(LODConflict, "Assets/Resources/LODConflict.txt");
        AssetDatabase.SaveAssets();
    }


    [MenuItem("Tools/Create Workshop Item Icon", false, 100)]
    static void CreateWorkshopItemIcon()
    {
        if (Selection.objects.Length == 0)
        {
            Debug.Log("Cần chọn 1 object!");
            return;
        }

        string pathForSave = "Assets/Resources/WorkShop/ItemIcon/";

        foreach (var v0 in Selection.objects)
        {
            if (!v0.name.Contains("_large")) continue;

            string path = AssetDatabase.GetAssetPath(v0);
            Sprite sprite = ConvertToTextureAndLoad(path);

            string name = "";

            //gameObject.name = v0.name;
            bool fitName = false;
            for (ItemId itemId = ItemId.SharpDresser; itemId < ItemId.END_WEAPON; itemId++)
            {

                int index_ = itemId.ToString().IndexOf('_');
                if (index_ > 0)
                {
                    string s = itemId.ToString().Remove(0, index_ + 1);
                    if (v0.name.Contains(s))
                    {
                        name = itemId.ToString();
                        fitName = true;
                        break;
                    }
                }

            }

            if (fitName)
            {
                ConvertToPngAndSave($"Assets/Resources/WorkShop/DraftIcon/{name}.png", sprite);

            }

            //DestroyImmediate(gameObject);

        }

        AssetDatabase.SaveAssets();
    }


    [MenuItem("Tools/Create Materials", false, 100)]
    static void CreateMaterials()
    {
        if (Selection.objects.Length == 0)
        {
            Debug.Log("Cần chọn 1 object!");
            return;
        }
        Debug.Log(Selection.objects.Length);
        //string pathForSave = "Assets/Resources/WorkShop/ItemIcon/";

        foreach (var v0 in Selection.objects)
        {
            if (v0.name.EndsWith("_red") || v0.name.EndsWith("_blue")) continue;
            string path = AssetDatabase.GetAssetPath(v0);


            Material material = (Material)AssetDatabase.LoadAssetAtPath(path,
                typeof(Material));

            path = path.Split('.')[0];
            if (material != null)
            {
                string mainTextPath = path + "_color.png";
                Texture mainText = (Texture)AssetDatabase.LoadAssetAtPath(mainTextPath,
                typeof(Texture));
                if (mainText == null)
                {
                    mainTextPath = path + ".png";
                    mainText = (Texture)AssetDatabase.LoadAssetAtPath(mainTextPath,
                typeof(Texture));
                }

                if (mainText == null && material.name.EndsWith("_1"))
                {
                    mainTextPath = path.Replace("_1", "_color.png");
                    mainText = (Texture)AssetDatabase.LoadAssetAtPath(mainTextPath,
                typeof(Texture));
                    if (mainText == null)
                    {
                        mainTextPath = path.Replace("_1", ".png");
                        mainText = (Texture)AssetDatabase.LoadAssetAtPath(mainTextPath,
                    typeof(Texture));
                    }
                }

                if (mainText != null)
                {
                    material.mainTexture = mainText;
                    string blueTextPath = mainTextPath.Replace("_color.png", "_blue_color.png");
                    Texture blueText = (Texture)AssetDatabase.LoadAssetAtPath(blueTextPath,
                typeof(Texture));
                    if (blueText != null)
                    {
                        string orgName = material.name;

                        Material blueMat = Instantiate(material);
                        blueMat.mainTexture = blueText;
                        blueMat.name = orgName + "_blue";
                        string newPath = path + "_blue.mat";
                        AssetDatabase.CreateAsset(blueMat, newPath);

                        // AssetDatabase.AddObjectToAsset(blueMat, newPath);

                        //path = AssetDatabase.GUIDToAssetPath(path + ".mat");
                        AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(material), material.name + "_red");
                        Debug.Log(AssetDatabase.GetAssetPath(material));

                    }
                }

            }

            //DestroyImmediate(gameObject);

        }

        Debug.Log("End");

        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Remove Collider", false, 100)]
    public static void RemoveCollider()
    {
        if (Selection.objects.Length == 0)
        {
            Debug.Log("Cần chọn 1 object!");
            return;
        }

        foreach (var v0 in Selection.objects)
        {
            if (v0.GetComponent<Collider>() != null)
            {
                DestroyImmediate(v0.GetComponent<Collider>());
            }
        }
    }


    /// <summary>
    ///Convert & save to image
    /// </summary>
    private static void ConvertToPngAndSave(string path, Sprite sprite)
    {
        //Convert to png
        byte[] bytes = sprite.texture.EncodeToPNG();
        //Save
        File.WriteAllBytes(path, bytes);
    }

    /// <summary>
    ///Convert & load to texture
    /// </summary>
    private static Sprite ConvertToTextureAndLoad(string path)
    {
        //Read
        byte[] bytes = File.ReadAllBytes(path);
        //Convert image to texture
        Texture2D loadTexture = new Texture2D(2, 2);
        loadTexture.LoadImage(bytes);
        //Convert textures to sprites
        return Sprite.Create(loadTexture, new Rect(0, 0, loadTexture.width, loadTexture.height), Vector2.zero);
    }


    public static void CreateTextFile(string content, string path)
    {
        //string path = "Assets/Resources/result.txt";
        //TextAsset assetOld = Resources.Load<TextAsset>("result");
        // if (assetOld != null)
        // {
        AssetDatabase.DeleteAsset(path);
        //}
        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter(path, true);
        //string s = LoadCSV();
        writer.Write(content);
        writer.Close();
        //Re-import the file to update the reference in the editor
        AssetDatabase.ImportAsset(path);
        //TextAsset asset = Resources.Load<TextAsset>("result");
        //Print the text from the file
        Debug.Log("Success");
    }



    // private void CreateLodGroup()
    //      {
    //          GameObject[] sel = Selection.gameObjects;
    //          if (sel == null || sel.Length == 0)
    //          {
    //              Debug.LogError("FAILED -> You must have at least 1 GameObject selected!");
    //              return;
    //          }
    //          var lods = new SortedList<int, GameObject>();
    //          string mainName = null;
    //          foreach (GameObject o in sel)
    //          {
    //              string oName = o.name;
    //              Match regexMatch = Regex.Match(oName, @"(.*)_LOD([0-9])");
    //              if (regexMatch.Success)
    //              {
    //                  string oMainName = regexMatch.Groups[1].ToString();
    //                  int oIndex = Convert.ToInt32(regexMatch.Groups[2].ToString());
    //                  if (mainName == null || oMainName == mainName)
    //                  {
    //                      mainName = oMainName;
    //                  }
    //                  else
    //                  {
    //                      Debug.LogError("FAILED -> The GameObject main names aren't matching! Expected main name \"" +
    //                                     mainName + "\", got \"" + oMainName + "\"!");
    //                      return;
    //                  }
    //                  if (!lods.ContainsKey(oIndex))
    //                  {
    //                      lods.Add(oIndex, o);
    //                  }
    //                  else
    //                  {
    //                      Debug.LogError("FAILED -> The LOD object with the ID " + oIndex +
    //                                     " exists twice! Make sure to avoid duplicates!");
    //                      return;
    //                  }
    //              }
    //              else
    //              {
    //                  Debug.LogError("FAILED -> GameObject \"" + oName +
    //                                 "\" was in the wrong name format to be added as a LOD object!");
    //                  return;
    //              }
    //          }
    //          int lfIndex = lods.Count - 1;
    //          while (lfIndex >= 0)
    //          {
    //              if (lods.ContainsKey(lfIndex))
    //              {
    //                  lfIndex--;
    //              }
    //              else
    //              {
    //                  Debug.LogError("FAILED -> LOD level " + lfIndex + " could not be found!");
    //                  return;
    //              }
    //          }
    //          var lodObject = new GameObject(mainName);
    //          var lodGroup = lodObject.AddComponent<LODGroup>();
    //          var lodLevels = new LOD[lods.Count];
    //          foreach (var lod in lods)
    //          {
    //              float lodSize = (float)Math.Round(1d / lods.Count, 1);
    //              Debug.Log("LOG -> Size=" + lodSize);
    //              Renderer[] renderers = lod.Value.GetComponentsInChildren<Renderer>();
    //              if (renderers == null || renderers.Length == 0)
    //              {
    //                  Debug.LogWarning("WARNING -> No Renderers for LOD level " + lod.Key + " found!");
    //              }
    //              lod.Value.transform.SetParent(lodObject.transform);
    //              LOD lodLevel = new LOD(lodSize, renderers);
    //              lodLevels[lod.Key] = lodLevel;
    //          }
    //          lodGroup.SetLODs(lodLevels); // <!!> The exception is raised from calling this method! <!!>
    //      }


    [MenuItem("Tools/Turn off shadow", false, 100)]
    static void TurnOffCastShadow()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.Log("Cần chọn 1 object!");
            return;
        }


        foreach (var v0 in Selection.gameObjects)
        {
            Renderer[] renderers = v0.GetComponentsInChildren<Renderer>();
            if(renderers != null)
            {
                for(int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderers[i].receiveShadows = false;
                }
            } 
        }
        Debug.Log("Success");

        AssetDatabase.SaveAssets();
    }


    [MenuItem("Tools/FixLOD", false, 100)]
    static void FixLOD()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.Log("Cần chọn 1 object!");
            return;
        }


        foreach (var v0 in Selection.gameObjects)
        {
            var LODGroup = v0.GetComponent<LODGroup>();
            if(LODGroup != null && LODGroup.size < 1.75)
            {
                LODGroup.size = 1.75f;
            }
            
        }

        AssetDatabase.SaveAssets();
    }
    
    
    [MenuItem("Tools/Merge Map Texture Materials", false, 100)]
    static void MergeMapTextureMaterials()
    {
        if (Selection.objects.Length == 0)
        {
            Debug.Log("Cần chọn 1 object!");
            return;
        }
        Debug.Log(Selection.objects.Length);
        string pathOfMainTexture = "Assets/Maps/_MaterialCommon/";

        foreach (var v0 in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(v0);

            Material material = (Material)AssetDatabase.LoadAssetAtPath(path,
                typeof(Material));

            if (material != null)
            {
                Texture mainText = material.mainTexture;
                if (mainText != null)
                {
                    string mainTextPath = AssetDatabase.GetAssetPath(mainText);
                    string newTextPath = pathOfMainTexture + mainText.name + ".png";
                    Texture newMainText = (Texture)AssetDatabase.LoadAssetAtPath(newTextPath,
                    typeof(Texture));

                    if (newMainText != null)
                    {
                        material.mainTexture = newMainText;
                        if (!mainTextPath.Equals(newTextPath))
                            AssetDatabase.DeleteAsset(mainTextPath);

                    }
                    else
                    {
                        AssetDatabase.MoveAsset(mainTextPath, newTextPath);
                    }
                }

            }

            //DestroyImmediate(gameObject);

        }

        Debug.Log("End");

        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Create Collider", false, 100)]
    static void CreateCollider()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.Log("Cần chọn 1 object!");
            return;
        }


        foreach (var v0 in Selection.gameObjects)
        {
            foreach(Transform child in v0.transform)
            {
                MeshRenderer meshRender = child.GetComponent<MeshRenderer>();
                if(meshRender != null && meshRender.materials != null && meshRender.sharedMaterials[0].name.Contains("phy"))
                {
                    child.AddComponent<MeshCollider>().convex = true;
                    meshRender.enabled = false;
                }
            }

        }

    }
}

public class TexturePngConverter : MonoBehaviour
{

    
}


public class JsonItem
{
    public string ItemId;
    public string ItemClass;
    public int price;
    public string tag;
    public string customData;
}

public class CatalogJson
{
    public string CatalogVersion;
    public List<CatalogItem> Catalog;
    public string[] DropTables;
}

#endif