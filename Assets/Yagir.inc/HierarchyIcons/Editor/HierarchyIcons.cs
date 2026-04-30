using System;
using System.Collections.Generic;
using HierarchyIcons;
using TMPro;
using UnityEditor;
using UnityEngine;
using Yagir.inc.HierarchyIcons.Scripts;
using Object = UnityEngine.Object;

namespace Yagir.inc.HierarchyIcons.Editor
{
    static class TypeIconCache
    {
        static readonly Dictionary<Type, GUIContent> cache = new();

        public static GUIContent Get(Type t, Component instanceIfYouHaveIt = null)
        {
            if (cache.TryGetValue(t, out var gc) && gc != null && gc.image != null)
                return gc;

            var content = EditorGUIUtility.ObjectContent(instanceIfYouHaveIt, t);
            var tex = content?.image;

            if (tex == null)
                tex = AssetPreview.GetMiniTypeThumbnail(t);

            gc = new GUIContent(tex);
            cache[t] = gc;
            return gc;
        }
    }

    [InitializeOnLoad]
    class HierarchyIcons
    {
        private static IconsList list;

        static HierarchyIcons()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }

        private static readonly Vector2 offset = new Vector2(18, 0);
        private static readonly float multiIconStep = 18f;

        private static readonly Dictionary<string, GUIContent> iconsNames = new Dictionary<string, GUIContent>();
    
        private static GUIContent tmpText;
        private static Dictionary<Transform, bool> openedFolders = new Dictionary<Transform, bool>(); 

        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            Object obj = EditorUtility.InstanceIDToObject(instanceID);

            switch (Event.current.type)
            {
                case EventType.Repaint:
                    Repaint(obj, selectionRect);
                    break;
            }
            
            if (Event.current.type == EventType.Layout)
            {
                openedFolders = new Dictionary<Transform, bool>();
                CheckFolders(obj);
            }
        }
        
        public static void CheckFolders(Object obj)
        {
            if (obj != null)
            {
                var go = (obj as GameObject);
                var parent = go.transform.parent;
                if (parent != null && parent.GetComponent<Folder>())
                {
                    openedFolders.TryAdd(parent, true);
                }
            }
        }

        private static bool IsManager(string name)
        {
            for (var i = 0; i < list.gameManagerIconFor.Count; i++)
            {
                if (HasToken(name, list.gameManagerIconFor[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Repaint(Object obj, Rect selectionRect)
        {
            if (CreateSingleton())
            {
                if (obj != null)
                {
                    GameObject gameObject = obj as GameObject;
                
                    selectionRect.position += offset;

                    Rect iconPos = GetIconPos(selectionRect);
                    if (gameObject == null) return;
                
                    if (gameObject.TryGetComponent(out Light light))
                        GUI.Label(iconPos, DrawLightIcon(light.type));
                    else if (IsManager(gameObject.name))
                        GUI.Label(iconPos, GameManagerIcon);
                    else if (gameObject.GetComponent<TMP_Text>())
                    {
                        iconPos.size = Vector2.one * 18;
                        iconPos.position += new Vector2(3, 2.5f);
                        GUI.Label(iconPos, tmpText);
                    }else
                    if (gameObject.GetComponent<Folder>())
                    {
                        RepaintFolders(gameObject,iconPos);
                    }
                    else
                        RepaintComponent(gameObject, iconPos);
                }
            }
        }
        private static GUIContent GetCachedComponentIcon(Component cmp)
        {
            if (cmp == null) return null;

            var t = cmp.GetType();
            string key = t.FullName;

            if (!iconsNames.TryGetValue(key, out var gc) || gc == null || gc.image == null)
            {
                gc = TypeIconCache.Get(t, cmp);
                if (gc == null || gc.image == null) return null;

                iconsNames[key] = gc;
            }

            return gc;
        }
    
        static bool HasToken(string name, string token) => name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;

        private static void RepaintComponent(GameObject go, Rect iconPos)
        {
            int max = Mathf.Clamp(list.iconsCount, 1, 3);
            int drawn = 0;

            iconPos.size = Vector2.one * 20;
            iconPos.position -= Vector2.down * 2.5f;
            iconPos.position += Vector2.right * 2f;

            for (int i = 0; i < list.icons.Count && drawn < max; i++)
            {
                string typeName = list.icons[i];

                var cmp = go.GetComponent(typeName);
                if (!cmp) continue;

                GUIContent icon = (cmp is Folder)
                    ? FolderIcon
                    : GetCachedComponentIcon(cmp);

                if (icon == null || icon.image == null) continue;

                Rect r = iconPos;
                r.position += Vector2.left * (multiIconStep * drawn);

                GUI.Label(r, icon);
                drawn++;
            }
        }
        
        private static bool CreateSingleton()
        {
            if (list == null)
            {
                string[] find = AssetDatabase.FindAssets("t:IconsList");
            
                if (find.Length != 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(find[0]);
                
                    list = AssetDatabase.LoadAssetAtPath<IconsList>(path);

                    string[] tmptextIcons = AssetDatabase.FindAssets("TMP - Text Component Icon");
                
                    if (tmptextIcons.Length != 0)
                    {
                        Texture2D tmpIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(tmptextIcons[0]));
                        tmpText = new GUIContent(tmpIcon);
                    }
                
                }
            }

            return list != null;
        }

        private static readonly GUIContent DirLightIcon = EditorGUIUtility.IconContent("d_DirectionalLight Icon");
        private static readonly GUIContent SpotLightIcon = EditorGUIUtility.IconContent("d_Spotlight Icon");
        private static readonly GUIContent LightIcon     = EditorGUIUtility.IconContent("d_Light Icon");
        private static readonly GUIContent GameManagerIcon     = EditorGUIUtility.IconContent("GameManager Icon");
        private static readonly GUIContent FolderIcon     = EditorGUIUtility.IconContent("d_Folder Icon");

        private static GUIContent DrawLightIcon(LightType t) => t switch
        {
            LightType.Directional => DirLightIcon,
            LightType.Spot => SpotLightIcon,
            _ => LightIcon
        };

        private static Rect GetIconPos(Rect selectionRect) 
        {
            Rect iconPos = selectionRect;
            iconPos.position += new Vector2(selectionRect.width / 1.25f, 0);
            iconPos.position -= Vector2.up * 5;
            iconPos.size = new Vector2(25, 25);

            return iconPos;
        }
        
        public static void RepaintFolders(GameObject go, Rect iconPos)
        {
            GUIContent folder;
            if (go.transform.childCount == 0)
            {
                folder = EditorGUIUtility.IconContent("d_FolderEmpty Icon");
                GUI.Label(iconPos, folder);
            }
            else
            {
                if (openedFolders.ContainsKey(go.transform))
                {
                    folder = EditorGUIUtility.IconContent("Folder On Icon");
                    GUI.Label(iconPos, folder);
                }
                else
                {
                    folder = EditorGUIUtility.IconContent("Folder On Icon");
                    GUI.Label(iconPos, folder);
                    folder = EditorGUIUtility.IconContent("Folder Icon");
                    GUI.Label(new Rect(iconPos.position.x+ 1, iconPos.position.y + 1, iconPos.size.x, iconPos.size.y), folder);
                
                }
            }

        }
    }
}