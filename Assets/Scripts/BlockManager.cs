using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public Block[] blocks;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public int FillBlock()
    {
        for(int i = 0; i < blocks.Length; i++)
        {
            Block block = blocks[i];
            if (!block.HasCurrentFood())
            {
                return i;
            }
        }
        Debug.Log("All blocks are filled!");
        return -1;
    }
    public Block[] GetBlocks()
    {
        return blocks;
    }
}
