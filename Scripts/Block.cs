using TMPro;
using UnityEngine;

public class Block : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private TextMeshPro _countText;
    [HideInInspector] public Node Node;
    [HideInInspector] public Block MergingBlock;

    [HideInInspector] public int Value;
    [HideInInspector] public bool IsMerging;

    [HideInInspector] public Vector2 Position => transform.position;

    public void Initialize(BlockType typeBlock)
    {
        Value = typeBlock.Value;
        _spriteRenderer.color = typeBlock.Color;
        _countText.text = typeBlock.Value.ToString();
    }

    public void SetBlock(Node node)
    {
        if (Node != null) Node.OccupiedBlock = null;

        Node = node;

        Node.OccupiedBlock = this;
    }

    public void MergeBlock(Block blockToMergin)
    {
        // Set the block we are merging with
        MergingBlock = blockToMergin;

        Node.OccupiedBlock = null;

        blockToMergin.IsMerging = true;
    }

    public bool CanMerge(int value) => value == Value && !IsMerging && MergingBlock == null; 
}
