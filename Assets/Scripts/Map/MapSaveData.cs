using System;
using System.Collections.Generic;

namespace MergesChef.Map
{
    [Serializable]
    public class SavedAccessory
    {
        public string type;
        public string itemName;
        public int x;
        public int y;
        public int level;
        public bool isFlipped;
    }

    [Serializable]
    public class SavedArea
    {
        public string type;
        public int x;
        public int y;
        public int level;
        public int areaTypeEnum;
        public List<SavedAccessory> accessories = new List<SavedAccessory>();
    }

    [Serializable]
    public class MapSaveData
    {
        public List<SavedArea> areas = new List<SavedArea>();
    }
}
