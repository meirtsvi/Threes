using System;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private const int WIDTH = 4;
    private const int HEIGHT = 4;

    private const int IMPOSSIBLE_MOVE = -1;

    private enum Direction
    {
        Invalid,
        Up,
        Down,
        Left,
        Right
    }

    int[,] Board = new int[HEIGHT, WIDTH];
    GameObject[,] ObjectBoard = new GameObject[HEIGHT, WIDTH];

    public GameObject squarePrefab;
    private Material red_color, blue_color, white_color;

    void Start()
    {
        for (int y=0; y<HEIGHT; y++)
        {
            for (int x=0; x<WIDTH; x++)
            {
                Board[y, x] = 0;
            }
        }
        red_color = Resources.Load<Material>("Materials/RedMaterial");
        blue_color = Resources.Load<Material>("Materials/BlueMaterial");
        white_color = Resources.Load<Material>("Materials/WhiteMaterial");
        CreateBoard();
        SetInitialBoard();
        UpdateBoard();
   
    }

    void Update()
    {
        int[] res;
        Direction direction = Direction.Invalid;

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            direction = Direction.Left;
        } else if (Input.GetKeyDown(KeyCode.RightArrow))
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

        if (direction != Direction.Invalid && CheckMove(direction, out res))
        {
            Debug.Log(String.Format("Going {0} is possible", direction.ToString()));
            ApplyMove(direction, res);
            AddNewNumber(direction);
        }
    }

    private void AddNewNumber(Direction direction)
    {
        int random = UnityEngine.Random.Range(0, WIDTH);

        switch (direction)
        {
            case Direction.Up:
                while (Board[HEIGHT - 1, random] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                Board[HEIGHT - 1, random] = 3;
                break;
            case Direction.Down:
                while (Board[0, random] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                Board[0, random] = 3;
                break;
            case Direction.Left:
                while (Board[random, WIDTH-1] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                Board[random, WIDTH-1] = 3;
                break;
            case Direction.Right:
                while (Board[random, 0] != 0)
                {
                    random = UnityEngine.Random.Range(0, WIDTH);
                }
                Board[random, 0] = 3;
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
        int target_x, target_y;
        switch (direction)
        {
            case Direction.Up:
                target_x = x;
                target_y = y - 1;
                break;
            case Direction.Down:
                target_x = x;
                target_y = y + 1;
                break;
            case Direction.Left:
                target_x = x - 1;
                target_y = y;                     
                break;
            case Direction.Right:
                target_x = x + 1;
                target_y = y;
                break;
            default:
                throw new Exception("Should not get here");
        }

        if (target_x < 0 || target_y < 0)
        {
            x = 1;
        }
        int target_value = Board[target_y, target_x];
        int source_value = Board[y, x];
        if (target_value == 0)
        {
            Board[target_y, target_x] = Board[y, x];
        }
        else
        {
            if ((target_value == 1 && source_value == 2) ||
                (target_value == 2 && source_value == 1))
            {
                Board[target_y, target_x] = 3;
            }
            else
            {
                if (target_value == source_value)
                {
                    Board[target_y, target_x] = target_value * 2;
                }
            }
        }
        Board[y, x] = 0;
    }

    private void SetInitialBoard()
    {
        int x, y;
        for (int i=0; i<3; i++)
        {
            do
            {
                x = UnityEngine.Random.Range(0, WIDTH);
                y = UnityEngine.Random.Range(0, HEIGHT);
            } while (Board[y, x] != 0);
            Board[y, x] = 1;
        }
        for (int i = 0; i < 2; i++)
        {
            do
            {
                x = UnityEngine.Random.Range(0, WIDTH);
                y = UnityEngine.Random.Range(0, HEIGHT);
            } while (Board[y, x] != 0);
            Board[y, x] = 2;
        }

        Board[3, 0] = 24;
        Board[3, 1] = 12;
        Board[3, 2] = 3;
        Board[3, 3] = 3;

    }

    private void UpdateBoard()
    {
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                GameObject curr_cell = ObjectBoard[y, x];
                int number = Board[y, x];
                Text text_object = curr_cell.transform.Find("Text").GetComponent<Text>();
                text_object.text = number.ToString();

                Material color;
                switch (number)
                {
                    case 0:
                        color = white_color;
                        text_object.text = "";
                        break;
                    case 1:
                        color = red_color;
                        text_object.color = Color.white;
                        break;
                    case 2:
                        color = blue_color;
                        text_object.color = Color.white;
                        break;
                    default:
                        color = white_color;
                        text_object.color = Color.red;
                        break;
                }
                curr_cell.GetComponent<Image>().material = color;

            }
        }
    }
    private void DrawCell(int x, int y)
    {
        GameObject canvas_container = GameObject.Find("Canvas");
        Canvas canvas = canvas_container.GetComponent<Canvas>();
        RectTransform rt = (RectTransform)squarePrefab.transform;

        float squareWidth = rt.rect.width;
        float squareHeight = rt.rect.height;
        float zero_x = ((-1) * canvas.pixelRect.width / 2) + (squareWidth / 2);
        float zero_y = ((-1) * canvas.pixelRect.height / 2) + (squareHeight / 2);

        GameObject newSquare = Instantiate(squarePrefab, new Vector3(0, 0, 0), Quaternion.identity, canvas.transform);
        newSquare.transform.localPosition = new Vector3(zero_x + squareWidth*x, zero_y +squareHeight*y, 0);
        Text text_object = newSquare.transform.Find("Text").GetComponent<Text>();
        text_object.transform.localPosition = new Vector3(-0.5f, -0.5f, 0);
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

    private bool CheckCellMove(int x_from, int y_from, int x_to, int y_to)
    {
        int from = Board[y_from, x_from];
        int to = Board[y_to, x_to];

        if (to == 0)
            return true;

        if ((from == 1 && to == 2) ||
            (from == 2 && to == 1) ||
            (from > 2 && to > 2 && from == to))
            return true;

        return false;
    }

    private bool CheckMove(Direction direction, out int[] possible_moves)
    {
        //assert(WIDTH == HEIGHT);
        possible_moves = new int[WIDTH];
        for (int i = 0; i < WIDTH; i++)
            possible_moves[i] = IMPOSSIBLE_MOVE;

        switch (direction)
        {
            case Direction.Up:
                for (int x = 0; x < WIDTH; x++)
                {
                    for (int y = 1; y < HEIGHT; y++)
                    {
                        if (CheckCellMove(x, y, x, y-1))
                        {
                            possible_moves[x] = y;
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
                            possible_moves[x] = y;
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
                            possible_moves[y] = x;
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
                            possible_moves[y] = x;
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
            if (possible_moves[i] != IMPOSSIBLE_MOVE)
                return true;
        }
        return false;
    }
}
