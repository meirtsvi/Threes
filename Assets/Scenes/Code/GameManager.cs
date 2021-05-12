using System;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class GameManager : Agent
{
    public GameObject squarePrefab;

    private const int WIDTH = 4;
    private const int HEIGHT = 4;

    private const int IMPOSSIBLE_MOVE = -1;

    public enum Direction
    {
        Up,
        Down,
        Left,
        Right,
        DirectionsCount
    }

    private int[,] board = new int[HEIGHT, WIDTH];
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

    private int score;
    private Text scoreObj;
    private Text highScoreObj;
    private Text highestNMovesObj;
    private Text highestRankObj;
    private GameObject scoreContainer;
    private GameObject highScoreContainer;
    private GameObject highestNMovesContainer;
    private GameObject highestRankContainer;

    private GameObject restartButton;

    private int highScore;
    private int nTimesHighScoreAchieved;
    private int highestNMoves;
    private int highestRank;
    private int nTimeHighestRankAchieved;

    private List<int[,]> bestPlayMoves;
    private float lastReward;

    public void Restart()
    {
        Init();
    }

    private void DumpResults()
    {
        File.WriteAllText(@"c:\src\threes\ml_stats.txt",
            "High Score: " + highScore + ", in number of moves: " + numMoves + ", achieved " + nTimesHighScoreAchieved + " times. " +
            "Highest number of moves: " + highestNMoves + ". " +
            "Highest Rank: " + highestRank + " achieved " + nTimeHighestRankAchieved + " times");
    }
    private void Init()
    {
        highScoreObj = highScoreContainer.GetComponent<Text>();
        if (score > highScore)
        {
            nTimesHighScoreAchieved = 1;
            highScore = score;
            highScoreObj.text = AddThousandsSeparator(highScore.ToString()) + " (" + numMoves + ", 1)";
            DumpResults();
        }
        else if (score == highScore)
        {
            nTimesHighScoreAchieved++;
            highScoreObj.text = AddThousandsSeparator(highScore.ToString()) + " (" + numMoves + ", " + nTimesHighScoreAchieved + ")";
            DumpResults();
        }

        highestNMovesObj = highestNMovesContainer.GetComponent<Text>();
        if (numMoves > highestNMoves)
        {
            highestNMoves = numMoves;
            highestNMovesObj.text = highestNMoves.ToString();
            DumpResults();
            DumpBestPlay();
        }

        int curHighestRank = GetHighestRank();
        highestRankObj = highestRankContainer.GetComponent<Text>();
        if (curHighestRank > highestRank)
        {
            nTimeHighestRankAchieved = 1;
            highestRank = curHighestRank;
            highestRankObj.text = highestRank.ToString() + " (" + nTimeHighestRankAchieved + ")";
            DumpResults();
        }
        else if (curHighestRank == highestRank)
        {
            nTimeHighestRankAchieved++;
            highestRankObj.text = highestRank.ToString() + " (" + nTimeHighestRankAchieved + ")";
            DumpResults();
        }

        gameOverObj.SetActive(false);
        gameOver = false;
        restartButton.GetComponent<Button>().onClick.AddListener(Restart);

        scoreObj = scoreContainer.GetComponent<Text>();
        score = 0;

        numMoves = 0;
        nextValueManager = new NextValueManager();

        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                board[y, x] = 0;
            }
        }

        FillInitialBoard();
        UpdateBoard();
        UpdateScore();
        nextValues = GenerateNextValue();

        bestPlayMoves.Clear();
    }
    void Start()
    {
        Application.runInBackground = true;
        highScore = 0;
        highestNMoves = 0;
        highestRank = 0;

        gameOverObj = GameObject.Find("GameOver");
        scoreContainer = GameObject.Find("Score");
        highScoreContainer = GameObject.Find("HighScore");
        highestNMovesContainer = GameObject.Find("HighestNMoves");
        highestRankContainer = GameObject.Find("HighestRank");

        restartButton = GameObject.Find("RestartButton");

        redColor = Resources.Load<Material>("Materials/RedMaterial");
        blueColor = Resources.Load<Material>("Materials/BlueMaterial");
        whiteColor = Resources.Load<Material>("Materials/WhiteMaterial");

        GameObject canvasContainer = GameObject.Find("Canvas");
        canvas = canvasContainer.GetComponent<Canvas>();
        canvasRt = (RectTransform)squarePrefab.transform;

        bestPlayMoves = new List<int[,]>();

        CreateBoard();
        CreateNextBox();
        Init();
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

    private int GetNumberOfEmptyCells()
    {
        int nEmptyCells = 0;
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                if (board[y, x] == 0)
                    nEmptyCells++;
            }
        }
        return nEmptyCells;        
    }

    private int GetHighestRank()
    {
        int highestRank = NextValueManager.GetRank(board[0, 0]);
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                int curRank = NextValueManager.GetRank(board[y, x]);
                if (curRank > highestRank)
                    highestRank = curRank;
            }
        }

        return highestRank;
    }
    private List<int> GenerateNextValue()
    {
        int highestRank = GetHighestRank();
        List<int> ret = nextValueManager.PredictFuture(numMoves, highestRank);

        RectTransform rt = nextBox.GetComponent<RectTransform>();
        Text text = nextBox.transform.Find("Text").GetComponent<Text>();
        text.fontSize = 50;
        rt.sizeDelta = new Vector2(nextBoxOrigialWidth, rt.rect.height);
        if (ret.Count > 1)
        {
            text.fontSize = 25;
            rt.sizeDelta = new Vector2(nextBoxOrigialWidth * 2, rt.rect.height);
        }
        RenderNextCell(nextBox, ret);
        return ret;
    }

    void Update()
    {
        Direction direction = Direction.DirectionsCount;

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

        if (direction != Direction.DirectionsCount)
        {
            MakeMove(direction);
        }
    }

    public bool MakeMove(Direction direction)
    {
        if (direction == Direction.DirectionsCount)
            return false;

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

        bestPlayMoves.Add((int[,])board.Clone());

        return true;
    }

    private void UpdateScore()
    {
        int curScore = 0;
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                int value = board[y, x];
                if (value < 3)
                    continue;

                int rank = NextValueManager.GetRank(value);
                if (rank >= 1)
                {
                    curScore += (int)Mathf.Pow(3f, (float)rank);
                }
            }
        }

        score = curScore;
        scoreObj.text = AddThousandsSeparator(score.ToString());
    }

    private static string AddThousandsSeparator(string scoreText)
    {
        if (scoreText.Length > 3)
        {
            for (int i = scoreText.Length - 3; i > 0; i -= 3)
            {
                scoreText = scoreText.Substring(0, i) + "," + scoreText.Substring(i);
            }
        }

        return scoreText;
    }

    private void AddNextValueToBoard(Direction direction)
    {
        int random = UnityEngine.Random.Range(0, WIDTH);
        int nextValue = nextValues[UnityEngine.Random.Range(0, nextValues.Count)];

        switch (direction)
        {
            case Direction.Up:
                while (board[HEIGHT - 1, random] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                board[HEIGHT - 1, random] = nextValue;
                break;
            case Direction.Down:
                while (board[0, random] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                board[0, random] = nextValue;
                break;
            case Direction.Left:
                while (board[random, WIDTH - 1] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                board[random, WIDTH - 1] = nextValue;
                break;
            case Direction.Right:
                while (board[random, 0] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                board[random, 0] = nextValue;
                break;
        }
        UpdateBoard();
    }

    private void ApplyMove(Direction direction, int[] offsets)
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
                for (int y = 0; y < HEIGHT; y++)
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
                    for (int x = offsets[y]; x >= 0; x--)
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
        int targetValue = board[targetY, targetX];
        int sourceValue = board[y, x];
        if (targetValue == 0)
        {
            board[targetY, targetX] = board[y, x];
        }
        else
        {
            if ((targetValue == 1 && sourceValue == 2) ||
                (targetValue == 2 && sourceValue == 1))
            {
                board[targetY, targetX] = 3;
            }
            else
            {
                if (targetValue == sourceValue)
                {
                    board[targetY, targetX] = targetValue * 2;
                }
            }
        }
        board[y, x] = 0;
    }

    private void FillInitialBoard()
    {
        int x, y;
        for (int i = 0; i < 9; i++)
        {
            do
            {
                x = UnityEngine.Random.Range(0, WIDTH);
                y = UnityEngine.Random.Range(0, HEIGHT);
            } while (board[y, x] != 0);
            board[y, x] = nextValueManager.numbers.GetNext();
        }
    }

    private void UpdateBoard()
    {
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                GameObject currCell = ObjectBoard[y, x];
                int number = board[y, x];
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
                color = blueColor;
                textObject.color = Color.white;
                break;
            case 2:
                color = redColor;
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
                color = blueColor; ;
                textObject.color = Color.white;
                break;
            case 2:
                color = redColor;
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
        int from = board[yFrom, xFrom];
        int to = board[yTo, xTo];

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
                        if (CheckCellMove(x, y, x, y - 1))
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
                    for (int y = HEIGHT - 2; y >= 0; y--)
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
                        if (CheckCellMove(x, y, x - 1, y))
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
                    for (int x = WIDTH - 2; x >= 0; x--)
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

        for (int i = 0; i < WIDTH; i++)
        {
            if (possibleMoves[i] != IMPOSSIBLE_MOVE)
                return true;
        }
        return false;
    }

    static float step_reward = 0.003f;
    public override void OnEpisodeBegin()
    {
        Init();
        step_reward = 0.003f;
    }

    private int NormalizeValue(int value)
    {
        switch (value)
        {
            case 0:
            case 1:
            case 2:
                return value;
            default:
                return NextValueManager.GetRank(value) + 2;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                int cellValue = board[y, x];
                sensor.AddOneHotObservation(NormalizeValue(cellValue), 15);
            }
        }

        sensor.AddOneHotObservation(NormalizeValue(nextValues[0]), 15);
        if (nextValues.Count > 1)
        {
            sensor.AddOneHotObservation(NormalizeValue(nextValues[1]), 15);
            if (nextValues.Count > 2)
            {
                sensor.AddOneHotObservation(NormalizeValue(nextValues[2]), 15);
            }
            else
            {
                sensor.AddOneHotObservation(NormalizeValue(nextValues[1]), 15);
            }
        }
        else
        {
            sensor.AddOneHotObservation(NormalizeValue(nextValues[0]), 15);
            sensor.AddOneHotObservation(NormalizeValue(nextValues[0]), 15);
        }
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        int prevHighestRank = GetHighestRank();
        int nEmptyCellsBeforeMove = GetNumberOfEmptyCells();
        Direction direction = (Direction)vectorAction[0];
        if (!MakeMove(direction))
        {
            // Probably due to a bug, the CollectDiscreteActionMasks is not always called before OnActionReceived
            // so there is a chance that vectionAction will require to make impossible move.
            // Don't change reward or end episode, simply exit in the hope that the brain WILL call CollectDiscreteActionMasks
            // on the next round before calling OnActionReceived.
            return;
        }

        int nEmptyCellsAfterMove = GetNumberOfEmptyCells();
        int emptyCellsDiff = nEmptyCellsAfterMove - nEmptyCellsBeforeMove;

        if (emptyCellsDiff < -1)
        {
            AddReward(-0.002f);
        }
        else if (emptyCellsDiff == -1)
        {
            AddReward(-0.001f);
        }
        else if (emptyCellsDiff == 1)
        {
            AddReward(0.001f);
        }
        else if (emptyCellsDiff > 1)
        {
            AddReward(0.003f);
        }

        if (GetHighestRank() > prevHighestRank)
        {
            AddReward(0.02f);
        }

        if (gameOver)
        {
            MarkGameOver();
            return;
        }

        if (GetHighestRank() == 8)
        {
            SetReward(1.0f);
            EndEpisode();
            return;
        }

        AddReward(step_reward);
        step_reward += 0.00001f;
    }

    private void MarkGameOver()
    {
        lastReward = GetCumulativeReward();
        SetReward(-1.0f);
        EndEpisode();
    }

    public override void CollectDiscreteActionMasks(DiscreteActionMasker actionMasker)
    {
        List<int> impossibleMoves = new List<int>();
        int[] possibleMoves;

        for (Direction i = Direction.Up; i < Direction.DirectionsCount; i++)
        {
            if (!CheckMove(i, out possibleMoves))
                impossibleMoves.Add((int)i);
        }

        actionMasker.SetMask(0, impossibleMoves);

        if (impossibleMoves.Count == (int)Direction.DirectionsCount)
            MarkGameOver();
    }

    private void DumpBestPlay()
    {
        String filename = @"c:\src\threes\best_play.txt";
        File.WriteAllText(filename, "Score: " + this.score + ", Num Moves: " + this.numMoves + ", Reward: " +lastReward.ToString("0.00"));

        int turn = 1;
        foreach (int[,] play in this.bestPlayMoves)
        {
            File.AppendAllText(filename, "\nturn " + turn + "\n");
            for (int y = 0; y < HEIGHT; y++)
            {
                String line = "";
                for (int x = 0; x < WIDTH; x++)
                {
                    line += play[y, x] + ", ";
                }
                File.AppendAllText(filename, line + "\n");
            }
            turn++;
        }
    }
}
