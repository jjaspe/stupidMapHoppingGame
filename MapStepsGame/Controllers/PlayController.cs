using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace MapStepsGame.Controllers
{
    public enum Direction
    {
        Up = 1,
        Right = 2,
        Down = 3,
        Left = 4,
        None = 0
    }

    public enum Difficulty
    {
        Easy = 1,
        Medium = 2,
        Hard = 3,
        VeryHard = 4
    }

    public class Match
    {
        public Tuple<int, int> start { get; set; }
        public Tuple<int, int> end { get ; set ;}
        public int XSize, YSize;
        public List<int> possibleMoves = new List<int>();
        public List<Tuple<int, int>> forbidden = new List<Tuple<int, int>>();
        public Tuple<int, int> lastTry= new Tuple<int, int>(0,0);

        public bool TrySolve(List<Tuple<int,Direction>> moves)
        {
            Tuple<int, int> currentTuple = start;

            foreach(Tuple<int,Direction> move in moves)
            {
                currentTuple = makeMove(currentTuple, move);
                if (!IsPointValid(currentTuple))
                    return false;               
            }
            lastTry = currentTuple;
            return currentTuple.Item1 == end.Item1 && currentTuple.Item2 == end.Item2;         
        }

        public Tuple<int, int> makeMove(Tuple<int,int> start, Tuple<int,Direction> move)
        {
            switch (move.Item2)
            {
                case Direction.Up:
                    return new Tuple<int, int>(start.Item1, start.Item2 + 1);
                case Direction.Right:
                    return new Tuple<int, int>(start.Item1 + 1, start.Item2);
                case Direction.Down:
                    return new Tuple<int, int>(start.Item1, start.Item2 - 1);
                case Direction.Left:
                    return new Tuple<int, int>(start.Item1 - 1, start.Item2);
                default:
                    return start;
            }
        }

        public bool IsPointValid(Tuple<int, int> point)
        {
            if (point.Item1 < 0 || point.Item1 >= XSize || point.Item2 < 0 || point.Item2 >= YSize)
                return false;
            if (forbidden.Find(n => n.Item1 == point.Item1 && n.Item2 == point.Item2) != null)
                return false;
            return true;
        }
    }

    public class MatchFactory
    {
        int sizeBase = 10, forbiddenBase = 2, sequenceBase = 5;
        int difficultyMultiplier = 2;

        public Match CreateMatch(Difficulty difficulty)
        {
            Match match = new Match()
            {
                XSize = sizeBase * (int)difficulty,
                YSize = sizeBase * (int)difficulty,
                start = getRandomPoint(sizeBase * (int)difficulty, sizeBase * (int)difficulty),
                possibleMoves = getRandomInts(sequenceBase*difficultyMultiplier),
                forbidden = getRandomPoints(forbiddenBase * difficultyMultiplier, sizeBase * (int)difficulty, sizeBase * (int)difficulty)
            };

            while (!findEndPoint(match)) ;

            return match;
        }

        Tuple<int, int> getRandomPoint(int maxX, int maxY)
        {
            Random r = new Random();
            int x = r.Next(maxX), y = r.Next(maxY);
            return new Tuple<int, int>(x, y);
        }

        List<int> getRandomInts(int max)
        {
            List<int> ints = new List<int>();
            Random r = new Random();
            for (int i = 0; i < max; i++)
            {
                ints.Add(r.Next(max));
            }
            return ints;
        }

        List<Tuple<int,int>> getRandomPoints(int number, int maxX, int maxY)
        {
            List<Tuple<int, int>> points = new List<Tuple<int, int>>();
            for (int i = 0; i < number; i++)
            {
                points.Add(getRandomPoint(maxX, maxY));
            }
            return points;
        }

        bool findEndPoint(Match match)
        {
            int tries = 100;
            List<int> movesLeft = new List<int>();
            match.possibleMoves.ForEach(n=>movesLeft.Add(n));
            Random r = new Random();
            Tuple<int, int> currentPosition = copy(match.start), temp = copy(match.start);

            while (movesLeft.Count > 0 && tries>0)
            {
                tries--;
                Direction dir = (Direction)r.Next(5)+1;
                int amount = movesLeft[r.Next(movesLeft.Count)];
                temp = match.makeMove(currentPosition, new Tuple<int, Direction>(amount, dir));
                if (match.IsPointValid(temp))
                {
                    currentPosition = copy(temp);
                    movesLeft.Remove(amount);
                }
            }

            if (tries > 0)
            {
                match.end = currentPosition;
            }
            return tries > 0;
        }

        Tuple<int,int> copy(Tuple<int,int> orig)
        {
            return new Tuple<int, int>(orig.Item1, orig.Item2);
        }
    }

    public class MatchPrinter
    {
        public ResultViewModel printMatch(Match match)
        {
            ResultViewModel model = new ResultViewModel()
            {
                map = GetMap(match),
                allowedMoves = match.possibleMoves
            };
            return model;
        }

        public List<string> GetMap(Match match)
        {
            List<string> map = new List<string>();
            
            for (int i = 0; i < match.XSize; i++)
            {
                string mapString = "";
                for (int j = 0; j < match.YSize; j++)
                {
                    mapString = mapString + GetTile(match, i, j);
                }
                map.Add(mapString);
            }
            return map;
        }

        public string GetTile(Match match, int x, int y)
        {
            if (match.forbidden.Find(n => n.Item1 == x && n.Item2 == y) != null)
                return "[x]";
            else if (match.start.Item1 == x && match.start.Item2 == y)
                return "[s]";
            else if (match.end.Item1 == x && match.end.Item2 == y)
                return "[e]";
            else if (match.lastTry.Item1 == x && match.lastTry.Item2 == y)
                return "[t]";
            else
                return "[ ]";
        }
    }

    public class Moves
    {
        public int matchIndex;
        public List<Tuple<int, int>> moves { get; set; }
    }

    public class ResultViewModel
    {
        public bool victory = false;
        public List<int> allowedMoves = new List<int>();
        public List<string> map = new List<string>();
    }

    public class PlayController : ApiController
    {
        MatchFactory Factory = new MatchFactory();
        MatchPrinter printer = new MatchPrinter();
        Match[] matches = new Match[5];
        public PlayController()
        {
            for (int i = 0; i < 4; i++)
            {
                matches[i] = Factory.CreateMatch((Difficulty)1);
            }
        }

        public ResultViewModel Get(int i)
        {
            return i<4?printer.printMatch(matches[i]):new ResultViewModel();
        }

        public ResultViewModel Post(Moves moves)
        {
            var matchMoves = getMoves(moves.moves);
            Match match = matches[moves.matchIndex];
            bool solved = match.TrySolve(matchMoves);
            if (solved)
                return new ResultViewModel();
            else
                return printer.printMatch(match);
        }

        List<Tuple<int,Direction>> getMoves(List<Tuple<int, int>> postedMoves)
        {
            List<Tuple<int, Direction>> moves = new List<Tuple<int, Direction>>();
            for (int i = 0; i < postedMoves.Count; i++)
            {
                Direction direction = (Direction)postedMoves[i].Item2;
                if (direction == Direction.None)
                    return null;
                moves.Add(new Tuple<int, Direction>(postedMoves[i].Item1, direction));
            }
            return moves;
        }
    }
}
