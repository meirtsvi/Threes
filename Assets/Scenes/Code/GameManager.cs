using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public GameObject squarePrefab;
    
    private const int WIDTH = 4;
    private const int HEIGHT = 4;

    private const int IMPOSSIBLE_MOVE = -1;

    public enum Direction
    {
        Invalid,
        Up,
        Down,
        Left,
        Right
    }

    private int[,] Board = new int[HEIGHT, WIDTH];
    private GameObject[,] ObjectBoard = new GameObject[HEIGHT, WIDTH];

    int numMoves;
    private GameObject nextBox;
    private float nextBoxOrigialWidth;
    private NextValueManager nextValueManager;
    private List<int> nextValues;

    private Material redColor, blueColor, whiteColor;

    private Canvas canvas;
    private RectTransform canvasRt;

    private GameObject gameOverObj;
    private bool gameOver;

    private Text scoreObj;

    void Start()
    {
        gameOverObj = GameObject.Find("GameOver");
        gameOverObj.SetActive(false);
        gameOver = false;

        GameObject scoreContainer = GameObject.Find("Score");
        scoreObj = scoreContainer.GetComponent<Text>();

        numMoves = 0;
        nextValueManager = new NextValueManager();
        GameObject canvasContainer = GameObject.Find("Canvas");
        canvas = canvasContainer.GetComponent<Canvas>();
        canvasRt = (RectTransform)squarePrefab.transform;

        for (int y=0; y<HEIGHT; y++)
        {
            for (int x=0; x<WIDTH; x++)
            {
                Board[y, x] = 0;
            }
        }
        redColor = Resources.Load<Material>("Materials/RedMaterial");
        blueColor = Resources.Load<Material>("Materials/BlueMaterial");
        whiteColor = Resources.Load<Material>("Materials/WhiteMaterial");
        
        CreateBoard();
        SetInitialBoard();
        UpdateBoard();

        CreateNextBox();
        nextValues = GenerateNextValue();
    }

    private void CreateNextBox()
    {
        GameObject newSquare = Instantiate(squarePrefab, new Vector3(0, 0, 0), Quaternion.identity, canvas.transform);
        RectTransform rt = newSquare.GetComponent<RectTransform>();
        nextBoxOrigialWidth = rt.rect.width;
        newSquare.transform.localPosition = new Vector3(100, -50, 0);
        Text textObject = newSquare.transform.Find("Text").GetComponent<Text>();
        textObject.transform.localPosition = new Vector3(-0.5f, -0.5f, 0);
        nextBox = newSquare;
    }

    private List<int> GenerateNextValue()
    {
        int highestRank = NextValueManager.GetRank(Board[0, 0]);
        for (int y=0; y<HEIGHT; y++)
        {
            for (int x=0; x<WIDTH; x++)
            {
                int curRank = NextValueManager.GetRank(Board[y, x]);
                if (curRank > highestRank)
                    highestRank = curRank;
            }
        }

        List<int> ret = nextValueManager.PredictFuture(numMoves, highestRank);

        RectTransform rt = nextBox.GetComponent<RectTransform>();
        Text text = nextBox.transform.Find("Text").GetComponent<Text>();
        text.fontSize = 50;
        rt.sizeDelta = new Vector2(nextBoxOrigialWidth, rt.rect.height);
        if (ret.Count > 1)
        {
            text.fontSize = 25;
            rt.sizeDelta = new Vector2(nextBoxOrigialWidth*2, rt.rect.height);
        }
        RenderNextCell(nextBox, ret);
        return ret;
    }

    void Update()
    {
        Direction direction = Direction.Invalid;

        if (gameOver)
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            direction = Direction.Left;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            direction = Direction.Right;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            direction = Direction.Down;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            direction = Direction.Up;
        }

        if (direction != Direction.Invalid)
        {
            MakeMove(direction);
        }
    }

    public bool MakeMove(Direction direction)
    {
        int[] possibleMoves;
        if (!CheckMove(direction, out possibleMoves))
            return false;

        this.numMoves++;
        ApplyMove(direction, possibleMoves);
        AddNextValueToBoard(direction);
        nextValues = GenerateNextValue();
        UpdateScore();

        if (!CheckMove(Direction.Up, out possibleMoves) &&
            !CheckMove(Direction.Down, out possibleMoves) &&
            !CheckMove(Direction.Left, out possibleMoves) &&
            !CheckMove(Direction.Right, out possibleMoves))
        {
            gameOver = true;
            gameOverObj.SetActive(true);
        }

        return true;
    }

    private void UpdateScore()
    {
        int score = 0;
        for (int y=0; y<HEIGHT; y++)
        {
            for (int x=0; x<WIDTH; x++)
            {
                int value = Board[y, x];
                if (value < 3)
                    continue;

                int rank = NextValueManager.GetRank(value);
                if (rank >= 1)
                {
                    score += (int)Mathf.Pow(3f, (float)rank);
                }
            }
        }

        String scoreText = score.ToString();
        if (scoreText.Length > 3)
        {
            for (int i=scoreText.Length-3; i>0; i-=3)
            {
                scoreText = scoreText.Substring(0, i) + "," + scoreText.Substring(i);
            }
        }
        scoreObj.text = scoreText;
    }

    private void AddNextValueToBoard(Direction direction)
    {
        int random = UnityEngine.Random.Range(0, WIDTH);
        int nextValue = nextValues[UnityEngine.Random.Range(0, nextValues.Count)];

        switch (direction)
        {
            case Direction.Up:
                while (Board[HEIGHT - 1, random] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                Board[HEIGHT - 1, random] = nextValue;
                break;
            case Direction.Down:
                while (Board[0, random] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                Board[0, random] = nextValue;
                break;
            case Direction.Left:
                while (Board[random, WIDTH-1] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                Board[random, WIDTH - 1] = nextValue;
                break;
            case Direction.Right:
                while (Board[random, 0] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                Board[random, 0] = nextValue;
                break;
        }
        UpdateBoard();
    }

    private void ApplyMove(Direction direction, int []offsets)
    {
        switch (direction)
        {
            case Direction.Up:
                for (int x = 0; x < WIDTH; x++)
                {
                    if (offsets[x] == IMPOSSIBLE_MOVE)
                    {
                        continue;
                    }
                    for (int y = offsets[x]; y < HEIGHT; y++)
                    {
                        SetMoveResult(direction, x, y);
                    }
                }
                break;
            case Direction.Down:
                for (int x = 0; x < WIDTH; x++)
                {
                    if (offsets[x] == IMPOSSIBLE_MOVE)
                    {
                        continue;
                    }
                    for (int y = offsets[x]; y >= 0; y--)
                    {
                        SetMoveResult(direction, x, y);
                    }
                }
                break;
            case Direction.Left:
                for (int y=0; y<HEIGHT; y++)
                {
                    if (offsets[y] == IMPOSSIBLE_MOVE)
                    {
                        continue;
                    }
                    for (int x = offsets[y]; x < WIDTH; x++)
                    {
                        SetMoveResult(direction, x, y);
                    }
                }
                break;
            case Direction.Right:
                for (int y = 0; y < HEIGHT; y++)
                {
                    if (offsets[y] == IMPOSSIBLE_MOVE)
                    {
                        continue;
                    }
                    for (int x = offsets[y]; x >=0; x--)
                    {
                        SetMoveResult(direction, x, y);
                    }
                }
                break;
        }
        UpdateBoard();
    }

    private void SetMoveResult(Direction direction, int x, int y)
    {
        int targetX, targetY;
        switch (direction)
        {
            case Direction.Up:
                targetX = x;
                targetY = y - 1;
                break;
            case Direction.Down:
                targetX = x;
                targetY = y + 1;
                break;
            case Direction.Left:
                targetX = x - 1;
                targetY = y;                     
                break;
            case Direction.Right:
                targetX = x + 1;
                targetY = y;
                break;
            default:
                throw new Exception("Should not get here");
        }

        if (targetX < 0 || targetY < 0)
        {
            x = 1;
        }
        int targetValue = Board[targetY, targetX];
        int sourceValue = Board[y, x];
        if (targetValue == 0)
        {
            Board[targetY, targetX] = Board[y, x];
        }
        else
        {
            if ((targetValue == 1 && sourceValue == 2) ||
                (targetValue == 2 && sourceValue == 1))
            {
                Board[targetY, targetX] = 3;
            }
            else
            {
                if (targetValue == sourceValue)
                {
                    Board[targetY, targetX] = targetValue * 2;
                }
            }
        }
        Board[y, x] = 0;
    }

    private void SetInitialBoard()
    {
        int x, y;
        for (int i=0; i<9; i++)
        {
            do
            {
                x = UnityEngine.Random.Range(0, WIDTH);
                y = UnityEngine.Random.Range(0, HEIGHT);
            } while (Board[y, x] != 0);
            Board[y, x] = UnityEngine.Random.Range(1, 4);
        }
    }

    private void UpdateBoard()
    {
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                GameObject currCell = ObjectBoard[y, x];
                int number = Board[y, x];
                RenderCell(currCell, number);
            }
        }
    }

    private void RenderCell(GameObject cell, int value)
    {
        Text textObject = cell.transform.Find("Text").GetComponent<Text>();
        textObject.text = value.ToString();

        Material color;
        switch (value)
        {
            case 0:
                color = whiteColor;
                textObject.text = "";
                break;
            case 1:
                color = redColor;
                textObject.color = Color.white;
                break;
            case 2:
                color = blueColor;
                textObject.color = Color.white;
                break;
            default:
                color = whiteColor;
                textObject.color = Color.red;
                break;
        }
        cell.GetComponent<Image>().material = color;
    }

    private void RenderNextCell(GameObject cell, List<int> nextValues)
    {
        Text textObject = cell.transform.Find("Text").GetComponent<Text>();
        if (nextValues.Count == 1 && nextValues[0] >= 1 && nextValues[0] <= 3)
            textObject.text = "";
        else
            textObject.text = String.Join(",", nextValues);

        Material color;
        switch (nextValues[0])
        {
            case 1:
                color = redColor;
                textObject.color = Color.white;
                break;
            case 2:
                color = blueColor;
                textObject.color = Color.white;
                break;
            default:
                color = whiteColor;
                textObject.color = Color.red;
                break;
        }
        cell.GetComponent<Image>().material = color;
    }

    private void DrawCell(int x, int y)
    {
        float squareWidth = canvasRt.rect.width;
        float squareHeight = canvasRt.rect.height;
        float zeroX = ((-1) * canvas.pixelRect.width / 2) + (squareWidth / 2);
        float zeroY = ((-1) * canvas.pixelRect.height / 2) + (squareHeight / 2);
        float xPos = zeroX + squareWidth * x;
        float yPos = zeroY + squareHeight * y;
        GameObject newSquare = Instantiate(squarePrefab, new Vector3(0, 0, 0), Quaternion.identity, canvas.transform);
        newSquare.transform.localPosition = new Vector3(xPos, yPos, 0);
        Text textObject = newSquare.transform.Find("Text").GetComponent<Text>();
        textObject.transform.localPosition = new Vector3(-0.5f, -0.5f, 0);
        ObjectBoard[y, x] = newSquare;
    }

    public void CreateBoard()
    {
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                DrawCell(x, y);
            }
        }
    }

    private bool CheckCellMove(int xFrom, int yFrom, int xTo, int yTo)
    {
        int from = Board[yFrom, xFrom];
        int to = Board[yTo, xTo];

        if (to == 0)
            return true;

        if ((from == 1 && to == 2) ||
            (from == 2 && to == 1) ||
            (from > 2 && to > 2 && from == to))
            return true;

        return false;
    }

    private bool CheckMove(Direction direction, out int[] possibleMoves)
    {
        //assert(WIDTH == HEIGHT);
        possibleMoves = new int[WIDTH];
        for (int i = 0; i < WIDTH; i++)
            possibleMoves[i] = IMPOSSIBLE_MOVE;

        switch (direction)
        {
            case Direction.Up:
                for (int x = 0; x < WIDTH; x++)
                {
                    for (int y = 1; y < HEIGHT; y++)
                    {
                        if (CheckCellMove(x, y, x, y-1))
                        {
                            possibleMoves[x] = y;
                            break;
                        }
                    }
                }
                break;
            case Direction.Down:
                for (int x = 0; x < WIDTH; x++)
                {
                    for (int y = HEIGHT-2; y >= 0; y--)
                    {
                        if (CheckCellMove(x, y, x, y + 1))
                        {
                            possibleMoves[x] = y;
                            break;
                        }
                    }
                }
                break;
            case Direction.Left:
                for (int y = 0; y < HEIGHT; y++)
                {
                    for (int x = 1; x < WIDTH; x++)
                    {
                        if (CheckCellMove(x, y, x-1, y))
                        {
                            possibleMoves[y] = x;
                            break;
                        }
                    }
                }
                break;
            case Direction.Right:
                for (int y = 0; y < HEIGHT; y++)
                {
                    for (int x = WIDTH-2; x >= 0; x--)
                    {
                        if (CheckCellMove(x, y, x + 1, y))
                        {
                            possibleMoves[y] = x;
                            break;
                        }
                    }
                }
                break;
            default:
                throw new Exception("");
        }

        for (int i=0; i<WIDTH; i++)
        {
            if (possibleMoves[i] != IMPOSSIBLE_MOVE)
                return true;
        }
        return false;
    }
}
