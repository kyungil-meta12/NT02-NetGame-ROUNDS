using System.Collections.Generic;
using UnityEngine;

namespace Yagir.inc.HierarchyIcons.Scripts
{
    [CreateAssetMenu(fileName = "", menuName = "Editor/Icons List", order = 1)]
    public class IconsList : ScriptableObject
    {
        [Range(1, 3)]
        public int iconsCount;
        public List<string> gameManagerIconFor = new List<string>();
        public List<string> icons = new List<string>();
    }
}