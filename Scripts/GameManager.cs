using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Settings Grid")]
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;

    [Header("Prefabs")]
    [SerializeField] private Node _nodePrefab;
    [SerializeField] private Block _blockPrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    private float _bordersBoard = 0.5f;

    [Header("Block Settings")]
    [SerializeField] private List<BlockType> _typesBlock;
    private BlockType GetBlockTypeByValue(int value) => 
        _typesBlock.First(t => t.Value == value);

    [Header("Other Prefabs")]
    [SerializeField] private GameObject _winScreen;
    [SerializeField] private GameObject _loseScreen;

    private List<Node> _nodes;
    private List<Block> _blocks;

    private GameState _state;

    private int _round;
    private float _travelTime = 0.2f;
    private int _winCondition = 2048;


    private void Start()
    {
        ChangeGameState(GameState.GenerateLevel);
    }

    private void Update()
    {
        InputManager();
    }

    Node GetNodeAtPosition(Vector2 position)
    {
        return _nodes.FirstOrDefault(n => n.Position == position);
    }

    private void ChangeGameState(GameState newState)
    {
        _state = newState;

        switch(newState)
        {
            case GameState.GenerateLevel:
                GenerateGrid();
                break;

            case GameState.SpawningBlocks:
                SpawnBlocks(_round++ == 0 ? 2 : 1);
                break;

            case GameState.WaitingInput:
                break;

            case GameState.Moving:
                break;

            case GameState.Win:
                _winScreen.SetActive(true);
                break;

            case GameState.Lose:
                _loseScreen.SetActive(true);
                break;

            default:
                break;
        }
    }

    private void InputManager()
    {
        if (_state != GameState.WaitingInput) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) Shift(Vector2.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) Shift(Vector2.right);
        if (Input.GetKeyDown(KeyCode.UpArrow)) Shift(Vector2.up);
        if (Input.GetKeyDown(KeyCode.DownArrow)) Shift(Vector2.down);
    }

    private void SpawnBlocks(int amount)
    {
        var freeNodes = _nodes
            .Where(n => n.OccupiedBlock == null)
            .OrderBy(b => UnityEngine.Random.value)
            .ToList();

        foreach (var node in freeNodes.Take(amount))
        {
            SpawnBlock(node, UnityEngine.Random.value > 0.8f ? 4 : 2);
        }

        if (freeNodes.Count() == 1)
        {
            ChangeGameState(GameState.Lose);
            return;
        }

        ChangeGameState(_blocks.Any(b => b.Value == _winCondition) ? GameState.Win : GameState.WaitingInput);
    }

    private void SpawnBlock(Node node, int value)
    {
        Block block = Instantiate(_blockPrefab, node.Position, Quaternion.identity);
        block.Initialize(GetBlockTypeByValue(value));
        block.SetBlock(node);
        _blocks.Add(block);
    }

    private void Shift(Vector2 direction)
    {
        ChangeGameState(GameState.Moving);

        var orderedBlocks = _blocks
            .OrderBy(b => b.Position.x)
            .ThenBy(b => b.Position.y)
            .ToList();

        if (direction == Vector2.right || direction == Vector2.up) orderedBlocks.Reverse();

        foreach (var block in orderedBlocks)
        {
            var next = block.Node;

            do
            {
                block.SetBlock(next);

                var possibleNode = GetNodeAtPosition(next.Position + direction);
                if (possibleNode != null)
                {
                    // If it possible to merge. set merge
                    if(possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value))
                    {
                        block.MergeBlock(possibleNode.OccupiedBlock);
                    }
                    else if (possibleNode.OccupiedBlock == null)
                    {
                        next = possibleNode;
                    }
                }

            } while (next != block.Node);
        }

        var sequence = DOTween.Sequence();

        foreach (var block in orderedBlocks)
        {
            var movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Position : block.Node.Position;

            sequence.Insert(0, block.transform.DOMove(movePoint, _travelTime));
        }

        sequence.OnComplete(() =>
        {
            foreach (var block in orderedBlocks.Where(b => b.MergingBlock != null))
            {
                MergeBlocks(block.MergingBlock , block);
            }

            ChangeGameState(GameState.SpawningBlocks);
        });
    }

    private void MergeBlocks(Block baseBlock, Block mergingBlock)
    {
        SpawnBlock(baseBlock.Node, baseBlock.Value * 2);

        RemoveBlock(baseBlock);
        RemoveBlock(mergingBlock);
    }

    private void RemoveBlock(Block block)
    {
        _blocks.Remove(block);
        Destroy(block.gameObject);
    }

    private void GenerateGrid()
    {
        _round = 0;

        _nodes = new List<Node>();
        _blocks = new List<Block>();

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Node node = Instantiate(_nodePrefab, new Vector2(x, y), Quaternion.identity);
                _nodes.Add(node);
            }
        }

        Vector2 centerPosition = new Vector2((float)_width / 2 - _bordersBoard, (float)_height / 2 - _bordersBoard);

        SpriteRenderer board = Instantiate(_boardPrefab, centerPosition, Quaternion.identity);
        board.size = new Vector2(_width, _height);

        ChangeGameState(GameState.SpawningBlocks);

        SetCameraPosition(centerPosition);
    }

    private void SetCameraPosition(Vector2 centerPosition)
    {
        Camera.main.transform.position = new Vector3(centerPosition.x, centerPosition.y, -10);
    }
}

[Serializable]
public struct BlockType
{
    public int Value;
    public Color Color;
}

public enum GameState
{
    GenerateLevel,
    SpawningBlocks,
    WaitingInput,
    Moving,
    Win,
    Lose
}
